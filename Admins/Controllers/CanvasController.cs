using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rss.Providers.Canvas.Helpers;

namespace Admins.Controllers
{
    // derive from authorize attribute.
    [Authorize(Roles = RoleNames.AccountAdmin + "," + RoleNames.HelpDesk)]
    [ResponseCache(Duration = 3600)]
    public class CanvasController : Controller
    {
        private readonly HttpClient canvasClient;

        public CanvasController(IHttpClientFactory httpClientFactory)
        {
            this.canvasClient = httpClientFactory.CreateClient("CanvasClient");
        }

        public async Task<JsonResult> GetSubAccounts()
        {
            var response = await canvasClient.GetStringAsync("/api/v1/accounts/1/sub_accounts?per_page=100");
            return Json(JsonConvert.DeserializeObject<Object>(response));
        }

        public async Task<JsonResult> GetAdmins(string subAccountId)
        {
            var response = await canvasClient.GetStringAsync($"/api/v1/accounts/{subAccountId}/admins");
            return Json(JsonConvert.DeserializeObject<Object>(response));
        }

        public async Task<JsonResult> GetUserProfile(string userId)
        {
            var response = await canvasClient.GetStringAsync($"/api/v1/users/{userId}/profile");
            return Json(new Result() { IsUserProfile = true, Data = response });
        }

        public async Task<JsonResult> GetUserPageViews(string userId)
        {
            var response = await canvasClient.GetStringAsync($"/api/v1/users/{userId}/page_views?per_page=1");
            return Json(new Result() { IsUserProfile = false, Data = response });
        }

        public class Result
        {
            public bool IsUserProfile { get; set; }
            public string Data { get; set; }
        }
    }
}