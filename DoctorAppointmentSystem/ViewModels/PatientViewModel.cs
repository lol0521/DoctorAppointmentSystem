using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;

namespace DoctorAppointmentSystem.ViewModels
{
    public class PatientViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        [Remote(action: "VerifyPatientEmail", controller: "Admin")]
        public string Email { get; set; }

        [Required]
        [Phone]
        [Remote(action: "VerifyPatientPhone", controller: "Admin")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Gender")]
        public string? Gender { get; set; }

        [Required]
        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; } = DateTime.Now.AddYears(-20);

        [Display(Name = "Profile Image URL")]
        public string? ProfileImage { get; set; }

        [Display(Name = "Address")]
        public string? Address { get; set; }
    }
}