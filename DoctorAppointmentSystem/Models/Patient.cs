using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;

namespace DoctorAppointmentSystem.Models
{
    public class Patient : User
    {
        public DateTime DateOfBirth { get; set; }

        [MaxLength(200)]
        public string? Address { get; set; }

        public ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
    }
}