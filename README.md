TaskTrackingSystem (AkTask)

Internal task-tracking web app built during my internship. Helps teammates see their own tasks; lets admins assign, filter, and review work.

Overview

Sign-in with work account, cookie session (Username + DisplayName)

Admin vs User routing (allow-list → “Admin” claim)

Admin: Assigned Tasks (search/filter + paging), My Tasks, Calendar, Create (others), Create (Mine), Status Options

User: My Tasks

Task model: Title, Status, AssignedTo, StartDate, EstimatedEndDate, Note

Notes append as username (dd.MM.yyyy HH:mm): text

Calendar: shows only the signed-in user’s tasks; popover on click

Avatar upload with fallback

Tech Stack

ASP.NET MVC (.NET Framework 4.7.2), OWIN (Cookie + OIDC)

Entity Framework 6, SQL Server (TaskDB: TaskItems, Users, StatusOptions)

UI: Bootstrap-style utilities, FullCalendar v5

Quick Start

Clone & open: WebApplication1.sln in Visual Studio.

Config: Copy Web.config.example → Web.config, fill placeholders (TenantId, ClientId, ClientSecret, DB).

DB: Point connectionStrings to your SQL instance (TaskDB).

Run: F5 (IIS Express). Test Admin/User flows and Calendar.

Secrets are not in the repo. Keep Web.config local (ignored by git).

Screenshots (suggested)

Login • Assigned Tasks • Calendar (popover) • Create Task • Azure app registration (redirect URI).

License

MIT
