using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Admins.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;
using Rss.Providers.Canvas.Helpers;

namespace Admins.Controllers
{
    public class HomeController : Controller
    {
        private readonly HttpClient canvasClient;
        private readonly CanvasApiAuth canvasApiAuth;

        public HomeController(IHttpClientFactory httpClientFactory, IOptions<CanvasApiAuth> authOptions)
        {
            this.canvasClient = httpClientFactory.CreateClient("CanvasClient");
            this.canvasApiAuth = authOptions.Value;
        }

        public async Task<IActionResult> Index()
        {
            var authenticateResult = await HttpContext.AuthenticateAsync();
            if (!authenticateResult.Succeeded)
            {
                return RedirectToAction("ExternalLogin");
            }
            
            var model = new HomeViewModel();
            if (HttpContext.User.IsInRole(RoleNames.AccountAdmin) || HttpContext.User.IsInRole(RoleNames.HelpDesk))
            {
                model.Authorized = true;
                model.BaseCanvasUrl = canvasApiAuth.BaseUrl;
            }
            else
            {
                // return unauthorized view
                model.Authorized = false;
            }

            return View(model);
        }
        
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        #region LoginHelper
        [AllowAnonymous]
        public ActionResult ExternalLogin(string provider)
        {
            // Request a redirect to the external login provider
            return new ChallengeResult("Canvas", Url.Action("ExternalLoginCallback", "Home"));
        }

        [AllowAnonymous]
        public ActionResult ExternalLoginCallback()
        {
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExternalLogout(string provider)
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("LoggedOut");
        }

        public ActionResult LoggedOut()
        {
            return View();
        }

        // Used for XSRF protection when adding external logns
        private const string XsrfKey = "XsrfId";

        internal class ChallengeResult : UnauthorizedResult
        {
            private readonly string LoginProvider;
            private readonly string RedirectUri;
            private readonly string UserId;

            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                this.LoginProvider = provider;
                this.RedirectUri = redirectUri;
                this.UserId = userId;
            }

            public override void ExecuteResult(ActionContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Parameters.Add(XsrfKey, UserId);
                }
                context.HttpContext.ChallengeAsync(LoginProvider, properties);
            }
        }

        private async Task<string> GetCurrentUserEmail()
        {
            var authenticateResult = await HttpContext.AuthenticateAsync();
            if (authenticateResult != null)
            {
                var emailClaim = authenticateResult.Principal.Claims.Where(cl => cl.Type == ClaimTypes.Email).FirstOrDefault();

                return emailClaim?.Value;
            }

            return string.Empty;
        }
        #endregion
    }
}
