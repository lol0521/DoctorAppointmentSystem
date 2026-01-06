using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoctorAppointmentSystem.Models
{
    public class Appointment
    {
        public int Id { get; set; } // Primary Key

        [Required]
        public int DoctorId { get; set; } // FK to Doctor

        [Required]
        public int PatientId { get; set; } // FK to Patient

        [Required]
        public DateTime AppointmentDate { get; set; } // Date and time of appointment

        public int Duration { get; set; }

        public string? Status { get; set; } // Pending, Confirmed, Completed, Cancelled

        public string? Notes { get; set; } // Optional notes

        // Navigation properties
        public Doctor? Doctor { get; set; }
        public Patient? Patient { get; set; }
    }
}