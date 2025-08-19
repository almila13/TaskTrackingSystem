using System;
using System.Web.Mvc;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var username = Session["Username"] as string;
            var role = Session["Role"] as string;

            if (!string.IsNullOrEmpty(username))
            {
                if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
                    return RedirectToAction("Index", "Admin");
                return RedirectToAction("Index", "User");
            }

            // login olmamışsa
            return RedirectToAction("Login", "Account");
        }



        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            return View();
        }
    }
}
