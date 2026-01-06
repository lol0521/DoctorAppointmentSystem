using System.ComponentModel.DataAnnotations;

namespace DoctorAppointmentSystem.ViewModels
{
    public class DoctorProfileViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Username")]
        [RegularExpression(@"^\S+$", ErrorMessage = "Username cannot contain spaces.")]
        public string Username { get; set; }

        [Required]
        [Display(Name = "Full Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^\+?[0-9]{10,15}$", ErrorMessage = "Phone number must be 10–15 digits and may start with +")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Gender")]
        public string? Gender { get; set; }

        [Required]
        [Display(Name = "Specialty")]
        public string Specialty { get; set; } = string.Empty;

        [Display(Name = "Profile Image")]
        public string? ProfileImage { get; set; }

        [Display(Name = "New Profile Image")]
        public IFormFile? NewProfileImage { get; set; }

        [Display(Name = "Remove Profile Image")]
        public bool RemoveProfileImage { get; set; }

        // Statistics
        public int TotalAppointments { get; set; }
        public int UpcomingAppointments { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
    }
}