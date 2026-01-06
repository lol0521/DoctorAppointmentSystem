using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoctorAppointmentSystem.Models
{
    public class MedicalRecord
    {
        [Key]
        public int Id { get; set; } // Primary Key

        // Foreign Key to Patient
        [ForeignKey("Patient")]
        public int PatientId { get; set; }

        [Required(ErrorMessage = "Blood type is required.")]
        [Display(Name = "Blood Type")]
        public string BloodType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Height is required.")]
        [Range(70, 250, ErrorMessage = "Height must be between 70 and 250 cm.")]
        [Display(Name = "Height (cm)")]
        public decimal Height { get; set; }

        [Required(ErrorMessage = "Weight is required.")]
        [Range(15, 200, ErrorMessage = "Weight must be between 15 and 200 kg.")]
        [Display(Name = "Weight (kg)")]
        public decimal Weight { get; set; }

        [Display(Name = "Known Allergies")]
        public string? Allergies { get; set; }

        [Display(Name = "Current Medications")]
        public string? CurrentMedications { get; set; }

        [Display(Name = "Past Medical History")]
        public string? PastMedicalHistory { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? UpdatedDate { get; set; }

        public Patient? Patient { get; set; }
    }
}