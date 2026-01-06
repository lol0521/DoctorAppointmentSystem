using DoctorAppointmentSystem.Models;
using System.Threading.Tasks;

namespace DoctorAppointmentSystem.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string message);

        Task SendPaymentConfirmationEmailAsync(string patientEmail, string patientName, decimal amount, Appointment appointment);
    }
}