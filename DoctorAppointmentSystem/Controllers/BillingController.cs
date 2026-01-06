using DoctorAppointmentSystem.Data;
using DoctorAppointmentSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace DoctorAppointmentSystem.Controllers
{
    public class BillingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BillingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Billing
        public IActionResult Index()
        {
            // Get logged-in patient
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userAccountId))
            {
                TempData["Error"] = "You must log in to view billing.";
                return RedirectToAction("Index", "Dashboard");
            }

            var patient = _context.UserAccounts
                .Include(u => u.Patient)
                .FirstOrDefault(u => u.Id == userAccountId)?.Patient;

            if (patient == null)
            {
                TempData["Error"] = "Patient record not found.";
                return RedirectToAction("Index", "Dashboard");
            }

            // Include both Pending and Confirmed appointments
            var billingAppointments = _context.Appointments
                .Include(a => a.Doctor)
                .Where(a => a.PatientId == patient.Id
                         && (a.Status == "Pending" || a.Status == "Confirmed"))
                .ToList();

            if (!billingAppointments.Any())
            {
                TempData["Success"] = "You have no pending payments.";
            }

            // Render Billing.cshtml
            return View("Billing", billingAppointments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelAppointment(int appointmentId)
        {
            var appointment = _context.Appointments.FirstOrDefault(a => a.Id == appointmentId);
            if (appointment == null)
            {
                TempData["Error"] = "Appointment not found.";
                return RedirectToAction("Index");
            }

            if (appointment.Status != "Pending")
            {
                TempData["Error"] = "Only pending appointments can be cancelled.";
                return RedirectToAction("Index");
            }

            // Delete appointment or mark as cancelled
            appointment.Status = "Cancelled";
            _context.Appointments.Update(appointment);

            _context.SaveChanges();

            TempData["Success"] = "Appointment has been cancelled.";
            return RedirectToAction("Index");
        }
    }
}