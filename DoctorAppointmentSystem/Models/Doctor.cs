using System.ComponentModel.DataAnnotations.Schema;

namespace DoctorAppointmentSystem.Models
{
    public class Doctor : User
    {
        public string? Specialty { get; set; }
    }
}