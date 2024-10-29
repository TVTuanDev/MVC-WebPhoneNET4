using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace WebPhone.Attributes
{
    public class AppAuthorizeAttribute : AuthorizeAttribute
    {
        public string RoleName { get; set; }

        public AppAuthorizeAttribute(string roleName = "Guest")
        {
            RoleName = roleName;
        }
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (!httpContext.User.Identity.IsAuthenticated)
                return false;

            return CanAccessToAction(httpContext);
        }

        private bool CanAccessToAction(HttpContextBase httpContext)
        {
            if (RoleName == "Guest") return true;

            var claimPrincipal = httpContext.Session["UserClaim"] as ClaimsPrincipal;
            var listRoleNameAttr = RoleName.Split(',').Select(r => r.Trim()).ToList();

            return listRoleNameAttr.Any(roleName => claimPrincipal.IsInRole(roleName));
        }

        protected override void HandleUnauthorizedRequest(System.Web.Mvc.AuthorizationContext filterContext)
        {
            var httpContext = filterContext.HttpContext;

            if (!httpContext.User.Identity.IsAuthenticated)
            {
                var returnUrl = httpContext.Request.FilePath;
                filterContext.Result = new RedirectResult($"~/customer/login?returnUrl={returnUrl}");
                return;
            }

            filterContext.Result = new RedirectResult("~/access-denied");
        }
    }
}