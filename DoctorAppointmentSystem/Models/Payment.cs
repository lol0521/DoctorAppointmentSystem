using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoctorAppointmentSystem.Models
{
    public class Payment
    {
        public int Id { get; set; } // Primary Key

        [Required]
        public int AppointmentId { get; set; } // FK to Appointment

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; } // Payment amount

        public string? PaymentMethod { get; set; }

        public DateTime PaymentDate { get; set; } // Date paid

        public string? Status { get; set; } // Pending, Paid, Failed

        // Navigation property
        public Appointment? Appointment { get; set; }
        public string? TransactionId { get; internal set; }
    }
}
