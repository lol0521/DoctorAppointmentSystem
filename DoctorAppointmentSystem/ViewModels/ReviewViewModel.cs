using DoctorAppointmentSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace DoctorAppointmentSystem.ViewModels
{
    public class ReviewViewModel
    {
        public int DoctorId { get; set; }
        public string DoctorName { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [MaxLength(500)]
        public string? Comment { get; set; }

        public List<Review> ExistingReviews { get; set; } = new List<Review>();
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
    }
}