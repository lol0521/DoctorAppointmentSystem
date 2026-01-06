using System;
using System.ComponentModel.DataAnnotations;

namespace DoctorAppointmentSystem.ViewModels
{
    public class PaymentInvoiceViewModel
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public string Status { get; set; }
        public DateTime PaymentDate { get; set; }
        public string TransactionId { get; set; }

        // Appointment details
        public int AppointmentId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string AppointmentNotes { get; set; }

        // Patient details
        public string PatientName { get; set; }
        public string PatientEmail { get; set; }
        public string PatientPhone { get; set; }
        public string PatientAddress { get; set; }

        // Doctor details
        public string DoctorName { get; set; }
        public string DoctorSpecialty { get; set; }
        public string DoctorEmail { get; set; }
        public string DoctorPhone { get; set; }

        // Clinic details
        public string ClinicName { get; set; } = "MediCare Health Clinic";
        public string ClinicAddress { get; set; } = "123 Healthcare Street, Medical District";
        public string ClinicCity { get; set; } = "Kuala Lumpur";
        public string ClinicPhone { get; set; } = "+60 3-1234 5678";
        public string ClinicEmail { get; set; } = "info@medicare-clinic.com";

        // Calculated properties
        public string FormattedAmount => Amount.ToString("C");
        public string FormattedPaymentDate => PaymentDate.ToString("dd MMMM yyyy");
        public string FormattedAppointmentDate => AppointmentDate.ToString("dd MMMM yyyy hh:mm tt");
    }
}