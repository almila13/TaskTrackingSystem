using System;
using System.Configuration;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin;
using Microsoft.Owin.Host.SystemWeb;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;

[assembly: OwinStartup(typeof(WebApplication1.Startup))]

namespace WebApplication1
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Cookie auth
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = CookieAuthenticationDefaults.AuthenticationType,
                LoginPath = new PathString("/Account/Login"),
                CookieManager = new SystemWebCookieManager(),
                CookieSecure = CookieSecureOption.Always,
                CookieSameSite = Microsoft.Owin.SameSiteMode.None
            });

            // Azure AD (v2) OIDC
            var tenantId = ConfigurationManager.AppSettings["ida:TenantId"];
            var clientId = ConfigurationManager.AppSettings["ida:ClientId"];
            var redirectUri = "https://localhost:44337/signin-oidc";
            var authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";

            app.UseOpenIdConnectAuthentication(new OpenIdConnectAuthenticationOptions
            {
                ClientId = clientId,
                Authority = authority,
                RedirectUri = redirectUri,
                PostLogoutRedirectUri = "https://localhost:44337/",
                ResponseType = "code id_token",
                Scope = "openid profile email offline_access",
                SaveTokens = true,
                RequireHttpsMetadata = true,

                TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "preferred_username",
                    RoleClaimType = ClaimTypes.Role,
                    ValidateIssuer = true
                },

                Notifications = new OpenIdConnectAuthenticationNotifications
                {
                    SecurityTokenValidated = ctx =>
                    {
                        var id = (ClaimsIdentity)ctx.AuthenticationTicket.Identity;

                        // Kimlik: username/email/upn
                        string userId =
                            id.FindFirst("preferred_username")?.Value ??
                            id.FindFirst(ClaimTypes.Email)?.Value ??
                            id.FindFirst(ClaimTypes.Upn)?.Value ??
                            id.Name;
                        if (userId != null) userId = userId.Trim();

                        // ✅ GÖRÜNEN AD (ad soyad)
                        string displayName =
                            id.FindFirst("name")?.Value ??
                            string.Join(" ", new[]
                            {
                                id.FindFirst(ClaimTypes.GivenName)?.Value,
                                id.FindFirst(ClaimTypes.Surname)?.Value
                            }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
                        if (string.IsNullOrWhiteSpace(displayName)) displayName = userId;

                        HttpContext.Current.Session["Username"] = userId;      // teknik amaçlar için
                        HttpContext.Current.Session["DisplayName"] = displayName; // UI'da göstereceğiz

                        // Admin allow-list → Admin rolü ekle
                        var allowRaw = ConfigurationManager.AppSettings["ida:AdminAllowList"] ?? string.Empty;
                        var allow = allowRaw.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                            .Select(s => s.Trim()).ToArray();

                        // tüm claim değerlerinde eşleşme ara
                        var allValues = id.Claims.Select(c => (c.Value ?? "").Trim()).ToList();
                        bool isAdmin = allow.Any(a => allValues.Any(v => a.Equals(v, StringComparison.OrdinalIgnoreCase)));

                        if (isAdmin)
                        {
                            id.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
                            HttpContext.Current.Session["IsAdmin"] = true;
                        }
                        else
                        {
                            HttpContext.Current.Session["IsAdmin"] = false;
                        }

                        // Güncellenmiş identity'yi cookie'ye yaz
                        ctx.AuthenticationTicket = new AuthenticationTicket(id, ctx.AuthenticationTicket.Properties);
                        return Task.FromResult(0);
                    },

                    AuthenticationFailed = ctx =>
                    {
                        ctx.HandleResponse();
                        var msg = Uri.EscapeDataString(ctx.Exception?.Message ?? "Authentication failed");
                        ctx.Response.Redirect("/Account/Error?message=" + msg);
                        return Task.FromResult(0);
                    }
                }
            });
        }
    }
}









