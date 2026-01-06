using System;
using System.ComponentModel.DataAnnotations;

namespace DoctorAppointmentSystem.Models
{
    public class DoctorSchedule
    {
        public int Id { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        public bool IsAvailable { get; set; } = true;

        public Doctor? Doctor { get; set; }
    }
}