using DoctorAppointmentSystem.Data;
using DoctorAppointmentSystem.Models;
using DoctorAppointmentSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DoctorAppointmentSystem.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public PaymentController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public IActionResult Index(int appointmentId, decimal amount)
        {
            var model = new PaymentModel
            {
                AppointmentId = appointmentId,
                Amount = amount
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(PaymentModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            var payment = new Payment
            {
                AppointmentId = model.AppointmentId,
                Amount = model.Amount,
                PaymentMethod = "Credit Card",
                PaymentDate = DateTime.Now,
                Status = "Paid"
            };
            _context.Payments.Add(payment);

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == model.AppointmentId);

            if (appointment != null)
            {
                appointment.Status = "Confirmed";
                _context.Appointments.Update(appointment);
            }

            await _context.SaveChangesAsync();

            if (appointment?.Patient?.Email != null)
            {
                await _emailService.SendPaymentConfirmationEmailAsync(
                    appointment.Patient.Email,
                    appointment.Patient.Name,
                    model.Amount,
                    appointment
                );
            }

            return RedirectToAction("Confirmation", new { amount = model.Amount });
        }

        public IActionResult Confirmation(decimal amount)
        {
            ViewBag.Amount = amount;
            return View();
        }

        private async Task SendAppointmentConfirmationEmail(Appointment appointment)
        {
            var subject = "Appointment Confirmation - Doctor Appointment System";

            // Format duration for display
            string durationText = appointment.Duration switch
            {
                30 => "30 minutes",
                60 => "1 hour",
                90 => "1.5 hours",
                _ => $"{appointment.Duration} minutes"
            };

            // Calculate end time
            var endTime = appointment.AppointmentDate.AddMinutes(appointment.Duration);

            var message = $@"
        <h3>Appointment Confirmed!</h3>
        <p>Dear {appointment.Patient.Name},</p>
        <p>Your appointment has been successfully booked:</p>
        <p><strong>Date:</strong> {appointment.AppointmentDate:MMMM dd, yyyy}</p>
        <p><strong>Time:</strong> {appointment.AppointmentDate:hh:mm tt} - {endTime:hh:mm tt}</p>
        <p><strong>Duration:</strong> {durationText}</p>
        <p><strong>Doctor:</strong> Dr. {appointment.Doctor.Name}</p>
        <p><strong>Specialty:</strong> {appointment.Doctor.Specialty}</p>
        <p>You will receive a reminder on the day of your appointment.</p>
        <br>
        <p>Thank you for choosing our service!</p>
    ";

            await _emailService.SendEmailAsync(appointment.Patient.Email, subject, message);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PayLater(int appointmentId, decimal amount)
        {
            var appointment = _context.Appointments.FirstOrDefault(a => a.Id == appointmentId);
            if (appointment != null)
            {
                appointment.Status = "Pending";
                _context.Appointments.Update(appointment);
                _context.SaveChanges();
            }

            return RedirectToAction("Index", "Billing");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessEWallet(int appointmentId, decimal amount, string eWalletPhoneNumber)
        {
            if (string.IsNullOrEmpty(eWalletPhoneNumber) || !long.TryParse(eWalletPhoneNumber, out _))
            {
                // If the value is not a valid number, set an error message in TempData.
                TempData["PaymentError"] = "Invalid phone number format. Please enter digits only.";
                // Redirect the user back to the payment selection page.
                return RedirectToAction("Index", new { appointmentId = appointmentId, amount = amount });
            }

            var payment = new Payment
            {
                AppointmentId = appointmentId,
                Amount = amount,
                PaymentMethod = "E-Wallet",
                PaymentDate = DateTime.Now,
                Status = "Paid",
            };
            _context.Payments.Add(payment);

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment != null)
            {
                appointment.Status = "Confirmed";
                _context.Appointments.Update(appointment);
            }

            await _context.SaveChangesAsync();

            if (appointment?.Patient?.Email != null)
            {
                await _emailService.SendPaymentConfirmationEmailAsync(
                    appointment.Patient.Email,
                    appointment.Patient.Name,
                    amount,
                    appointment
                );
            }

            return RedirectToAction("Confirmation", new { amount = amount });
        }
    }
}