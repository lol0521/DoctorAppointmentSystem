using DoctorAppointmentSystem.Data;
using DoctorAppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DoctorAppointmentSystem.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PatientAppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PatientAppointmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string filter = "upcoming")
        {
            var userId = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userAccountId))
            {
                return RedirectToAction("Login", "Account");
            }

            var userAccount = await _context.UserAccounts
                .Include(ua => ua.Patient)
                .FirstOrDefaultAsync(ua => ua.Id == userAccountId);

            if (userAccount?.Patient == null)
            {
                return NotFound("Patient not found");
            }

            var today = DateTime.Today;

            var viewModel = new PatientAppointmentViewModel
            {
                Filter = filter,
                UpcomingAppointments = await _context.Appointments
                    .Include(a => a.Doctor)
                    .Where(a => a.PatientId == userAccount.Patient.Id &&
                               a.AppointmentDate >= today &&
                               a.Status != "Cancelled")
                    .OrderBy(a => a.AppointmentDate)
                    .ToListAsync(),
                PastAppointments = await _context.Appointments
                    .Include(a => a.Doctor)
                    .Where(a => a.PatientId == userAccount.Patient.Id &&
                               (a.AppointmentDate < today || a.Status == "Cancelled"))
                    .OrderByDescending(a => a.AppointmentDate)
                    .ToListAsync(),
                AllAppointments = await _context.Appointments
                    .Include(a => a.Doctor)
                    .Where(a => a.PatientId == userAccount.Patient.Id)
                    .OrderByDescending(a => a.AppointmentDate)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var userId = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userAccountId))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            var userAccount = await _context.UserAccounts
                .Include(ua => ua.Patient)
                .FirstOrDefaultAsync(ua => ua.Id == userAccountId);

            if (userAccount?.Patient == null)
            {
                return Json(new { success = false, message = "Patient not found" });
            }

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id && a.PatientId == userAccount.Patient.Id);

            if (appointment == null)
            {
                return Json(new { success = false, message = "Appointment not found. Please check if the appointment exists and belongs to you." });
            }

            if (appointment.AppointmentDate < DateTime.Today)
            {
                return Json(new { success = false, message = "Cannot cancel past appointments" });
            }

            if (appointment.Status == "Cancelled")
            {
                return Json(new { success = false, message = "Appointment is already cancelled" });
            }

            appointment.Status = "Cancelled";
            _context.Appointments.Update(appointment);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Appointment cancelled successfully" });
        }

        [HttpGet]
        public async Task<IActionResult> GetAppointmentDetails(int id)
        {
            var userId = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userAccountId))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            var userAccount = await _context.UserAccounts
                .Include(ua => ua.Patient)
                .FirstOrDefaultAsync(ua => ua.Id == userAccountId);

            if (userAccount?.Patient == null)
            {
                return Json(new { success = false, message = "Patient not found" });
            }

            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.Id == id && a.PatientId == userAccount.Patient.Id);

            if (appointment == null)
            {
                return Json(new { success = false, message = "Appointment not found" });
            }

            return Json(new
            {
                success = true,
                appointment = new
                {
                    id = appointment.Id,
                    doctorName = appointment.Doctor?.Name,
                    specialty = appointment.Doctor?.Specialty,
                    date = appointment.AppointmentDate.ToString("yyyy-MM-dd HH:mm"),
                    duration = appointment.Duration,
                    status = appointment.Status,
                    notes = appointment.Notes
                }
            });
        }
    }
}