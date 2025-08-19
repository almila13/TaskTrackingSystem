using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class UserController : Controller
    {
        private readonly TaskDBEntities db = new TaskDBEntities();

        // ===== LISTE =====
        public ActionResult Index(string search, string statusFilter, DateTime? startDateFrom, DateTime? endDateTo)
        {
            if (Session["Username"] == null)
                return RedirectToAction("Login", "Account");

            string currentUser = Session["Username"].ToString();
            var tasks = db.TaskItems.Where(t => t.AssignedTo == currentUser);

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

        // ===== EDIT =====
        [HttpGet]
        public ActionResult Edit(int id)
        {
            var task = db.TaskItems.Find(id);
            if (task == null) return HttpNotFound();

            ViewBag.StatusList = new SelectList(db.StatusOptions.Select(s => s.Name), task.Status);
            // Not alanı edit ekranında boş gelsin
            ModelState.Remove("Note");
            return View(task);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, FormCollection form)
        {
            var task = db.TaskItems.Find(id);
            if (task == null) return HttpNotFound();

            string selectedStatus = form["Status"];
            string newNote = form["Note"];
            string username = Session["Username"]?.ToString();
            string timestamp = DateTime.Now.ToString("dd.MM.yyyy HH:mm");

            if (!string.IsNullOrEmpty(selectedStatus))
                task.Status = selectedStatus;

            if (!string.IsNullOrWhiteSpace(newNote))
            {
                var formattedNote = $"{username} ({timestamp}): {newNote}";
                task.Note += (string.IsNullOrWhiteSpace(task.Note) ? "" : "\n") + formattedNote;
            }

            if (DateTime.TryParse(form["EstimatedEndDate"], out DateTime estEnd))
                task.EstimatedEndDate = estEnd;

            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // ===== CREATE (kendine görev) =====
        [HttpGet]
        public ActionResult Create()
        {
            if (Session["Username"] == null)
                return RedirectToAction("Login", "Account");

            return View(new TaskItems { StartDate = DateTime.Now });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(TaskItems item)
        {
            if (Session["Username"] == null)
                return RedirectToAction("Login", "Account");

            string username = Session["Username"].ToString();

            item.AssignedTo = username;
            item.Status = "Not selected yet";

            if (!string.IsNullOrWhiteSpace(item.Note))
            {
                var stamp = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                item.Note = $"{username} ({stamp}): {item.Note}";
            }

            db.TaskItems.Add(item);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // ===== CALENDAR VIEW =====
        [HttpGet]
        public ActionResult Calendar()
        {
            if (Session["Username"] == null)
                return RedirectToAction("Login", "Account");

            // View sadece FullCalendar'ı render eder; veriyi aşağıdaki JSON endpoint verir
            return View();
        }

        // ===== CALENDAR JSON (FullCalendar events kaynağı) =====
        // FullCalendar 'start' ve 'end' parametrelerini ISO formatında gönderir.
        [HttpGet]
        public JsonResult CalendarEvents(DateTime? start, DateTime? end)
        {
            if (Session["Username"] == null)
                return Json(new object[] { }, JsonRequestBehavior.AllowGet);

            string me = Session["Username"].ToString();

            // Eğer start/end gelmediyse yaklaşık bir ay aralığı kullan
            DateTime rangeStart = start ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(-7);
            DateTime rangeEnd = end ?? rangeStart.AddMonths(1).AddDays(7);

            var list = db.TaskItems
                         .Where(t => t.AssignedTo == me)
                         .ToList();

            var events = new List<object>();

            foreach (var t in list)
            {
                // Başlangıç günü etkinliği
                if (t.StartDate.HasValue && t.StartDate.Value >= rangeStart && t.StartDate.Value <= rangeEnd)
                {
                    events.Add(new
                    {
                        id = "start_" + t.Id,
                        title = "Starting: " + t.Title,
                        start = t.StartDate.Value.ToString("s"),  // ISO-8601
                        allDay = true,
                        extendedProps = new
                        {
                            taskId = t.Id,
                            type = "Start",
                            status = t.Status,
                            assignedTo = t.AssignedTo,
                            titleText = t.Title,
                            startText = t.StartDate.Value.ToString("dd.MM.yyyy HH:mm"),
                            endText = t.EstimatedEndDate.HasValue ? t.EstimatedEndDate.Value.ToString("dd.MM.yyyy HH:mm") : "",
                            note = t.Note
                        }
                    });
                }

                // Bitiş günü etkinliği
                if (t.EstimatedEndDate.HasValue && t.EstimatedEndDate.Value >= rangeStart && t.EstimatedEndDate.Value <= rangeEnd)
                {
                    events.Add(new
                    {
                        id = "end_" + t.Id,
                        title = "Last day: " + t.Title,
                        start = t.EstimatedEndDate.Value.ToString("s"),
                        allDay = true,
                        extendedProps = new
                        {
                            taskId = t.Id,
                            type = "End",
                            status = t.Status,
                            assignedTo = t.AssignedTo,
                            titleText = t.Title,
                            startText = t.StartDate.HasValue ? t.StartDate.Value.ToString("dd.MM.yyyy HH:mm") : "",
                            endText = t.EstimatedEndDate.Value.ToString("dd.MM.yyyy HH:mm"),
                            note = t.Note
                        }
                    });
                }
            }

            return Json(events, JsonRequestBehavior.AllowGet);
        }
    }
}


