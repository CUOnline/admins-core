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
using Admins.Models.Enums;
using Microsoft.Extensions.Options;

namespace Admins.Controllers
{
    public class HomeController : Controller
    {
        private readonly HttpClient canvasClient;
        private readonly AppSettings appSettings;
        private readonly CanvasApiAuth canvasApiAuth;

        public HomeController(IHttpClientFactory httpClientFactory, IOptions<AppSettings> settingOptions, IOptions<CanvasApiAuth> authOptions)
        {
            this.canvasClient = httpClientFactory.CreateClient("CanvasClient");
            this.appSettings = settingOptions.Value;
            this.canvasApiAuth = authOptions.Value;
        }

        public async Task<IActionResult> Index()
        {
            var authenticateResult = await HttpContext.AuthenticateAsync();
            if (!authenticateResult.Succeeded)
            {
                return RedirectToAction("ExternalLogin");
            }

            if (!(await Authorized(appSettings.CanvasAccountId)))
            {
                // return unauthorized view
                var model = new HomeViewModel()
                {
                    Authorized = false
                };
                return View(model);
            }
            else
            {
                var model = new HomeViewModel()
                {
                    Authorized = true,
                    BaseCanvasUrl = canvasApiAuth.BaseUrl,
                    ApiToken = canvasApiAuth.ApiKey
                };

                return View(model);
            }
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

        private async Task<bool> Authorized(string accountId)
        {
            List<RoleNames> authorizedRoles = new List<RoleNames>()
            {
                RoleNames.AccountAdmin,
                RoleNames.HelpDesk
            };

            var authenticateResult = await HttpContext.AuthenticateAsync();
            if (authenticateResult != null)
            {
                ViewBag.authenticated = true;
                
                var userId = authenticateResult.Principal.Claims.Where(cl => cl.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value;
                
                var roles = (await GetAccountRolesForUserAsync(accountId, userId)).Where(x => x.WorkflowState == WorkflowState.Active); // .Where(x => x == WorkflowState.Active);

                if (roles.Select(x => x.Name).Intersect(authorizedRoles).Any())
                {
                    return true;
                }
                else
                {
                    var accountResponse = await canvasClient.GetAsync($"/api/v1/accounts/{accountId}");
                    if (!accountResponse.IsSuccessStatusCode)
                    {
                        ViewBag.error = $"{accountResponse.StatusCode}: {accountResponse.ReasonPhrase}";
                    }

                    var account = JsonConvert.DeserializeObject<Account>(await accountResponse.Content.ReadAsStringAsync());
                    ViewBag.error = $"You do not have the proper roles assigned to access information for {account.Name}";
                }
            }

            return false;
        }

        public async Task<List<Role>> GetAccountRolesForUserAsync(string accountId, string userId)
        {
            var roles = new List<Role>();

            // Get the account
            var accountResponse = await canvasClient.GetAsync($"/api/v1/accounts/{accountId}");
            if (!accountResponse.IsSuccessStatusCode)
            {
                return roles;
            }

            var account = JsonConvert.DeserializeObject<Account>(await accountResponse.Content.ReadAsStringAsync());

            // Get roles for account
            var response = await canvasClient.GetAsync($"/api/v1/accounts/{accountId}/admins?user_id[]={userId}");
            if (!response.IsSuccessStatusCode)
            {
                return roles;
            }

            var result = JArray.Parse(await response.Content.ReadAsStringAsync());
            if (result.Count > 0)
            {
                foreach (var role in result.Children<JObject>())
                {
                    roles.Add(JsonConvert.DeserializeObject<Role>(role.ToString()));
                }
            }

            // Get roles for parent account (recursive)
            if (!string.IsNullOrWhiteSpace(account.ParentAccountId))
            {
                roles.AddRange(await GetAccountRolesForUserAsync(account.ParentAccountId, userId));
            }

            return roles;
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
