using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;

namespace WebApplication1.Controllers
{
    public class AccountController : Controller
    {
        [AllowAnonymous]
        public ActionResult Login() => View();

        [AllowAnonymous]
        public ActionResult SignIn(string returnUrl = null)
        {
            if (Request.IsAuthenticated) return RedirectToAppHome();

            var redirect = Url.Action("SignedIn", "Account", new { returnUrl }, Request.Url.Scheme);
            HttpContext.GetOwinContext().Authentication.Challenge(
                new AuthenticationProperties { RedirectUri = redirect },
                OpenIdConnectAuthenticationDefaults.AuthenticationType
            );
            return new HttpUnauthorizedResult();
        }

        [AllowAnonymous]
        public ActionResult SignedIn(string returnUrl = null)
        {
            if (!Request.IsAuthenticated) return RedirectToAction("Login");

            var ci = User.Identity as ClaimsIdentity;

            // username/email/upn (teknik)
            var name =
                ci?.FindFirst("preferred_username")?.Value ??
                ci?.FindFirst(ClaimTypes.Email)?.Value ??
                ci?.FindFirst(ClaimTypes.Upn)?.Value ??
                ci?.Name ?? User.Identity.Name;

            Session["Username"] = name;

            // Görünen ad
            var dn =
                ci?.FindFirst("name")?.Value ??
                string.Join(" ", new[]
                {
                    ci?.FindFirst(ClaimTypes.GivenName)?.Value,
                    ci?.FindFirst(ClaimTypes.Surname)?.Value
                }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
            if (string.IsNullOrWhiteSpace(dn)) dn = name;
            Session["DisplayName"] = dn;

            // Admin claim?
            bool claimAdmin = ci?.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin") == true;

            if (claimAdmin) return RedirectToAction("Index", "Admin");
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Index", "User");
        }

        [Authorize]
        public ActionResult Logout()
        {
            HttpContext.GetOwinContext().Authentication.SignOut(
                OpenIdConnectAuthenticationDefaults.AuthenticationType,
                CookieAuthenticationDefaults.AuthenticationType
            );
            return RedirectToAction("Login");
        }

        private ActionResult RedirectToAppHome()
        {
            var ci = User.Identity as ClaimsIdentity;
            bool isAdmin = ci?.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin") == true
                           || ((Session["IsAdmin"] as bool?) == true);
            return Redirect(isAdmin ? Url.Action("Index", "Admin") : Url.Action("Index", "User"));
        }

        // ===== Profile / Avatar =====

        [Authorize]
        [HttpGet]
        public ActionResult Profile()
        {
            var u = (Session["Username"] as string) ?? (User?.Identity?.Name ?? "user");
            ViewBag.Username = u;
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Profile(HttpPostedFileBase file)
        {
            var u = (Session["Username"] as string) ?? (User?.Identity?.Name ?? "user");
            if (file != null && file.ContentLength > 0)
            {
                var safe = Regex.Replace(u, @"[^a-zA-Z0-9@._-]+", "_");
                var dir = Server.MapPath("~/Content/Uploads/avatars/");
                Directory.CreateDirectory(dir);
                var ext = Path.GetExtension(file.FileName);
                if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
                var full = Path.Combine(dir, safe + ext);
                file.SaveAs(full);
                TempData["Message"] = "Saved";
            }
            else
            {
                TempData["Message"] = "No file selected";
            }
            return RedirectToAction("Profile");
        }

        [AllowAnonymous]
        public ActionResult Avatar(string u = null)
        {
            var id = u ?? (Session["Username"] as string) ?? (User?.Identity?.Name ?? "user");
            var safe = Regex.Replace(id, @"[^a-zA-Z0-9@._-]+", "_");
            var dir = Server.MapPath("~/Content/Uploads/avatars/");
            Directory.CreateDirectory(dir);

            // arama: safe.* (jpg/png/jpeg)
            var candidates = Directory.GetFiles(dir, safe + ".*");
            var path = candidates.FirstOrDefault();

            if (string.IsNullOrEmpty(path))
            {
                // varsayılan
                var def = Server.MapPath("~/Content/Images/default_avatar.png");
                return System.IO.File.Exists(def) ? File(def, "image/png") : null;
            }

            var mime = "image/jpeg";
            var ext = Path.GetExtension(path)?.ToLowerInvariant();
            if (ext == ".png") mime = "image/png";
            return File(path, mime);
        }
    }
}




