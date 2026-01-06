using DoctorAppointmentSystem.Models;
using System.Collections.Generic;

namespace DoctorAppointmentSystem.ViewModels
{
    public class DoctorDashboardViewModel
    {
        public int TotalAppointments { get; set; }
        public int TotalPatients { get; set; }
        public List<Appointment> TodayAppointments { get; set; } = new List<Appointment>();
        public List<Appointment> UpcomingAppointments { get; set; } = new List<Appointment>();
        public List<DoctorSchedule> TodaySchedule { get; set; } = new List<DoctorSchedule>();
    }

    public class PatientDashboardViewModel
    {
        public int TotalAppointments { get; set; }
        public List<Appointment> UpcomingAppointments { get; set; } = new List<Appointment>();
        public List<MedicalRecord> RecentMedicalRecords { get; set; } = new List<MedicalRecord>();
        public List<Payment> PendingPayments { get; set; } = new List<Payment>();
    }
}