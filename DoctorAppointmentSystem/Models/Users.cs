using System.ComponentModel.DataAnnotations;

namespace DoctorAppointmentSystem.Models
{
    public class User
    {
        public int Id { get; set; } // Primary Key

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public string? Gender { get; set; }

        [Required]
        public string Role { get; set; } = "Patient"; // Admin, Doctor, Patient //if empty then patient
        public string? ProfileImage { get; set; } // Path to profile photo (optional)
    }
}
