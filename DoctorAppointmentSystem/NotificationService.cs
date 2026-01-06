using DoctorAppointmentSystem.Data;
using DoctorAppointmentSystem.Models;
using DoctorAppointmentSystem.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DoctorAppointmentSystem.Services
{
    public class NotificationService
    {
        private readonly IMemoryCache _cache;
        private readonly IEmailService _emailService;
        private readonly ApplicationDbContext _context;

        public NotificationService(IMemoryCache cache, IEmailService emailService, ApplicationDbContext context)
        {
            _cache = cache;
            _emailService = emailService;
            _context = context;
        }

        public async Task CheckAndSendReminders()
        {
            var now = DateTime.Now;
            var fiveHoursFromNow = now.AddHours(5);
            
            // Get appointments happening in the next 5 hours
            var upcomingAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.AppointmentDate >= now &&
                          a.AppointmentDate <= fiveHoursFromNow &&
                          a.Status == "Confirmed")
                .ToListAsync();

            foreach (var appointment in upcomingAppointments)
            {
                var reminderKey = $"reminder_sent_{appointment.Id}";
                
                // Check if reminder was already sent
                if (!_cache.TryGetValue(reminderKey, out bool reminderSent))
                {
                    // Send email reminder
                    await SendAppointmentReminderEmail(appointment);
                    
                    // Mark as sent in cache (valid for 6 hours)
                    _cache.Set(reminderKey, true, TimeSpan.FromHours(6));
                }
            }
        }

        private async Task SendAppointmentReminderEmail(Appointment appointment)
        {
            string durationText = appointment.Duration switch
            {
                30 => "30 minutes",
                60 => "1 hour",
                90 => "1.5 hours",
                _ => $"{appointment.Duration} minutes"
            };

            // Calculate end time
            var endTime = appointment.AppointmentDate.AddMinutes(appointment.Duration);

            var subject = "Appointment Reminder - In 5 Hours";
            var message = $@"
                <h3>Appointment Reminder</h3>
                <p>Dear {appointment.Patient.Name},</p>
                <p>This is a reminder that you have an appointment in 5 hours:</p>
                <p><strong>Date:</strong> {appointment.AppointmentDate:MMMM dd, yyyy}</p>
                <p><strong>Time:</strong> {appointment.AppointmentDate:hh:mm tt} - {endTime:hh:mm tt}</p>
                <p><strong>Duration:</strong> {durationText}</p>
                <p><strong>Doctor:</strong> Dr. {appointment.Doctor.Name}</p>
                <p><strong>Specialty:</strong> {appointment.Doctor.Specialty}</p>
                <p>Please make sure to arrive on time for your appointment.</p>
                <br>
                <p>Thank you,<br>Doctor Appointment System</p>
            ";

            await _emailService.SendEmailAsync(appointment.Patient.Email, subject, message);
        }
    }
}