using System;
using System.Linq;
using System.Security.Claims;
using GittiBu.Common;
using GittiBu.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GittiBu.Web.Areas.AdminPanel.Controllers
{
    [Area("AdminPanel")]
    [Authorize(Roles = "Admin")]
    public class AdminBaseController : Controller
    {
        public UiMessage AdminNotification
        {
            get => TempData["ErrorMessage"] == null ? null : TempData.Get<UiMessage>("Notification");
            set => TempData.Put("Notification", new UiMessage { Type = value.Type, Message = value.Message });
        }

        public int GetLoginID()
        {
            return User.Identity.IsAuthenticated ? int.Parse(User.Claims.Single(x => x.Type == ClaimTypes.UserData).Value) : 0;
        }
        
        public string GetIpAddress()
        {
            try
            {
                return HttpContext.Connection.RemoteIpAddress.ToString();
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}