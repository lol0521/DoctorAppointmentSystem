// Controllers/DoctorController.cs
using DoctorAppointmentSystem.Data;
using DoctorAppointmentSystem.Models;
using DoctorAppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

namespace DoctorAppointmentSystem.Controllers
{
    public class DoctorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DoctorController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Doctor - Show all doctors
        public async Task<IActionResult> Index(string searchString, string specialtyFilter)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["SpecialtyFilter"] = specialtyFilter;

            var doctors = _context.Doctors.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                doctors = doctors.Where(d => d.Name.Contains(searchString) ||
                                           d.Specialty.Contains(searchString) ||
                                           d.Email.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(specialtyFilter))
            {
                doctors = doctors.Where(d => d.Specialty == specialtyFilter);
            }

            // Get distinct specialties for filter dropdown
            var specialties = await _context.Doctors
                .Select(d => d.Specialty)
                .Distinct()
                .ToListAsync();

            ViewBag.Specialties = new SelectList(specialties);

            return View(await doctors.ToListAsync());
        }

        // GET: /Doctor/Details/5 - Show doctor details and reviews
        public async Task<IActionResult> Details(int id)
        {
            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null)
            {
                return NotFound();
            }

            // Get reviews for this doctor
            var reviews = await _context.Reviews
                .Include(r => r.Patient)
                .Where(r => r.DoctorId == id)
                .OrderByDescending(r => r.Date)
                .ToListAsync();

            // Calculate average rating
            var averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
            var totalReviews = reviews.Count;

            var viewModel = new ReviewViewModel
            {
                DoctorId = doctor.Id,
                DoctorName = doctor.Name,
                ExistingReviews = reviews,
                AverageRating = Math.Round(averageRating, 1),
                TotalReviews = totalReviews
            };

            ViewBag.Doctor = doctor;
            return View(viewModel);
        }

        // POST: /Doctor/AddReview - Add a new review
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int DoctorId, int Rating, string Comment)
        {
            try
            {
                Console.WriteLine($"AddReview called: DoctorId={DoctorId}, Rating={Rating}, Comment={Comment}");

                if (!User.Identity.IsAuthenticated)
                {
                    TempData["ErrorMessage"] = "Please login to submit a review.";
                    return RedirectToAction("Details", new { id = DoctorId });
                }

                var userIdString = User.FindFirstValue("UserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                {
                    TempData["ErrorMessage"] = "User authentication error. Please login again.";
                    return RedirectToAction("Details", new { id = DoctorId });
                }

                // Check if user has a patient profile
                var userAccount = await _context.UserAccounts
                    .Include(ua => ua.Patient)
                    .FirstOrDefaultAsync(ua => ua.Id == userId);

                if (userAccount == null)
                {
                    TempData["ErrorMessage"] = "User account not found.";
                    return RedirectToAction("Details", new { id = DoctorId });
                }

                // Check if user has a patient profile
                if (userAccount.PatientId == null || userAccount.Patient == null)
                {
                    TempData["ErrorMessage"] = "You need to have a patient profile to leave reviews. Doctors cannot leave reviews.";
                    return RedirectToAction("Details", new { id = DoctorId });
                }

                var patient = userAccount.Patient;

                // Check if doctor exists
                var doctor = await _context.Doctors.FindAsync(DoctorId);
                if (doctor == null)
                {
                    TempData["ErrorMessage"] = "Doctor not found.";
                    return RedirectToAction("Details", new { id = DoctorId });
                }

                // Check if patient already reviewed this doctor
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.DoctorId == DoctorId && r.PatientId == patient.Id);

                if (existingReview != null)
                {
                    existingReview.Rating = Rating;
                    existingReview.Comment = Comment;
                    existingReview.Date = DateTime.Now;
                    _context.Reviews.Update(existingReview);
                }
                else
                {
                    var review = new Review
                    {
                        DoctorId = DoctorId,
                        PatientId = patient.Id,
                        Rating = Rating,
                        Comment = Comment,
                        Date = DateTime.Now
                    };
                    _context.Reviews.Add(review);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thank you for your review!";
            }
            catch (DbUpdateException dbEx)
            {
                var innerException = dbEx.InnerException?.Message ?? dbEx.Message;
                Console.WriteLine($"Database error: {innerException}");
                TempData["ErrorMessage"] = $"Database error: {innerException}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while saving your review.";
            }

            return RedirectToAction("Details", new { id = DoctorId });
        }

        [HttpGet]
        public IActionResult TestAuth()
        {
            var result = new StringBuilder();
            result.AppendLine($"Authenticated: {User.Identity.IsAuthenticated}");
            result.AppendLine($"User: {User.Identity.Name}");

            result.AppendLine("Claims:");
            foreach (var claim in User.Claims)
            {
                result.AppendLine($"{claim.Type}: {claim.Value}");
            }

            return Content(result.ToString());
        }
        [HttpGet]
        public async Task<IActionResult> DebugDatabase()
        {
            var result = new StringBuilder();

            try
            {
                // Check if we can connect
                result.AppendLine($"Database connected: {await _context.Database.CanConnectAsync()}");

                // Check tables
                result.AppendLine($"Doctors count: {await _context.Doctors.CountAsync()}");
                result.AppendLine($"Patients count: {await _context.Patients.CountAsync()}");
                result.AppendLine($"Reviews count: {await _context.Reviews.CountAsync()}");

                // Check specific doctor and patient
                var firstDoctor = await _context.Doctors.FirstOrDefaultAsync();
                var firstPatient = await _context.Patients.FirstOrDefaultAsync();

                result.AppendLine($"First doctor: {firstDoctor?.Id} - {firstDoctor?.Name}");
                result.AppendLine($"First patient: {firstPatient?.Id} - {firstPatient?.Name}");

                // Check review table structure
                var reviews = await _context.Reviews.ToListAsync();
                foreach (var review in reviews.Take(5))
                {
                    result.AppendLine($"Review: ID={review.Id}, Doctor={review.DoctorId}, Patient={review.PatientId}, Rating={review.Rating}");
                }
            }
            catch (Exception ex)
            {
                result.AppendLine($"Error: {ex.Message}");
            }

            return Content(result.ToString());
        }
    }
}