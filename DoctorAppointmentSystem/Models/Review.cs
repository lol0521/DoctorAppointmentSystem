using System;
using System.ComponentModel.DataAnnotations;

namespace DoctorAppointmentSystem.Models
{
    public class Review
    {
        public int Id { get; set; } // Primary Key

        [Required]
        public int DoctorId { get; set; } // FK to Doctor

        [Required]
        public int PatientId { get; set; } // FK to Patient

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; } // 1 to 5 stars

        public string? Comment { get; set; } // Optional text feedback

        public DateTime Date { get; set; } = DateTime.Now; // When review was posted

        // Navigation properties
        public Doctor? Doctor { get; set; }
        public Patient? Patient { get; set; }
    }
}