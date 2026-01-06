using DoctorAppointmentSystem.Models;
using System.Collections.Generic;

namespace DoctorAppointmentSystem.ViewModels
{
    public class PatientAppointmentViewModel
    {
        public List<Appointment> UpcomingAppointments { get; set; } = new List<Appointment>();
        public List<Appointment> PastAppointments { get; set; } = new List<Appointment>();
        public List<Appointment> AllAppointments { get; set; } = new List<Appointment>();
        public string Filter { get; set; } = "upcoming";
    }
}