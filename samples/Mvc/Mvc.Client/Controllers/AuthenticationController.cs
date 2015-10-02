﻿using System;
using System.Web;
using System.Web.Mvc;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OpenIdConnect;

namespace Mvc.Client.Controllers {
    public class AuthenticationController : Controller {
        [HttpGet, Route("~/signin")]
        public ActionResult SignIn() {
            var context = HttpContext.GetOwinContext();
            if (context == null) {
                throw new NotSupportedException("An OWIN context cannot be extracted from HttpContext");
            }

            var properties = new AuthenticationProperties {
                RedirectUri = "/"
            };

            // Instruct the OIDC client middleware to redirect the user agent to the identity provider.
            // Note: the authenticationType parameter must match the value configured in Startup.cs
            context.Authentication.Challenge(properties, OpenIdConnectAuthenticationDefaults.AuthenticationType);

            return new HttpStatusCodeResult(401);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post), Route("~/signout")]
        public ActionResult SignOut() {
            var context = HttpContext.GetOwinContext();
            if (context == null) {
                throw new NotSupportedException("An OWIN context cannot be extracted from HttpContext");
            }

            // Instruct the cookies middleware to delete the local cookie created when the user agent
            // is redirected from the identity provider after a successful authorization flow.
            context.Authentication.SignOut("ClientCookie");

            // Instruct the OpenID Connect middleware to redirect
            // the user agent to the identity provider to sign out.
            context.Authentication.SignOut(OpenIdConnectAuthenticationDefaults.AuthenticationType);
            
            return new HttpStatusCodeResult(200);
        }

        [HttpGet, Route("~/localsignout"), Authorize]
        public ActionResult LocaSignOut()
        {
            var context = HttpContext.GetOwinContext();
            if (context == null)
            {
                throw new NotSupportedException("An OWIN context cannot be extracted from HttpContext");
            }

            // Instruct the cookies middleware to delete the local cookie created when the user agent
            // is redirected from the identity provider after a successful authorization flow.

            // Used only to test remember me
            context.Authentication.SignOut("ClientCookie");

            return RedirectToAction("Index", "Home");
        }
    }
}