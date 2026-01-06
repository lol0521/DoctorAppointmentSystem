using DoctorAppointmentSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace DoctorAppointmentSystem.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalPatients { get; set; }
        public int ActiveDoctors { get; set; }
        public int TodaysAppointments { get; set; }
        public decimal MonthlyRevenue { get; set; }

        public List<AppointmentChartData> MonthlyAppointments { get; set; }
        public Dictionary<string, int> AppointmentStatuses { get; set; }

        public string[] MonthlyAppointmentLabels { get; set; }
        public int[] MonthlyAppointmentData { get; set; }
        public string[] AppointmentStatusLabels { get; set; }
        public int[] AppointmentStatusData { get; set; }

        public List<Appointment> RecentAppointments { get; set; }
    }

    public class AppointmentChartData
    {
        public string Month { get; set; }
        public int Count { get; set; }
    }
}
