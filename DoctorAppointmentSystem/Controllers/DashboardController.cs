using DoctorAppointmentSystem.Data;
using DoctorAppointmentSystem.Models;
using DoctorAppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DoctorAppointmentSystem.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst("UserId")?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userAccountId))
            {
                return RedirectToAction("Login", "Account");
            }

            if (role == "Doctor")
            {
                return await DoctorDashboard(userAccountId);
            }
            else if (role == "Patient")
            {
                return await PatientDashboard(userAccountId);
            }

            return RedirectToAction("Index", "Home");
        }

        private async Task<IActionResult> DoctorDashboard(int userAccountId)
        {
            var userAccount = await _context.UserAccounts
                .Include(ua => ua.Doctor)
                .FirstOrDefaultAsync(ua => ua.Id == userAccountId);

            if (userAccount?.Doctor == null)
            {
                return NotFound("Doctor not found");
            }

            var today = DateTime.Today;

            var dashboard = new DoctorDashboardViewModel
            {
                TotalAppointments = await _context.Appointments
                    .CountAsync(a => a.DoctorId == userAccount.Doctor.Id),

                TodayAppointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Where(a => a.DoctorId == userAccount.Doctor.Id &&
                               a.AppointmentDate.Date == today &&
                               a.Status != "Cancelled")
                    .OrderBy(a => a.AppointmentDate)
                    .ToListAsync(),

                UpcomingAppointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Where(a => a.DoctorId == userAccount.Doctor.Id &&
                               a.AppointmentDate >= today &&
                               a.Status == "Confirmed")
                    .OrderBy(a => a.AppointmentDate)
                    .ToListAsync(),

                TotalPatients = await _context.Appointments
                    .Where(a => a.DoctorId == userAccount.Doctor.Id)
                    .Select(a => a.PatientId)
                    .Distinct()
                    .CountAsync(),

                TodaySchedule = await _context.DoctorSchedules
                    .Where(ds => ds.DoctorId == userAccount.Doctor.Id &&
                                ds.Date == today &&
                                ds.IsAvailable)
                    .OrderBy(ds => ds.StartTime)
                    .ToListAsync()
            };

            return View("DoctorDashboard", dashboard);
        }

        private async Task<IActionResult> PatientDashboard(int userAccountId)
        {
            var userAccount = await _context.UserAccounts
                .FirstOrDefaultAsync(ua => ua.Id == userAccountId);

            if (userAccount == null)
            {
                return NotFound("User account not found");
            }

            if (userAccount.PatientId == null)
            {
                return NotFound("Patient not found");
            }

            var patientId = userAccount.PatientId.Value;
            var today = DateTime.Today;

            try
            {
                // Use simpler queries first to identify which one is causing the issue
                var dashboard = new PatientDashboardViewModel
                {
                    TotalAppointments = await _context.Appointments
                        .CountAsync(a => a.PatientId == patientId),

                    // Test each query individually to find which one fails
                    UpcomingAppointments = new List<Appointment>(),
                    RecentMedicalRecords = new List<MedicalRecord>(),
                    PendingPayments = new List<Payment>()
                };

                // Try to load upcoming appointments
                try
                {
                    dashboard.UpcomingAppointments = await _context.Appointments
                        .Include(a => a.Doctor)
                        .Where(a => a.PatientId == patientId &&
                                   a.AppointmentDate >= today &&
                                   a.Status == "Confirmed")
                        .OrderBy(a => a.AppointmentDate)
                        .ToListAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading appointments: {ex.Message}");
                }

                // Try to load medical records
                try
                {
                    dashboard.RecentMedicalRecords = await _context.MedicalRecords
                        .Where(m => m.PatientId == patientId)
                        .OrderByDescending(m => m.CreatedDate)
                        .Take(5)
                        .ToListAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading medical records: {ex.Message}");
                }

                // Try to load payments
                try
                {
                    dashboard.PendingPayments = await _context.Payments
                        .Include(p => p.Appointment)
                            .ThenInclude(a => a.Doctor)
                        .Where(p => p.Appointment.PatientId == patientId &&
                                   p.Status == "Pending")
                        .OrderByDescending(p => p.PaymentDate)
                        .Take(5)
                        .ToListAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading payments: {ex.Message}");
                }

                return View("PatientDashboard", dashboard);
            }
            catch (Exception ex)
            {
                // Fallback: return basic dashboard without complex queries
                var fallbackDashboard = new PatientDashboardViewModel
                {
                    TotalAppointments = await _context.Appointments
                        .CountAsync(a => a.PatientId == patientId),
                    UpcomingAppointments = new List<Appointment>(),
                    RecentMedicalRecords = new List<MedicalRecord>(),
                    PendingPayments = new List<Payment>()
                };

                return View("PatientDashboard", fallbackDashboard);
            }
        }
    }
}