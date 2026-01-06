using DoctorAppointmentSystem.Models;
using System.Collections.Generic;

namespace DoctorAppointmentSystem.ViewModels
{
    public class ScheduleViewModel
    {
        public List<Appointment> Appointments { get; set; } = new List<Appointment>();
        public List<DoctorSchedule> DoctorSchedules { get; set; } = new List<DoctorSchedule>();
        public DateTime SelectedDate { get; set; } = DateTime.Today;
        public int DoctorId { get; set; }
    }

    public class CalendarEvent
    {
        public string Title { get; set; } = string.Empty;
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Color { get; set; } = "#007bff";
        public int AppointmentId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}