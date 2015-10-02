using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Web.Http;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Jwt;
using Mvc.Server.Providers;
using Owin;

namespace Mvc.Server {
    public class Startup {
        public void Configuration(IAppBuilder app) {
            var certificate = GetCertificate();

            app.Map("/api", map => {
                var configuration = new HttpConfiguration();
                configuration.MapHttpAttributeRoutes();
                configuration.EnsureInitialized();

                map.UseJwtBearerAuthentication(new JwtBearerAuthenticationOptions {
                    AuthenticationMode = AuthenticationMode.Active,
                    AllowedAudiences = new[] { "http://localhost:54540/" },
                    IssuerSecurityTokenProviders = new[] { new X509CertificateSecurityTokenProvider("http://localhost:54540/", certificate) }
                });

                map.UseWebApi(configuration);
            });

            app.SetDefaultSignInAsAuthenticationType("ServerCookie");

            // Insert a new cookies middleware in the pipeline to store
            // the user identity for the remember-me feature.
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationMode = AuthenticationMode.Active,
                AuthenticationType = "ServerCookie",
                CookieName = CookieAuthenticationDefaults.CookiePrefix + "ServerCookie",
                ExpireTimeSpan = TimeSpan.FromDays(10),
                LoginPath = new PathString("/signin")
            });

            app.UseOpenIdConnectServer(configuration => {
                configuration.UseCertificate(certificate);

                configuration.Provider = new AuthorizationProvider();
                configuration.Options.AccessTokenLifetime = TimeSpan.FromDays(14);
                configuration.Options.IdentityTokenLifetime = TimeSpan.FromMinutes(60);
                configuration.Options.AllowInsecureHttp = true;

                // Note: see AuthorizationController.cs for more
                // information concerning ApplicationCanDisplayErrors.
                configuration.Options.ApplicationCanDisplayErrors = true;
            });
        }

        private static X509Certificate2 GetCertificate() {
            // Note: in a real world app, you'd probably prefer storing the X.509 certificate
            // in the user or machine store. To keep this sample easy to use, the certificate
            // is extracted from the Certificate.pfx file embedded in this assembly.
            using (var stream = typeof(Startup).Assembly.GetManifestResourceStream("Mvc.Server.Certificate.pfx"))
            using (var buffer = new MemoryStream()) {
                stream.CopyTo(buffer);
                buffer.Flush();

                return new X509Certificate2(
                    rawData: buffer.GetBuffer(),
                    password: "Owin.Security.OpenIdConnect.Server");
            }
        }
    }
}