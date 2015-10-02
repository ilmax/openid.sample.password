using System;
using System.Web;
using System.Web.Mvc;
using Microsoft.Owin.Security;

namespace Mvc.Server.Controllers
{
    public class AuthenticationController : Controller
    {
        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post), Route("~/signout")]
        public ActionResult SignOut()
        {
            // Instruct the cookies middleware to delete the local cookie created
            // when the user agent is redirected from the external identity provider
            // after a successful authentication flow (e.g Google or Facebook).
            AuthenticationManager.SignOut("ServerCookie");

            return new HttpStatusCodeResult(200);
        }

        /// <summary>
        /// Gets the IAuthenticationManager instance associated with the current request.
        /// </summary>
        protected IAuthenticationManager AuthenticationManager
        {
            get
            {
                var context = HttpContext.GetOwinContext();
                if (context == null)
                {
                    throw new NotSupportedException("An OWIN context cannot be extracted from HttpContext");
                }

                return context.Authentication;
            }
        }
    }
}