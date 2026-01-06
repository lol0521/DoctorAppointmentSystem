using System.ComponentModel.DataAnnotations;

namespace DoctorAppointmentSystem.Models
{
    public class MedicalRecordViewModel
    {
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
    }
}