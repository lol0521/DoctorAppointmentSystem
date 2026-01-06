using System.ComponentModel.DataAnnotations;

namespace DoctorAppointmentSystem.Models
{
    public class UserAccount
    {
        public int Id { get; set; } // Primary Key

        [Required]
        public string Username { get; set; } = string.Empty; // Login username

        [Required]
        public string PasswordHash { get; set; } = string.Empty; // Hashed password

        [Required]
        public string Role { get; set; } = "Patient"; // Admin, Doctor, Patient

        public int? DoctorId { get; set; } // FK if user is Doctor
        public int? PatientId { get; set; } // FK if user is Patient

        // Navigation properties
        public Doctor? Doctor { get; set; }
        public Patient? Patient { get; set; }
    }
}