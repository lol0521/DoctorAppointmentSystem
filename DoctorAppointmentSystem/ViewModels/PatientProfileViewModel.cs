using System.ComponentModel.DataAnnotations;

namespace DoctorAppointmentSystem.ViewModels
{
    public class PatientProfileViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Username")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 20 characters")]
        [RegularExpression(@"^\S+$", ErrorMessage = "Username cannot contain spaces")]
        public string Username { get; set; } = string.Empty;

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

        [Display(Name = "Address")]
        public string? Address { get; set; }

        [Display(Name = "Gender")]
        public string? Gender { get; set; }

        [Required]
        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Display(Name = "Profile Image")]
        public string? ProfileImage { get; set; }

        [Display(Name = "New Profile Image")]
        public IFormFile? NewProfileImage { get; set; }

        [Display(Name = "Remove Profile Image")]
        public bool RemoveProfileImage { get; set; }

        [Display(Name = "Latitude")]
        public double? SelectedLat { get; set; }

        [Display(Name = "Longitude")]
        public double? SelectedLng { get; set; }
        // Statistics
        public int TotalAppointments { get; set; }
        public int UpcomingAppointments { get; set; }
        public int CompletedAppointments { get; set; }
    }
}