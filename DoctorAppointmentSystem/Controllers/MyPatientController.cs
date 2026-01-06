using DoctorAppointmentSystem.Data;
using DoctorAppointmentSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace DoctorAppointmentSystem.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class MyPatientController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MyPatientController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Show all patients of logged-in doctor
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userAccountId))
                return RedirectToAction("Login", "Account");

            var userAccount = await _context.UserAccounts
                .Include(ua => ua.Doctor)
                .FirstOrDefaultAsync(ua => ua.Id == userAccountId);

            if (userAccount?.Doctor == null)
                return Unauthorized();

            int doctorId = userAccount.Doctor.Id;

            var patients = await _context.Appointments
                .Where(a => a.DoctorId == doctorId)
                .Include(a => a.Patient)
                .ThenInclude(p => p.MedicalRecords)
                .Select(a => a.Patient)
                .Distinct()
                .ToListAsync();

            return View(patients);
        }

        // Show medical records of a specific patient
        public async Task<IActionResult> MedicalRecords(int id)
        {
            var records = await _context.MedicalRecords
                .Include(mr => mr.Patient)
                .Where(mr => mr.PatientId == id)
                .ToListAsync();

            if (!records.Any())
                TempData["Info"] = "No medical records found for this patient.";

            return View(records);
        }

        // View & Edit a single record
        public async Task<IActionResult> RecordDetails(int id)
        {
            var record = await _context.MedicalRecords
                .Include(mr => mr.Patient)
                .FirstOrDefaultAsync(mr => mr.Id == id);

            if (record == null)
            {
                TempData["Error"] = "Medical record not found.";
                return RedirectToAction("Index");
            }

            return View(record);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditSimulate(MedicalRecord model)
        {
            TempData["Success"] = "Changes simulated. (Not saved to database)";
            return RedirectToAction("RecordDetails", new { id = model.Id });
        }
    }
}