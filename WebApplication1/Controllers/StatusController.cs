using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Web.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize] // sadece oturum sahipleri
    public class StatusController : Controller
    {
        private readonly TaskDBEntities db = new TaskDBEntities();

        // Yeni admin kontrolü: Role claim veya Session["IsAdmin"]
        private bool IsAdmin()
        {
            // Startup.cs içinde set edilen bayrak
            if (Session["IsAdmin"] is bool b && b) return true;

            // OIDC ile gelen rol claim’i
            var ci = User?.Identity as ClaimsIdentity;
            return ci?.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin") == true;
        }

        // Admin değilse nereye gitsin?
        private ActionResult NotAdminRedirect()
        {
            // login’e atmak yerine admin paneline/uygulama ana sayfasına dön
            return RedirectToAction("Index", "Admin");
        }

        public ActionResult Index()
        {
            if (!IsAdmin()) return NotAdminRedirect();
            var statusList = db.StatusOptions.ToList();
            return View(statusList);
        }

        [HttpGet]
        public ActionResult Edit(int id)
        {
            if (!IsAdmin()) return NotAdminRedirect();

            var option = db.StatusOptions.Find(id);
            if (option == null) return HttpNotFound();

            return View(option);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(StatusOptions updatedOption)
        {
            if (!IsAdmin()) return NotAdminRedirect();

            if (ModelState.IsValid)
            {
                db.Entry(updatedOption).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(updatedOption);
        }

        [HttpGet]
        public ActionResult Delete(int id)
        {
            if (!IsAdmin()) return NotAdminRedirect();

            var option = db.StatusOptions.Find(id);
            if (option == null) return HttpNotFound();

            db.StatusOptions.Remove(option);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult Create()
        {
            if (!IsAdmin()) return NotAdminRedirect();
            return View(new StatusOptions());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(StatusOptions status)
        {
            if (!IsAdmin()) return NotAdminRedirect();

            if (ModelState.IsValid)
            {
                db.StatusOptions.Add(status);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(status);
        }
    }
}

