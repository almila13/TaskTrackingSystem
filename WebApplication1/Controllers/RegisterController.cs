using System.Linq;
using System.Web.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class RegisterController : Controller
    {
        // GET: /Register
        [HttpGet]
        public ActionResult Index()
        {
            return View(new Users());
        }

        // POST: /Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(Users model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var email = (model.Username ?? "").Trim().ToLower();
            var pwd = (model.Password ?? "").Trim();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pwd))
            {
                ViewBag.Message = "E-posta ve şifre zorunludur.";
                return View(model);
            }

            using (var db = new TaskDBEntities())
            {
                // Aynı e-posta var mı?
                bool exists = db.Users.Any(u => u.Username.ToLower() == email);
                if (exists)
                {
                    ViewBag.Message = "Bu e-posta zaten kayıtlı.";
                    return View(model);
                }

                // (İstersen admin e-postasını burada özel işaretleyebilirsin)
                // if (email == "admin@firma.com") { ... }

                db.Users.Add(new Users
                {
                    Username = email,
                    Password = pwd // Not: ileride hash'leyeceğiz
                });
                db.SaveChanges();

                ViewBag.Message = "Kayıt başarılı! Giriş yapabilirsiniz.";
                return View(new Users());
            }
        }
    }
}



