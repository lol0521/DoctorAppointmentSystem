using DoctorAppointmentSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace DoctorAppointmentSystem.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class DoctorReviewController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DoctorReviewController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /DoctorReview/
        public async Task<IActionResult> Index()
        {
            // Get logged-in doctor based on UserAccount
            var userId = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userAccountId))
            {
                return RedirectToAction("Login", "Account");
            }

            var userAccount = await _context.UserAccounts
                .Include(u => u.Doctor)
                .FirstOrDefaultAsync(u => u.Id == userAccountId);

            if (userAccount?.Doctor == null)
            {
                return Unauthorized();
            }

            int doctorId = userAccount.Doctor.Id;

            // Fetch reviews for this doctor
            var reviews = await _context.Reviews
                .Where(r => r.DoctorId == doctorId)
                .Include(r => r.Patient)
                .OrderByDescending(r => r.Date)
                .ToListAsync();

            return View("DoctorReview", reviews);
        }
    }
}