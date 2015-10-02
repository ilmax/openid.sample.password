﻿using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Mvc.Server.Models;
using Owin.Security.OpenIdConnect.Extensions;
using Owin.Security.OpenIdConnect.Server;

namespace Mvc.Server.Providers {
    public class AuthorizationProvider : OpenIdConnectServerProvider {
        public override async Task ValidateClientAuthentication(ValidateClientAuthenticationContext context) {
            // Note: client authentication is not mandatory for non-confidential client applications like mobile apps
            // (except when using the client credentials grant type) but this authorization server uses a safer policy
            // that makes client authentication mandatory and returns an error if client_id or client_secret is missing.
            // You may consider relaxing it to support the resource owner password credentials grant type
            // with JavaScript or desktop applications, where client credentials cannot be safely stored.
            // In this case, call context.Skipped() to inform the server middleware the client is not trusted.
            if (string.IsNullOrEmpty(context.ClientId) || string.IsNullOrEmpty(context.ClientSecret)) {
                context.Rejected(
                    error: "invalid_request",
                    description: "Missing credentials: ensure that your credentials were correctly " +
                                 "flowed in the request body or in the authorization header");

                return;
            }

            using (var database = new ApplicationContext()) {
                // Retrieve the application details corresponding to the requested client_id.
                var application = await (from entity in database.Applications
                                         where entity.ApplicationID == context.ClientId
                                         select entity).SingleOrDefaultAsync(context.OwinContext.Request.CallCancelled);

                if (application == null) {
                    context.Rejected(
                        error: "invalid_client",
                        description: "Application not found in the database: " +
                                     "ensure that your client_id is correct");
                    return;
                }

                if (!string.Equals(context.ClientSecret, application.Secret, StringComparison.Ordinal)) {
                    context.Rejected(
                        error: "invalid_client",
                        description: "Invalid credentials: ensure that you " +
                                     "specified a correct client_secret");

                    return;
                }

                context.Validated();
            }
        }

        public override async Task ValidateClientRedirectUri(ValidateClientRedirectUriContext context) {
            using (var database = new ApplicationContext()) {
                // Retrieve the application details corresponding to the requested client_id.
                var application = await (from entity in database.Applications
                                         where entity.ApplicationID == context.ClientId
                                         select entity).SingleOrDefaultAsync(context.OwinContext.Request.CallCancelled);

                if (application == null) {
                    context.Rejected(
                        error: "invalid_client",
                        description: "Application not found in the database: " +
                                     "ensure that your client_id is correct");
                    return;
                }

                if (!string.IsNullOrEmpty(context.RedirectUri)) {
                    if (!string.Equals(context.RedirectUri, application.RedirectUri, StringComparison.Ordinal)) {
                        context.Rejected(error: "invalid_client", description: "Invalid redirect_uri");

                        return;
                    }
                }

                context.Validated(application.RedirectUri);
            }
        }

        public override async Task ValidateClientLogoutRedirectUri(ValidateClientLogoutRedirectUriContext context) {
            using (var database = new ApplicationContext()) {
                // Note: ValidateClientLogoutRedirectUri is not invoked when post_logout_redirect_uri is null.
                // When provided, post_logout_redirect_uri must exactly match the address registered by the client application.
                if (!await database.Applications.AnyAsync(application => application.LogoutRedirectUri == context.PostLogoutRedirectUri)) {
                    context.Rejected(error: "invalid_client", description: "Invalid post_logout_redirect_uri");

                    return;
                }

                context.Validated();
            }
        }

        public override async Task CreateAuthorizationCode(CreateAuthorizationCodeContext context) {
            using (var database = new ApplicationContext()) {
                // Create a new unique identifier that will be used to replace the authorization code serialized
                // by CreateAuthorizationCodeNotification.SerializeTicket() during the code/token exchange process.
                // Note: while you can replace the generation mechanism, you MUST ensure your custom algorithm
                // generates unpredictable identifiers to guarantee a correct entropy.

                string nonceID = Guid.NewGuid().ToString();

                var nonce = new Nonce {
                    NonceID = nonceID,
                    Ticket = await context.SerializeTicketAsync()
                };

                database.Nonces.Add(nonce);
                await database.SaveChangesAsync(context.OwinContext.Request.CallCancelled);

                context.AuthorizationCode = nonceID;
            }
        }

        public override async Task ReceiveAuthorizationCode(ReceiveAuthorizationCodeContext context) {
            using (var database = new ApplicationContext()) {
                // Retrieve the authorization code serialized by CreateAuthorizationCodeNotification.SerializeTicket
                // using the nonce identifier generated in CreateAuthorizationCode and returned to the client application.
                // Note: you MUST ensure the nonces are correctly removed after each call to prevent replay attacks.
                string nonceID = context.AuthorizationCode;

                var nonce = await (from entity in database.Nonces
                                   where entity.NonceID == nonceID
                                   select entity).SingleOrDefaultAsync(context.OwinContext.Request.CallCancelled);

                if (nonce == null) {
                    return;
                }

                database.Nonces.Remove(nonce);
                await database.SaveChangesAsync(context.OwinContext.Request.CallCancelled);

                context.AuthenticationTicket = await context.DeserializeTicketAsync(nonce.Ticket);
            }
        }

        public override Task MatchEndpoint(MatchEndpointContext context) {
            // Note: by default, OpenIdConnectServerHandler only handles authorization requests made to the authorization endpoint.
            // This notification handler uses a more relaxed policy that allows extracting authorization requests received at
            // /connect/authorize/login (see AuthorizationController.cs for more information).
            if (context.Options.AuthorizationEndpointPath.HasValue &&
                context.Request.Path.StartsWithSegments(context.Options.AuthorizationEndpointPath)) {
                context.MatchesAuthorizationEndpoint();
            }

            return Task.FromResult<object>(null);
        }

        public override Task ValidateTokenRequest(ValidateTokenRequestContext context) {
            // Note: OpenIdConnectServerHandler supports authorization code, refresh token, client credentials
            // and resource owner password credentials grant types but this authorization server uses a safer policy
            // rejecting the last two ones. You may consider relaxing it to support the ROPC or client credentials grant types.
            if (!context.Request.IsAuthorizationCodeGrantType() && !context.Request.IsRefreshTokenGrantType()) {
                context.Rejected(
                    error: "unsupported_grant_type",
                    description: "Only authorization code and refresh token grant types " +
                                 "are accepted by this authorization server");
            }

            return Task.FromResult<object>(null);
        }
    }
}