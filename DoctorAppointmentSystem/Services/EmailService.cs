using DoctorAppointmentSystem.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System;

namespace DoctorAppointmentSystem.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            using (var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort))
            {
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.SenderPassword);
                client.EnableSsl = _emailSettings.EnableSsl;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, "Doctor Appointment System"),
                    Subject = subject,
                    Body = message,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
            }
        }

        // --- payment email ---
        public async Task SendPaymentConfirmationEmailAsync(string patientEmail, string patientName, decimal amount, Appointment appointment)
        {
            var subject = "Your Payment Confirmation & Appointment Details";

            // Create a nice HTML body for the email
            var body = $@"
                <div style='font-family: Arial, sans-serif; color: #333;'>
                    <h2>Payment Successful!</h2>
                    <p>Dear {patientName},</p>
                    <p>Thank you for your payment. Your appointment is confirmed.</p>
                    <hr>
                    <h3>Payment Receipt</h3>
                    <p><strong>Amount Paid:</strong> ${amount.ToString("0.00")}</p>
                    <p><strong>Payment Date:</strong> {DateTime.Now.ToString("dddd, MMMM dd, yyyy hh:mm tt")}</p>
                    <hr>
                    <h3>Appointment Details</h3>
                    <p><strong>Doctor:</strong> Dr. {appointment.Doctor?.Name}</p>
                    <p><strong>Date & Time:</strong> {appointment.AppointmentDate.ToString("dddd, MMMM dd, yyyy hh:mm tt")}</p>
                    <p>We look forward to seeing you.</p>
                    <br>
                    <p>Sincerely,<br>The Medical Care Team</p>
                </div>";

            // Call your original method to send the email
            await SendEmailAsync(patientEmail, subject, body);
        }
    }
}