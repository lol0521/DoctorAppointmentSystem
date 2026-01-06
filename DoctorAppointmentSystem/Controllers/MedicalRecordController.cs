using DoctorAppointmentSystem.Data;
using DoctorAppointmentSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace DoctorAppointmentSystem.Controllers
{
    [Authorize(Roles = "Patient")] // Only logged-in patients can submit
    public class MedicalRecordController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MedicalRecordController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult SetAccessFlag()
        {
            HttpContext.Session.SetString("AllowMedicalRecord", "true");
            return RedirectToAction("BookNow");
        }

        // GET: Show form
        public IActionResult BookNow()
        {
            // Check if access is allowed
            if (HttpContext.Session.GetString("AllowMedicalRecord") != "true")
            {
                TempData["Error"] = "You cannot access this page directly.";
                return RedirectToAction("Index", "Home");
            }

            // Clear the flag so it can't be reused without clicking "Book Now" again
            HttpContext.Session.Remove("AllowMedicalRecord");

            var userAccountIdClaim = User.FindFirst("UserId");
            if (userAccountIdClaim == null || !int.TryParse(userAccountIdClaim.Value, out int userAccountId))
            {
                TempData["Error"] = "Cannot find logged-in user. Please login again.";
                return RedirectToAction("Index", "Home");
            }

            var patient = _context.UserAccounts
                .Include(ua => ua.Patient)
                .ThenInclude(p => p.MedicalRecords)
                .FirstOrDefault(ua => ua.Id == userAccountId)?.Patient;

            if (patient == null)
            {
                TempData["Error"] = "Patient record not found.";
                return RedirectToAction("Index", "Home");
            }

            var existingRecord = _context.MedicalRecords
                .FirstOrDefault(mr => mr.PatientId == patient.Id);

            var model = existingRecord != null
                ? new MedicalRecordViewModel
                {
                    BloodType = existingRecord.BloodType,
                    Height = existingRecord.Height,
                    Weight = existingRecord.Weight,
                    Allergies = existingRecord.Allergies,
                    CurrentMedications = existingRecord.CurrentMedications,
                    PastMedicalHistory = existingRecord.PastMedicalHistory
                }
                : new MedicalRecordViewModel();

            return View("~/Views/MedicalRecord/MedicalRecordForm.cshtml", model);
        }

        // POST: Save or update medical record
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitMedicalRecord(MedicalRecordViewModel model, string action)
        {
            // Only handle Continue now
            var userAccountIdClaim = User.FindFirst("UserId");
            if (userAccountIdClaim == null || !int.TryParse(userAccountIdClaim.Value, out int userAccountId))
            {
                ModelState.AddModelError(string.Empty, "Cannot find logged-in user. Please login again.");
                return View("~/Views/MedicalRecord/MedicalRecordForm.cshtml", model);
            }

            var patient = _context.UserAccounts
                .Include(ua => ua.Patient)
                .FirstOrDefault(ua => ua.Id == userAccountId)?.Patient;

            if (patient == null)
            {
                ModelState.AddModelError(string.Empty, "Patient record not found.");
                return View("~/Views/MedicalRecord/MedicalRecordForm.cshtml", model);
            }

            // Only validate and save for Continue
            if (!ModelState.IsValid)
                return View("~/Views/MedicalRecord/MedicalRecordForm.cshtml", model);

            var medicalRecord = _context.MedicalRecords.FirstOrDefault(mr => mr.PatientId == patient.Id);
            if (medicalRecord != null)
            {
                medicalRecord.BloodType = model.BloodType;
                medicalRecord.Height = model.Height;
                medicalRecord.Weight = model.Weight;
                medicalRecord.Allergies = model.Allergies;
                medicalRecord.CurrentMedications = model.CurrentMedications;
                medicalRecord.PastMedicalHistory = model.PastMedicalHistory;
                medicalRecord.UpdatedDate = DateTime.Now;
            }
            else
            {
                _context.MedicalRecords.Add(new MedicalRecord
                {
                    PatientId = patient.Id,
                    BloodType = model.BloodType,
                    Height = model.Height,
                    Weight = model.Weight,
                    Allergies = model.Allergies,
                    CurrentMedications = model.CurrentMedications,
                    PastMedicalHistory = model.PastMedicalHistory,
                    CreatedDate = DateTime.Now
                });
            }

            _context.SaveChanges();
            return RedirectToAction("Step1", "Appointment");
        }
    }
}