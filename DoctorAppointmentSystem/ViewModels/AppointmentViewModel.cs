using System;
using System.ComponentModel.DataAnnotations;

namespace DoctorAppointmentSystem.ViewModels
{
    public class AppointmentViewModel
    {
        [Required]
        [Display(Name = "Patient")]
        public int PatientId { get; set; }

        [Required]
        [Display(Name = "Doctor")]
        public int DoctorId { get; set; }

        [Required]
        [Display(Name = "Appointment Date")]
        [DataType(DataType.DateTime)]
        public DateTime AppointmentDate { get; set; } = DateTime.Now.AddDays(1);

        [Display(Name = "Notes")]
        public string? Notes { get; set; }
    }
}