using DoctorAppointmentSystem.Data;
using DoctorAppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace DoctorAppointmentSystem.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProfileController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // AJAX method to check phone availability for profile update
        [AcceptVerbs("GET", "POST")]
        [Authorize]
        public async Task<IActionResult> CheckProfilePhoneAvailability(string phoneNumber)
        {
            var userId = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userAccountId))
            {
                return Json(new { available = false });
            }

            // Get the current user's entity ID (Doctor or Patient)
            var userAccount = await _context.UserAccounts
                .FirstOrDefaultAsync(ua => ua.Id == userAccountId);

            if (userAccount == null)
            {
                return Json(new { available = false });
            }

            // Check Doctors table (excluding current user if they are a doctor)
            var doctorPhoneExists = await _context.Doctors
                .AnyAsync(d => d.PhoneNumber == phoneNumber &&
                              (userAccount.DoctorId == null || d.Id != userAccount.DoctorId));

            // Check Patients table (excluding current user if they are a patient)
            var patientPhoneExists = await _context.Patients
                .AnyAsync(p => p.PhoneNumber == phoneNumber &&
                              (userAccount.PatientId == null || p.Id != userAccount.PatientId));

            return Json(new { available = !(doctorPhoneExists || patientPhoneExists) });
        }

        // AJAX method to check name and phone combination
        [AcceptVerbs("GET", "POST")]
        [Authorize]
        public async Task<IActionResult> CheckProfileUsernameAvailability(string username)
        {
            var userId = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userAccountId))
            {
                return Json(new { available = false });
            }

            var exists = await _context.UserAccounts
                .AnyAsync(u => u.Username == username && u.Id != userAccountId);

            return Json(new { available = !exists });
        }

        // AJAX method to check email availability for profile update
        [AcceptVerbs("GET", "POST")]
        [Authorize]
        public async Task<IActionResult> CheckProfileEmailAvailability(string email)
        {
            var userId = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userAccountId))
            {
                return Json(new { available = false });
            }

            // Get the current user's entity ID (Doctor or Patient)
            var userAccount = await _context.UserAccounts
                .FirstOrDefaultAsync(ua => ua.Id == userAccountId);

            if (userAccount == null)
            {
                return Json(new { available = false });
            }

            // Check Doctors table (excluding current user if they are a doctor)
            var doctorEmailExists = await _context.Doctors
                .AnyAsync(d => d.Email == email &&
                              (userAccount.DoctorId == null || d.Id != userAccount.DoctorId));

            // Check Patients table (excluding current user if they are a patient)
            var patientEmailExists = await _context.Patients
                .AnyAsync(p => p.Email == email &&
                              (userAccount.PatientId == null || p.Id != userAccount.PatientId));

            return Json(new { available = !(doctorEmailExists || patientEmailExists) });
        }

        // GET: /Profile (redirects to appropriate profile based on role)
        public IActionResult Index()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            return role switch
            {
                "Doctor" => RedirectToAction("Doctor"),
                "Patient" => RedirectToAction("Patient"),
                _ => RedirectToAction("Index", "Home")
            };
        }

        // GET: /Profile/Doctor
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> Doctor()
        {
            var userId = User.FindFirst("UserId")?.Value;

            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userAccountId))
            {
                return RedirectToAction("Login", "Account");
            }

            var userAccount = await _context.UserAccounts
                .Include(ua => ua.Doctor)
                .FirstOrDefaultAsync(ua => ua.Id == userAccountId);

            if (userAccount == null || userAccount.Doctor == null)
            {
                return NotFound("Doctor profile not found");
            }

            var viewModel = new DoctorProfileViewModel
            {
                Id = userAccount.Doctor.Id,
                Username = userAccount.Username,
                Name = userAccount.Doctor.Name,
                Email = userAccount.Doctor.Email,
                PhoneNumber = userAccount.Doctor.PhoneNumber,
                Gender = userAccount.Doctor.Gender,
                Specialty = userAccount.Doctor.Specialty,
                ProfileImage = userAccount.Doctor.ProfileImage ?? "/images/profile-icon.png"
            };

            return View(viewModel);
        }

        // POST: /Profile/Doctor (Edit)
        [HttpPost]
        [Authorize(Roles = "Doctor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Doctor(DoctorProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userAccountId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var userAccount = await _context.UserAccounts
                    .Include(ua => ua.Doctor)
                    .FirstOrDefaultAsync(ua => ua.Id == userAccountId && ua.DoctorId == model.Id);

                if (userAccount == null || userAccount.Doctor == null)
                {
                    return Forbid();
                }

                // Centralized validation
                await ValidateDoctorProfileUpdate(model, userAccountId);
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                userAccount.Username = model.Username;
                var doctor = userAccount.Doctor;
                doctor.Name = model.Name;
                doctor.Email = model.Email;
                doctor.PhoneNumber = model.PhoneNumber;
                doctor.Gender = model.Gender;
                doctor.Specialty = model.Specialty;

                var uploadedFile = HttpContext.Request.Form.Files["NewProfileImage"];
                if (uploadedFile != null && uploadedFile.Length > 0)
                {
                    if (!string.IsNullOrEmpty(doctor.ProfileImage) && doctor.ProfileImage != "/images/profile-icon.png")
                    {
                        DeleteProfileImage(doctor.ProfileImage);
                    }
                    doctor.ProfileImage = await SaveProfileImage(uploadedFile);
                    UpdateProfileImageClaim(doctor.ProfileImage);
                }

                _context.UserAccounts.Update(userAccount);
                _context.Doctors.Update(doctor);
                await _context.SaveChangesAsync();
                UpdateUsernameClaim(model.Username);

                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction("Doctor");
            }
            return View(model);
        }

        // POST: /Profile/RemoveDoctorImage
        [HttpPost]
        [Authorize(Roles = "Doctor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveDoctorImage()
        {
            var userId = User.FindFirst("UserId")?.Value;

            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userAccountId))
            {
                return Json(new { success = false, message = "User not found" });
            }

            var userAccount = await _context.UserAccounts
                .Include(ua => ua.Doctor)
                .FirstOrDefaultAsync(ua => ua.Id == userAccountId);

            if (userAccount?.Doctor == null)
            {
                return Json(new { success = false, message = "Doctor profile not found" });
            }

            // Delete the old image file if it exists and is not the default
            if (!string.IsNullOrEmpty(userAccount.Doctor.ProfileImage) &&
                userAccount.Doctor.ProfileImage != "/images/profile-icon.png")
            {
                DeleteProfileImage(userAccount.Doctor.ProfileImage);
            }

            // Set to default avatar
            userAccount.Doctor.ProfileImage = "/images/profile-icon.png";

            _context.Doctors.Update(userAccount.Doctor);
            await _context.SaveChangesAsync();

            // Update claims
            UpdateProfileImageClaim("/images/profile-icon.png");

            return Json(new { success = true, message = "Profile image removed successfully", imageUrl = "/images/profile-icon.png" });
        }

        // GET: /Profile/Patient
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> Patient()
        {
            var userId = User.FindFirst("UserId")?.Value;

            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userAccountId))
            {
                return RedirectToAction("Login", "Account");
            }

            var userAccount = await _context.UserAccounts
                .Include(ua => ua.Patient)
                .FirstOrDefaultAsync(ua => ua.Id == userAccountId);

            if (userAccount == null || userAccount.Patient == null)
            {
                return NotFound("Patient profile not found");
            }

            var viewModel = new PatientProfileViewModel
            {
                Id = userAccount.Patient.Id,
                Username = userAccount.Username,
                Name = userAccount.Patient.Name,
                Email = userAccount.Patient.Email,
                PhoneNumber = userAccount.Patient.PhoneNumber,
                Address = userAccount.Patient.Address,
                Gender = userAccount.Patient.Gender,
                DateOfBirth = userAccount.Patient.DateOfBirth,
                ProfileImage = userAccount.Patient.ProfileImage ?? "/images/profile-icon.png"
            };

            return View(viewModel);
        }

        // POST: /Profile/Patient (Edit)
        [HttpPost]
        [Authorize(Roles = "Patient")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Patient(PatientProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userAccountId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var userAccount = await _context.UserAccounts
                    .Include(ua => ua.Patient)
                    .FirstOrDefaultAsync(ua => ua.Id == userAccountId && ua.PatientId == model.Id);

                if (userAccount == null || userAccount.Patient == null)
                {
                    return Forbid();
                }

                // Centralized validation
                await ValidatePatientProfileUpdate(model, userAccountId);
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                userAccount.Username = model.Username;
                var patient = userAccount.Patient;
                patient.Name = model.Name;
                patient.Email = model.Email;
                patient.PhoneNumber = model.PhoneNumber;
                patient.Address = model.Address;
                patient.Gender = model.Gender;
                patient.DateOfBirth = model.DateOfBirth;

                var uploadedFile = HttpContext.Request.Form.Files["NewProfileImage"];
                if (uploadedFile != null && uploadedFile.Length > 0)
                {
                    if (!string.IsNullOrEmpty(patient.ProfileImage) && patient.ProfileImage != "/images/profile-icon.png")
                    {
                        DeleteProfileImage(patient.ProfileImage);
                    }
                    patient.ProfileImage = await SaveProfileImage(uploadedFile);
                    UpdateProfileImageClaim(patient.ProfileImage);
                }

                _context.UserAccounts.Update(userAccount);
                _context.Patients.Update(patient);
                await _context.SaveChangesAsync();
                UpdateUsernameClaim(model.Username);

                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction("Patient");
            }
            return View(model);
        }

        // POST: /Profile/RemovePatientImage
        [HttpPost]
        [Authorize(Roles = "Patient")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemovePatientImage()
        {
            var userId = User.FindFirst("UserId")?.Value;

            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userAccountId))
            {
                return Json(new { success = false, message = "User not found" });
            }

            var userAccount = await _context.UserAccounts
                .Include(ua => ua.Patient)
                .FirstOrDefaultAsync(ua => ua.Id == userAccountId);

            if (userAccount?.Patient == null)
            {
                return Json(new { success = false, message = "Patient profile not found" });
            }

            // Delete the old image file if it exists and is not the default
            if (!string.IsNullOrEmpty(userAccount.Patient.ProfileImage) &&
                userAccount.Patient.ProfileImage != "/images/profile-icon.png")
            {
                DeleteProfileImage(userAccount.Patient.ProfileImage);
            }

            // Set to default avatar
            userAccount.Patient.ProfileImage = "/images/profile-icon.png";

            _context.Patients.Update(userAccount.Patient);
            await _context.SaveChangesAsync();

            // Update claims
            UpdateProfileImageClaim("/images/profile-icon.png");

            return Json(new { success = true, message = "Profile image removed successfully", imageUrl = "/images/profile-icon.png" });
        }

        public async Task<string> SaveProfileImage(IFormFile profileImage)
        {
            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            var extension = Path.GetExtension(profileImage.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
            {
                throw new ArgumentException("Invalid file type. Please upload an image file.");
            }

            // Validate file size (max 2MB)
            if (profileImage.Length > 2 * 1024 * 1024)
            {
                throw new ArgumentException("File size too large. Maximum allowed size is 2MB.");
            }

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "profiles");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + extension;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await profileImage.CopyToAsync(fileStream);
            }

            return $"/uploads/profiles/{uniqueFileName}";
        }

        private void DeleteProfileImage(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) || imagePath == "/images/profile-icon.png")
                return;

            try
            {
                var fullPath = Path.Combine(_environment.WebRootPath, imagePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - we don't want image deletion failure to break the app
                Console.WriteLine($"Error deleting profile image: {ex.Message}");
            }
        }

        private void UpdateProfileImageClaim(string imageUrl)
        {
            var identity = (ClaimsIdentity)User.Identity;
            var profileImageClaim = identity.FindFirst("ProfileImage");
            if (profileImageClaim != null)
            {
                identity.RemoveClaim(profileImageClaim);
            }
            identity.AddClaim(new Claim("ProfileImage", imageUrl ?? "/images/profile-icon.png"));
        }

        private void UpdateUsernameClaim(string username)
        {
            var identity = (ClaimsIdentity)User.Identity;
            var usernameClaim = identity.FindFirst(ClaimTypes.Name);
            if (usernameClaim != null)
            {
                identity.RemoveClaim(usernameClaim);
            }
            identity.AddClaim(new Claim(ClaimTypes.Name, username));
        }

        private bool DoctorExists(int id)
        {
            return _context.Doctors.Any(e => e.Id == id);
        }

        private bool PatientExists(int id)
        {
            return _context.Patients.Any(e => e.Id == id);
        }

        private async Task ValidateDoctorProfileUpdate(DoctorProfileViewModel model, int currentUserId)
        {
            // Check if username already exists (excluding current user)
            var usernameExists = await _context.UserAccounts
                .AnyAsync(ua => ua.Username == model.Username && ua.Id != currentUserId);

            if (usernameExists)
            {
                ModelState.AddModelError("Username", "Username is already taken.");
            }

            // Check if email already exists in Doctors table (excluding current doctor)
            var doctorEmailExists = await _context.Doctors
                .AnyAsync(d => d.Email == model.Email && d.Id != model.Id);

            // Check if email already exists in Patients table
            var patientEmailExists = await _context.Patients
                .AnyAsync(p => p.Email == model.Email);

            if (doctorEmailExists || patientEmailExists)
            {
                ModelState.AddModelError("Email", "Email address is already registered by another user.");
            }

            // Check if phone number already exists in Doctors table (excluding current doctor)
            var doctorPhoneExists = await _context.Doctors
                .AnyAsync(d => d.PhoneNumber == model.PhoneNumber && d.Id != model.Id);

            // Check if phone number already exists in Patients table
            var patientPhoneExists = await _context.Patients
                .AnyAsync(p => p.PhoneNumber == model.PhoneNumber);

            if (doctorPhoneExists || patientPhoneExists)
            {
                ModelState.AddModelError("PhoneNumber", "Phone number is already registered by another user.");
            }

            // Check if name and phone combination already exists in Doctors table (excluding current doctor)
            var doctorNamePhoneExists = await _context.Doctors
                .AnyAsync(d => d.Name == model.Name &&
                              d.PhoneNumber == model.PhoneNumber &&
                              d.Id != model.Id);

            // Check if name and phone combination already exists in Patients table
            var patientNamePhoneExists = await _context.Patients
                .AnyAsync(p => p.Name == model.Name && p.PhoneNumber == model.PhoneNumber);

            if (doctorNamePhoneExists || patientNamePhoneExists)
            {
                ModelState.AddModelError(string.Empty, "A user with this name and phone number combination already exists.");
            }
        }

        private async Task ValidatePatientProfileUpdate(PatientProfileViewModel model, int currentUserId)
        {
            // Check if username already exists (excluding current user)
            var usernameExists = await _context.UserAccounts
                .AnyAsync(ua => ua.Username == model.Username && ua.Id != currentUserId);

            if (usernameExists)
            {
                ModelState.AddModelError("Username", "Username is already taken.");
            }

            // Check if email already exists in Doctors table
            var doctorEmailExists = await _context.Doctors
                .AnyAsync(d => d.Email == model.Email);

            // Check if email already exists in Patients table (excluding current patient)
            var patientEmailExists = await _context.Patients
                .AnyAsync(p => p.Email == model.Email && p.Id != model.Id);

            if (doctorEmailExists || patientEmailExists)
            {
                ModelState.AddModelError("Email", "Email address is already registered by another user.");
            }

            // Check if phone number already exists in Doctors table
            var doctorPhoneExists = await _context.Doctors
                .AnyAsync(d => d.PhoneNumber == model.PhoneNumber);

            // Check if phone number already exists in Patients table (excluding current patient)
            var patientPhoneExists = await _context.Patients
                .AnyAsync(p => p.PhoneNumber == model.PhoneNumber && p.Id != model.Id);

            if (doctorPhoneExists || patientPhoneExists)
            {
                ModelState.AddModelError("PhoneNumber", "Phone number is already registered by another user.");
            }

            // Check if name and phone combination already exists in Doctors table
            var doctorNamePhoneExists = await _context.Doctors
                .AnyAsync(d => d.Name == model.Name && d.PhoneNumber == model.PhoneNumber);

            // Check if name and phone combination already exists in Patients table (excluding current patient)
            var patientNamePhoneExists = await _context.Patients
                .AnyAsync(p => p.Name == model.Name &&
                              p.PhoneNumber == model.PhoneNumber &&
                              p.Id != model.Id);

            if (doctorNamePhoneExists || patientNamePhoneExists)
            {
                ModelState.AddModelError(string.Empty, "A user with this name and phone number combination already exists.");
            }

            // Validate date of birth - cannot be in the future
            if (model.DateOfBirth > DateTime.Today)
            {
                ModelState.AddModelError("DateOfBirth", "Date of birth cannot be in the future.");
            }

            if (!string.IsNullOrEmpty(model.PhoneNumber))
            {
                var phonePattern = @"^\d{10,15}$"; // Only digits, 10–15 characters
                if (!Regex.IsMatch(model.PhoneNumber, phonePattern))
                {
                    ModelState.AddModelError("PhoneNumber", "Phone number must be 10-15 digits.");
                }
            }
        }
    }
}