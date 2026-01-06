// NotificationsController.cs
using DoctorAppointmentSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace DoctorAppointmentSystem.Controllers
{
    [Authorize(Roles = "Patient")]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public NotificationsController(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        [HttpGet]
        public async Task<IActionResult> GetAppointments()
        {
            var userId = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userAccountId))
            {
                return Json(new { success = false, appointments = new List<object>(), unreadCount = 0 });
            }

            // Get patient
            var patient = await _context.UserAccounts
                .Include(ua => ua.Patient)
                .Where(ua => ua.Id == userAccountId)
                .Select(ua => ua.Patient)
                .FirstOrDefaultAsync();

            if (patient == null)
            {
                return Json(new { success = false, appointments = new List<object>(), unreadCount = 0 });
            }

            // Get read appointments from cache
            var readAppointmentsKey = $"read_appointments_{patient.Id}";
            var readAppointments = _cache.Get<HashSet<int>>(readAppointmentsKey) ?? new HashSet<int>();

            // Get upcoming appointments
            var now = DateTime.Now;
            var nextWeek = now.AddDays(7);

            var appointments = await _context.Appointments
                .Include(a => a.Doctor)
                .Where(a => a.PatientId == patient.Id &&
                           a.AppointmentDate >= now &&
                           a.AppointmentDate <= nextWeek &&
                           a.Status == "Confirmed")
                .OrderBy(a => a.AppointmentDate)
                .Select(a => new
                {
                    a.Id,
                    DoctorName = a.Doctor.Name,
                    DoctorSpecialty = a.Doctor.Specialty,
                    a.AppointmentDate,
                    a.Duration,
                    FormattedDate = a.AppointmentDate.ToString("MMMM dd, yyyy"),
                    FormattedTime = a.AppointmentDate.ToString("hh:mm tt"),
                    FormattedEndTime = a.AppointmentDate.AddMinutes(a.Duration).ToString("hh:mm tt"),
                    Status = a.Status,
                    Notes = a.Notes,
                    IsToday = a.AppointmentDate.Date == DateTime.Today,
                    IsNew = !readAppointments.Contains(a.Id)
                })
                .ToListAsync();

            var unreadCount = appointments.Count(a => a.IsNew);

            return Json(new
            {
                success = true,
                appointments,
                unreadCount
            });
        }

        [HttpPost]
        public IActionResult MarkAsRead(int appointmentId)
        {
            var userId = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userAccountId))
            {
                return Json(new { success = false });
            }

            // Get patient
            var patient = _context.UserAccounts
                .Include(ua => ua.Patient)
                .FirstOrDefault(ua => ua.Id == userAccountId)?.Patient;

            if (patient != null)
            {
                // Get current read appointments
                var readAppointmentsKey = $"read_appointments_{patient.Id}";
                var readAppointments = _cache.Get<HashSet<int>>(readAppointmentsKey) ?? new HashSet<int>();

                readAppointments.Add(appointmentId);

                // Store back in cache (valid for 30 days)
                _cache.Set(readAppointmentsKey, readAppointments, TimeSpan.FromDays(30));
            }

            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult MarkAllAsRead()
        {
            var userId = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userAccountId))
            {
                return Json(new { success = false });
            }

            // Get patient
            var patient = _context.UserAccounts
                .Include(ua => ua.Patient)
                .FirstOrDefault(ua => ua.Id == userAccountId)?.Patient;

            if (patient != null)
            {
                // Get all upcoming appointments and mark them as read
                var readAppointmentsKey = $"read_appointments_{patient.Id}";
                var now = DateTime.Now;
                var nextWeek = now.AddDays(7);

                var appointmentIds = _context.Appointments
                    .Where(a => a.PatientId == patient.Id &&
                               a.AppointmentDate >= now &&
                               a.AppointmentDate <= nextWeek &&
                               a.Status == "Confirmed")
                    .Select(a => a.Id)
                    .ToList();

                // Store all appointment IDs as read
                _cache.Set(readAppointmentsKey, new HashSet<int>(appointmentIds), TimeSpan.FromDays(30));
            }

            return Json(new { success = true });
        }
    }
}