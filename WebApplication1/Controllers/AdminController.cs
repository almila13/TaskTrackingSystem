using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class AdminController : Controller
    {
        private readonly TaskDBEntities db = new TaskDBEntities();

        // ✅ Tüm Görevler (filtreli)
        public ActionResult Index(string search, string statusFilter, string assignedToFilter, DateTime? startDateFrom, DateTime? endDateTo)
        {
            var tasks = db.TaskItems.AsQueryable();

            // Arama
            if (!string.IsNullOrEmpty(search))
            {
                tasks = tasks.Where(t =>
                    t.Title.Contains(search) ||
                    t.Note.Contains(search) ||
                    t.StartDate.ToString().Contains(search) ||
                    t.EstimatedEndDate.ToString().Contains(search));
            }

            // Filtreler
            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
                tasks = tasks.Where(t => t.Status == statusFilter);

            if (!string.IsNullOrEmpty(assignedToFilter) && assignedToFilter != "All")
                tasks = tasks.Where(t => t.AssignedTo == assignedToFilter);

            if (startDateFrom.HasValue)
                tasks = tasks.Where(t => t.StartDate >= startDateFrom.Value);

            if (endDateTo.HasValue)
                tasks = tasks.Where(t => t.EstimatedEndDate <= endDateTo.Value);

            // Status ve kullanıcı listeleri
            ViewBag.StatusList = new List<string> { "All", "In Progress", "Completed", "On Hold", "Cancelled" };
            ViewBag.UserList = db.TaskItems.Select(t => t.AssignedTo).Distinct().ToList();

            return View(tasks.ToList());
        }

        // ✅ Admin → "My Tasks" (sadece kendine atanmışlar)
        [HttpGet]
        public ActionResult MyTasks(string search, string statusFilter, DateTime? startDateFrom, DateTime? endDateTo)
        {
            var me = (Session["Username"] as string) ?? (User?.Identity?.Name ?? "");
            var tasks = db.TaskItems.Where(t => t.AssignedTo == me);

            if (!string.IsNullOrWhiteSpace(search))
            {
                tasks = tasks.Where(t =>
                    t.Title.Contains(search) ||
                    t.Note.Contains(search) ||
                    DbFunctions.TruncateTime(t.StartDate).ToString().Contains(search) ||
                    DbFunctions.TruncateTime(t.EstimatedEndDate).ToString().Contains(search));
            }

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
                tasks = tasks.Where(t => t.Status == statusFilter);

            if (startDateFrom.HasValue)
                tasks = tasks.Where(t => t.StartDate >= startDateFrom.Value);

            if (endDateTo.HasValue)
                tasks = tasks.Where(t => t.EstimatedEndDate <= endDateTo.Value);

            ViewBag.StatusList = db.StatusOptions.Select(s => s.Name).Distinct().ToList();
            return View(tasks.ToList());
        }

        // ✅ Yeni Görev Oluştur (başkasına atama) – GET
        [HttpGet]
        public ActionResult Create()
        {
            ViewBag.StatusList = new SelectList(db.StatusOptions.Select(s => s.Name));
            var users = db.Users.Select(u => u.Username).ToList();
            if (users == null || users.Count == 0)
            {
                // Users boşsa form patlamasın
                var me = (Session["Username"] as string) ?? (User?.Identity?.Name ?? "me");
                users = new List<string> { me };
            }
            ViewBag.Users = new SelectList(users);
            return View();
        }

        // ✅ Yeni Görev Oluştur (başkasına atama) – POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(TaskItems item)
        {
            if (string.IsNullOrWhiteSpace(item.Status))
                item.Status = "Not selected yet";

            if (!string.IsNullOrWhiteSpace(item.Note))
            {
                var username = "admin";
                var timestamp = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                item.Note = $"{username} ({timestamp}): {item.Note}";
            }

            db.TaskItems.Add(item);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // ✅ Admin kendine görev oluşturur – GET (CreateSelf)
        [HttpGet]
        public ActionResult CreateSelf()
        {
            return View(new TaskItems
            {
                StartDate = DateTime.Now
            });
        }

        // ✅ Admin kendine görev oluşturur – POST (CreateSelf)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateSelf(TaskItems item)
        {
            var me = (Session["Username"] as string) ?? (User?.Identity?.Name ?? "");
            item.AssignedTo = me;
            if (string.IsNullOrWhiteSpace(item.Status))
                item.Status = "Not selected yet";

            if (!string.IsNullOrWhiteSpace(item.Note))
            {
                var timestamp = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                item.Note = $"admin ({timestamp}): {item.Note}";
            }

            db.TaskItems.Add(item);
            db.SaveChanges();
            return RedirectToAction("MyTasks");
        }

        // ✅ Görev Düzenle (GET)
        [HttpGet]
        public ActionResult Edit(int id)
        {
            var task = db.TaskItems.Find(id);
            if (task == null) return HttpNotFound();

            var statusOptions = db.StatusOptions.Select(s => s.Name).ToList();
            ViewBag.StatusList = new SelectList(statusOptions, task.Status);

            var users = db.Users.Select(u => u.Username).ToList();
            ViewBag.Users = new SelectList(users, task.AssignedTo);

            return View(task);
        }

        // ✅ Görev Düzenle (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(TaskItems updatedTask)
        {
            var task = db.TaskItems.Find(updatedTask.Id);
            if (task == null) return HttpNotFound();

            var timestamp = DateTime.Now.ToString("dd.MM.yyyy HH:mm");

            task.Title = updatedTask.Title;
            task.Status = updatedTask.Status;
            task.AssignedTo = updatedTask.AssignedTo;
            task.StartDate = updatedTask.StartDate;
            task.EstimatedEndDate = updatedTask.EstimatedEndDate;

            if (!string.IsNullOrWhiteSpace(updatedTask.Note))
            {
                var formattedNote = $"admin ({timestamp}): {updatedTask.Note}";
                task.Note += (string.IsNullOrWhiteSpace(task.Note) ? "" : "\n") + formattedNote;
            }

            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // ✅ Görev Sil
        [HttpGet]
        public ActionResult Delete(int id)
        {
            var task = db.TaskItems.Find(id);
            if (task != null)
            {
                db.TaskItems.Remove(task);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // ✅ TAKVİM EVENT KAYNAĞI (sadece kendi görevlerin)
        [HttpGet]
        public ActionResult CalendarEvents(DateTime? start, DateTime? end)
        {
            var from = start ?? DateTime.Today.AddDays(-30);
            var to = end ?? DateTime.Today.AddDays(60);

            var query = db.TaskItems.Where(t =>
                (t.StartDate.HasValue && t.StartDate >= from && t.StartDate <= to) ||
                (t.EstimatedEndDate.HasValue && t.EstimatedEndDate >= from && t.EstimatedEndDate <= to)
            );

            var userId = (Session["Username"] as string ?? "").Trim();
            var dispName = (Session["DisplayName"] as string ?? "").Trim();
            var userPart = userId.Contains("@") ? userId.Split('@')[0] : userId;

            var aliases = new[] { userId, dispName, userPart }
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var list = query.ToList()
                .Where(t => aliases.Any(a => string.Equals(t.AssignedTo?.Trim(), a, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var events = new List<object>();

            foreach (var t in list)
            {
                if (t.StartDate.HasValue)
                {
                    events.Add(new
                    {
                        id = $"{t.Id}-start",
                        title = $"Starting: {t.Title}",
                        start = t.StartDate.Value.ToString("s"),
                        allDay = true,
                        extendedProps = new
                        {
                            taskId = t.Id,
                            when = t.StartDate.Value.ToString("dd.MM.yyyy"),
                            note = t.Note,
                            status = t.Status,
                            type = "start",
                            assignedTo = t.AssignedTo
                        }
                    });
                }

                if (t.EstimatedEndDate.HasValue)
                {
                    events.Add(new
                    {
                        id = $"{t.Id}-end",
                        title = $"Last day: {t.Title}",
                        start = t.EstimatedEndDate.Value.ToString("s"),
                        allDay = true,
                        extendedProps = new
                        {
                            taskId = t.Id,
                            when = t.EstimatedEndDate.Value.ToString("dd.MM.yyyy"),
                            note = t.Note,
                            status = t.Status,
                            type = "end",
                            assignedTo = t.AssignedTo
                        }
                    });
                }
            }

            return Json(events, JsonRequestBehavior.AllowGet);
        }

        // ✅ Takvim sayfası
        public ActionResult Calendar()
        {
            return View();
        }
    }
}

