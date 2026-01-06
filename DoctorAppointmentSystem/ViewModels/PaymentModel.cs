using System.ComponentModel.DataAnnotations;

namespace DoctorAppointmentSystem.Models
{
    public class PaymentModel
    {
        [Required]
        [Display(Name = "Cardholder Name")]
        public string? CardholderName { get; set; }

        [Required(ErrorMessage = "Card number is required.")]
        [Display(Name = "Card Number")]
        // Removed [CreditCard] and allowed any digits (basic placeholder check optional)
        [RegularExpression(@"^\d{8,19}$", ErrorMessage = "Card number must be between 8–19 digits.")]
        public string? CardNumber { get; set; }

        [Required]
        [Display(Name = "Expiration Date")]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/?([0-9]{2})$", ErrorMessage = "Please enter a valid expiration date (MM/YY)")]
        public string? ExpirationDate { get; set; }

        [Required]
        [Display(Name = "CVV")]
        [RegularExpression(@"^[0-9]{3,4}$", ErrorMessage = "Please enter a valid CVV")]
        public string? CVV { get; set; }

        [Required]
        [Display(Name = "Amount")]
        [Range(1, 10000, ErrorMessage = "Amount must be between $1 and $10,000")]
        public decimal Amount { get; set; }

        [Required]
        [Display(Name = "Appointment ID")]
        public int AppointmentId { get; set; }
    }
}