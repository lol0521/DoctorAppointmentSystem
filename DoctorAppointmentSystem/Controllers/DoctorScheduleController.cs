using DoctorAppointmentSystem.Data;
using DoctorAppointmentSystem.Models;
using DoctorAppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DoctorAppointmentSystem.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class DoctorScheduleController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DoctorScheduleController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(DateTime? date)
        {
            var userId = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userAccountId))
            {
                return RedirectToAction("Login", "Account");
            }

            var userAccount = await _context.UserAccounts
                .Include(ua => ua.Doctor)
                .FirstOrDefaultAsync(ua => ua.Id == userAccountId);

            if (userAccount?.Doctor == null)
            {
                return NotFound("Doctor not found");
            }

            var selectedDate = date ?? DateTime.Today;

            var viewModel = new ScheduleViewModel
            {
                DoctorId = userAccount.Doctor.Id,
                SelectedDate = selectedDate,
                Appointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Where(a => a.DoctorId == userAccount.Doctor.Id &&
                               a.AppointmentDate.Date == selectedDate.Date)
                    .OrderBy(a => a.AppointmentDate)
                    
                    .ToListAsync(),
                DoctorSchedules = await _context.DoctorSchedules
                    .Where(ds => ds.DoctorId == userAccount.Doctor.Id &&
                                ds.Date == selectedDate.Date)
                    .OrderBy(ds => ds.StartTime)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetCalendarEvents(DateTime start, DateTime end)
        {
            var userId = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userAccountId))
            {
                return Json(new List<CalendarEvent>());
            }

            var userAccount = await _context.UserAccounts
                .Include(ua => ua.Doctor)
                .FirstOrDefaultAsync(ua => ua.Id == userAccountId);

            if (userAccount?.Doctor == null)
            {
                return Json(new List<CalendarEvent>());
            }

            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Where(a => a.DoctorId == userAccount.Doctor.Id &&
                           a.AppointmentDate >= start.Date &&
                           a.AppointmentDate <= end.Date)
                .ToListAsync();

            var events = appointments.Select(a => new CalendarEvent
            {
                Title = $"{a.Patient?.Name} - {a.Notes}",
                Start = a.AppointmentDate,
                End = a.AppointmentDate,
                Color = GetStatusColor(a.Status),
                AppointmentId = a.Id,
                PatientName = a.Patient?.Name ?? "Unknown",
                Status = a.Status ?? "Scheduled"
            }).ToList();

            return Json(events);
        }

        private string GetStatusColor(string status)
        {
            return status?.ToLower() switch
            {
                "confirmed" => "#28a745",
                "completed" => "#17a2b8",
                "cancelled" => "#dc3545",
                "pending" => "#ffc107",
                _ => "#007bff"
            };
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAppointmentStatus([FromBody] UpdateAppointmentRequest request)
        {
            if (request == null)
            {
                return Json(new { success = false, message = "Invalid request" });
            }

            var appointment = await _context.Appointments.FindAsync(request.AppointmentId);
            if (appointment == null)
            {
                return Json(new { success = false, message = "Appointment not found" });
            }

            // Verify the doctor owns this appointment
            var userId = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userAccountId))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            var userAccount = await _context.UserAccounts
                .Include(ua => ua.Doctor)
                .FirstOrDefaultAsync(ua => ua.Id == userAccountId);

            if (userAccount?.Doctor == null || appointment.DoctorId != userAccount.Doctor.Id)
            {
                return Json(new { success = false, message = "Unauthorized to update this appointment" });
            }

            appointment.Status = request.Status;
            _context.Appointments.Update(appointment);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Appointment status updated successfully" });
        }

        public class UpdateAppointmentRequest
        {
            public int AppointmentId { get; set; }
            public string Status { get; set; } = string.Empty;
        }

        [HttpGet]
        public async Task<IActionResult> MySchedule()
        {
            var userId = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userAccountId))
                return RedirectToAction("Login", "Account");

            var userAccount = await _context.UserAccounts
                .Include(ua => ua.Doctor)
                .FirstOrDefaultAsync(ua => ua.Id == userAccountId);

            if (userAccount?.Doctor == null)
                return NotFound("Doctor not found");

            var startDate = DateTime.Today;
            var endDate = startDate.AddDays(6); // 7 days including today

            var viewModel = new ScheduleViewModel
            {
                DoctorId = userAccount.Doctor.Id,
                SelectedDate = startDate,
                Appointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Where(a => a.DoctorId == userAccount.Doctor.Id &&
                                a.AppointmentDate.Date >= startDate &&
                                a.AppointmentDate.Date <= endDate)
                    .OrderBy(a => a.AppointmentDate)
                    .ToListAsync(),
                DoctorSchedules = await _context.DoctorSchedules
                    .Where(ds => ds.DoctorId == userAccount.Doctor.Id &&
                                 ds.Date >= startDate && ds.Date <= endDate)
                    .OrderBy(ds => ds.Date)
                    .ThenBy(ds => ds.StartTime)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSchedule(DoctorSchedule model)
        {
            var userId = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userAccountId))
            {
                return RedirectToAction("Login", "Account");
            }

            var userAccount = await _context.UserAccounts
                .Include(ua => ua.Doctor)
                .FirstOrDefaultAsync(ua => ua.Id == userAccountId);

            if (userAccount?.Doctor == null)
            {
                return NotFound("Doctor not found");
            }

            // Validation: Prevent same start and end time
            if (model.StartTime == model.EndTime)
            {
                TempData["Success"] = "❌ Start time and end time cannot be the same!";
                return RedirectToAction("MySchedule");
            }

            // Validation: Prevent start time later than end time
            if (model.StartTime > model.EndTime)
            {
                TempData["Success"] = "❌ Start time cannot be later than end time!";
                return RedirectToAction("MySchedule");
            }

            model.DoctorId = userAccount.Doctor.Id;
            _context.DoctorSchedules.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "✅ Schedule slot saved successfully!";
            return RedirectToAction("MySchedule");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            var userId = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userAccountId))
                return RedirectToAction("Login", "Account");

            var schedule = await _context.DoctorSchedules.FindAsync(id);
            if (schedule == null)
                return NotFound();

            // Verify the doctor owns this schedule
            var userAccount = await _context.UserAccounts
                .Include(ua => ua.Doctor)
                .FirstOrDefaultAsync(ua => ua.Id == userAccountId);

            if (userAccount?.Doctor == null || schedule.DoctorId != userAccount.Doctor.Id)
                return Unauthorized();

            _context.DoctorSchedules.Remove(schedule);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Schedule slot deleted successfully!";
            return RedirectToAction("MySchedule");
        }
    }
}