using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace DoctorAppointmentSystem.ViewModels
{
    public class DoctorViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        [Remote(action: "VerifyEmail", controller: "Admin")]
        public string Email { get; set; }

        [Required]
        [Phone]
        [Remote(action: "VerifyPhone", controller: "Admin")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Gender")]
        public string? Gender { get; set; }

        [Display(Name = "Specialty")]
        public string? Specialty { get; set; }

        [Display(Name = "Profile Image URL")]
        public string? ProfileImage { get; set; }


    }
}