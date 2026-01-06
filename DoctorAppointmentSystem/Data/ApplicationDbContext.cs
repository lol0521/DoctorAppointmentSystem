using DoctorAppointmentSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace DoctorAppointmentSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<MedicalRecord> MedicalRecords { get; set; }
        public DbSet<UserAccount> UserAccounts { get; set; }
        public DbSet<DoctorSchedule> DoctorSchedules { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasDiscriminator<string>("Role")
                .HasValue<User>("User")
                .HasValue<Doctor>("Doctor")
                .HasValue<Patient>("Patient");

            // Avoid multiple cascade paths
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany()
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict); // or NoAction

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany()
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict); // or NoAction

            modelBuilder.Entity<Review>()
    .HasOne(r => r.Doctor)
    .WithMany()
    .HasForeignKey(r => r.DoctorId)
    .OnDelete(DeleteBehavior.Restrict); // or NoAction

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Patient)
                .WithMany()
                .HasForeignKey(r => r.PatientId)
                .OnDelete(DeleteBehavior.Restrict); // or NoAction

            modelBuilder.Entity<User>().HasData(
                new User { Id = 999, Name = "Admin", Email = "admin@gmail.com", Role = "Admin" }
                );
            modelBuilder.Entity<MedicalRecord>(entity =>
            {
                entity.Property(e => e.BloodType).HasMaxLength(10); // or appropriate length
            });

        }
    }
}
