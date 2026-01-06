using DoctorAppointmentSystem.Data;
using DoctorAppointmentSystem.Models;
using DoctorAppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using DoctorAppointmentSystem.Services;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace DoctorAppointmentSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly IEmailService _emailService;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IMemoryCache _memoryCache;
        private const int MaxFailedAttempts = 3;
        private const int LockoutDurationSeconds = 30;

        public AccountController(ApplicationDbContext context, IWebHostEnvironment environment, IEmailService emailService, IMemoryCache memoryCache)
        {
            _context = context;
            _environment = environment;
            _emailService = emailService;
            _memoryCache = memoryCache;
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            // Check if user is temporarily locked out
            var ipAddress = GetClientIpAddress();
            var lockoutKey = $"login_lockout_{ipAddress}";

            if (_memoryCache.TryGetValue(lockoutKey, out DateTime lockoutEndTime))
            {
                if (lockoutEndTime > DateTime.Now)
                {
                    var remainingTime = (lockoutEndTime - DateTime.Now).TotalSeconds;
                    ViewData["LockoutMessage"] = $"Too many failed login attempts. Please try again in {Math.Ceiling(remainingTime)} seconds.";
                    ViewData["IsLockedOut"] = true;
                    ViewData["LockoutEndTime"] = lockoutEndTime;
                }
                else
                {
                    // Lockout period has expired, remove from cache
                    _memoryCache.Remove(lockoutKey);
                }
            }

            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var ipAddress = GetClientIpAddress();
            var attemptKey = $"login_attempts_{ipAddress}";
            var lockoutKey = $"login_lockout_{ipAddress}";

            // Check if user is currently locked out
            if (_memoryCache.TryGetValue(lockoutKey, out DateTime lockoutEndTime))
            {
                if (lockoutEndTime > DateTime.Now)
                {
                    var remainingTime = (lockoutEndTime - DateTime.Now).TotalSeconds;
                    ModelState.AddModelError(string.Empty, $"Too many failed login attempts. Please try again in {Math.Ceiling(remainingTime)} seconds.");
                    ViewData["IsLockedOut"] = true;
                    ViewData["LockoutEndTime"] = lockoutEndTime;
                    return View(model);
                }
                else
                {
                    // Lockout period has expired, remove from cache
                    _memoryCache.Remove(lockoutKey);
                    _memoryCache.Remove(attemptKey);
                }
            }

            if (ModelState.IsValid)
            {
                // Get current failed attempts
                var failedAttempts = _memoryCache.GetOrCreate(attemptKey, entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                    return 0;
                });

                var userAccount = await _context.UserAccounts
                    .Include(ua => ua.Patient)
                    .Include(ua => ua.Doctor)
                    .FirstOrDefaultAsync(u => u.Username == model.Username);

                if (userAccount != null && VerifyPassword(model.Password, userAccount.PasswordHash))
                {
                    // Successful login - reset failed attempts
                    _memoryCache.Remove(attemptKey);
                    _memoryCache.Remove(lockoutKey);

                    string displayName = userAccount.Username;
                    string profileImage = null;

                    if (userAccount.Role == "Doctor" && userAccount.Doctor != null)
                    {
                        displayName = "Dr. " + userAccount.Doctor.Name;
                        profileImage = userAccount.Doctor.ProfileImage;
                    }
                    else if (userAccount.Role == "Patient" && userAccount.Patient != null)
                    {
                        displayName = userAccount.Patient.Name;
                        profileImage = userAccount.Patient.ProfileImage;
                    }

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, userAccount.Username),
                        new Claim(ClaimTypes.Role, userAccount.Role),
                        new Claim("UserId", userAccount.Id.ToString()),
                        new Claim("DisplayName", displayName),
                        new Claim("ProfileImage", profileImage ?? "")
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddMinutes(30)
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    // Redirect directly to the appropriate page based on role
                    if (userAccount.Role == "Admin")
                    {
                        // Redirect admin to Admin Dashboard
                        return RedirectToAction("Doctors", "Admin");
                    }
                    else if (userAccount.Role == "Doctor")
                    {
                        return RedirectToAction("Doctor", "Profile");
                    }
                    else if (userAccount.Role == "Patient")
                    {
                        return RedirectToAction("Patient", "Profile");
                    }

                    // Fallback redirect if role is unknown
                    return RedirectToAction("Index", "Home");
                }

                // Failed login attempt
                failedAttempts++;
                _memoryCache.Set(attemptKey, failedAttempts, TimeSpan.FromMinutes(5));

                if (failedAttempts >= MaxFailedAttempts)
                {
                    // Lock the user out
                    var lockoutTime = DateTime.Now.AddSeconds(LockoutDurationSeconds);
                    _memoryCache.Set(lockoutKey, lockoutTime, TimeSpan.FromSeconds(LockoutDurationSeconds + 10));

                    ModelState.AddModelError(string.Empty, $"Too many failed login attempts. Please try again in {LockoutDurationSeconds} seconds.");
                    ViewData["IsLockedOut"] = true;
                    ViewData["LockoutEndTime"] = lockoutTime;
                }
                else
                {
                    var remainingAttempts = MaxFailedAttempts - failedAttempts;
                    ModelState.AddModelError(string.Empty, $"Invalid username or password. {remainingAttempts} attempt(s) remaining.");
                }
            }

            return View(model);
        }

        private string GetClientIpAddress()
        {
            // Get client IP address considering proxies and load balancers
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Check for forwarded headers (behind proxy/load balancer)
            if (HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].ToString().Split(',')[0];
            }

            return ipAddress ?? "unknown";
        }

        // GET: /Account/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // Custom validation for username/email
            await ValidateRegistration(model);

            if (model.RegisterAsDoctor && string.IsNullOrEmpty(model.Specialty))
            {
                ModelState.AddModelError("Specialty", "Specialty is required when registering as a doctor.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                string profileImagePath = null;
                if (model.ProfileImage != null && model.ProfileImage.Length > 0)
                {
                    profileImagePath = await SaveProfileImage(model.ProfileImage);
                }

                UserAccount userAccount;

                if (model.RegisterAsDoctor)
                {
                    // Create Doctor
                    var doctor = new Doctor
                    {
                        Name = model.Name,
                        Email = model.Email,
                        PhoneNumber = model.PhoneNumber,
                        Gender = model.Gender,
                        Role = "Doctor",
                        Specialty = model.Specialty,
                        ProfileImage = profileImagePath
                    };

                    _context.Users.Add(doctor);
                    await _context.SaveChangesAsync();

                    userAccount = new UserAccount
                    {
                        Username = model.Username,
                        PasswordHash = HashPassword(model.Password),
                        Role = "Doctor",
                        DoctorId = doctor.Id
                    };
                    _context.UserAccounts.Add(userAccount);
                }
                else
                {
                    // Create Patient
                    var patient = new Patient
                    {
                        Name = model.Name,
                        Email = model.Email,
                        PhoneNumber = model.PhoneNumber,
                        Gender = model.Gender,
                        Role = "Patient",
                        DateOfBirth = model.DateOfBirth,
                        ProfileImage = profileImagePath,
                        Address = model.Address
                    };

                    _context.Users.Add(patient);
                    await _context.SaveChangesAsync();

                    userAccount = new UserAccount
                    {
                        Username = model.Username,
                        PasswordHash = HashPassword(model.Password),
                        Role = "Patient",
                        PatientId = patient.Id
                    };
                    _context.UserAccounts.Add(userAccount);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Registration successful! Please login to continue.";
                return RedirectToAction("Login");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, "An error occurred during registration. Please try again.");
                return View(model);
            }
        }

        private async Task ValidateRegistration(RegisterViewModel model)
        {
            // Check if username already exists in UserAccounts
            var existingUsername = await _context.UserAccounts
                .FirstOrDefaultAsync(u => u.Username == model.Username);

            if (existingUsername != null)
            {
                ModelState.AddModelError("Username", "Username is already taken. Please choose a different one.");
            }

            // Check if email already exists in Users table (both Doctors and Patients)
            var existingEmail = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (existingEmail != null)
            {
                ModelState.AddModelError("Email", "Email address is already registered. Please use a different email.");
            }

            // Check if phone number already exists
            var existingPhone = await _context.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == model.PhoneNumber);

            if (existingPhone != null)
            {
                ModelState.AddModelError("PhoneNumber", "Phone number is already registered. Please use a different phone number.");
            }

            // Check if full name and phone number combination already exists
            var existingNamePhone = await _context.Users
                .FirstOrDefaultAsync(u => u.Name == model.Name && u.PhoneNumber == model.PhoneNumber);

            if (existingNamePhone != null)
            {
                ModelState.AddModelError(string.Empty, "A user with this name and phone number combination already exists.");
            }

            // Validate date of birth - cannot be in the future
            if (model.DateOfBirth > DateTime.Today)
            {
                ModelState.AddModelError("DateOfBirth", "Date of birth cannot be in the future.");
            }

            // Password cannot be the same as username
            if (model.Password.ToLower() == model.Username.ToLower())
            {
                ModelState.AddModelError("Password", "Password cannot be the same as your username.");
            }

            // Password cannot be the same as email
            if (model.Password.ToLower() == model.Email.ToLower())
            {
                ModelState.AddModelError("Password", "Password cannot be the same as your email address.");
            }

            // Password strength
            if (!IsPasswordStrong(model.Password))
            {
                ModelState.AddModelError("Password", "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.");
            }
        }

        private bool IsPasswordStrong(string password)
        {
            // Check for at least one uppercase letter
            if (!password.Any(char.IsUpper))
                return false;

            // Check for at least one lowercase letter
            if (!password.Any(char.IsLower))
                return false;

            // Check for at least one digit
            if (!password.Any(char.IsDigit))
                return false;

            // Check for at least one special character
            if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
                return false;

            return true;
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

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            var hashOfInput = HashPassword(password);
            return hashOfInput == storedHash;
        }

        // AJAX method to check username availability
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> CheckUsernameAvailability(string username)
        {
            var exists = await _context.UserAccounts.AnyAsync(u => u.Username == username);
            return Json(new { available = !exists });
        }

        // AJAX method to check email availability
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> CheckEmailAvailability(string email)
        {
            var exists = await _context.Users.AnyAsync(u => u.Email == email);
            return Json(new { available = !exists });
        }

        // AJAX method to check name and phone combination
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> CheckNamePhoneAvailability(string name, string phoneNumber)
        {
            var exists = await _context.Users.AnyAsync(u => u.Name == name && u.PhoneNumber == phoneNumber);
            return Json(new { available = !exists });
        }

        // AJAX method to check phone availability
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> CheckPhoneAvailability(string phoneNumber)
        {
            var exists = await _context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber);
            return Json(new { available = !exists });
        }

        [Authorize]
        public IActionResult ResetPassword()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            ViewData["UserRole"] = userRole;
            return View();
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            ViewData["UserRole"] = userRole;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Get current user ID from claims
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                ModelState.AddModelError(string.Empty, "User not found. Please log in again.");
                return View(model);
            }

            // Get user account
            var userAccount = await _context.UserAccounts.FindAsync(userId);
            if (userAccount == null)
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                return View(model);
            }

            // Verify current password
            if (!VerifyPassword(model.CurrentPassword, userAccount.PasswordHash))
            {
                ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
                return View(model);
            }

            // Check if new password is the same as current password
            if (VerifyPassword(model.NewPassword, userAccount.PasswordHash))
            {
                ModelState.AddModelError("NewPassword", "New password cannot be the same as current password.");
                return View(model);
            }

            // Check if password has been used before
            var hashedNewPassword = HashPassword(model.NewPassword);
            var passwordHistory = await _context.UserAccounts
                .Where(u => u.PasswordHash == hashedNewPassword && u.Id != userId)
                .FirstOrDefaultAsync();

            if (passwordHistory != null)
            {
                ModelState.AddModelError("NewPassword", "This password has been used before. Please choose a different password.");
                return View(model);
            }

            // Additional validation: Password cannot be the same as username
            if (model.NewPassword.ToLower() == User.Identity.Name.ToLower())
            {
                ModelState.AddModelError("NewPassword", "Password cannot be the same as your username.");
                return View(model);
            }

            // Additional validation: Password strength
            if (!IsPasswordStrong(model.NewPassword))
            {
                ModelState.AddModelError("NewPassword", "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.");
                return View(model);
            }

            // Update password
            userAccount.PasswordHash = hashedNewPassword;
            _context.UserAccounts.Update(userAccount);

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Password has been reset successfully!";
                return RedirectToAction("ResetPassword");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An error occurred while resetting your password. Please try again.");
                ViewData["UserRole"] = userRole; // Pass role here too
                return View(model);
            }
        }

        // GET: /Account/ForgotPassword
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Find user by email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)
            {
                // Don't reveal that the user doesn't exist
                ModelState.AddModelError(string.Empty, "If the email address exists in our system, we've sent a password reset link.");
                return View(model);
            }

            // Generate reset token
            var resetToken = GenerateResetToken();
            var resetLink = Url.Action("ResetPasswordEmail", "Account",
                new { email = model.Email, token = resetToken },
                protocol: HttpContext.Request.Scheme);

            // Store token in cache (valid for 1 hour)
            _memoryCache.Set($"reset_token_{model.Email}", resetToken, TimeSpan.FromHours(1));

            // Send email
            var emailSubject = "Password Reset Request";
            var emailMessage = $@"
        <h3>Password Reset Request</h3>
        <p>You requested to reset your password for the Doctor Appointment System.</p>
        <p>Please click the link below to reset your password:</p>
        <p><a href='{resetLink}'>{resetLink}</a></p>
        <p>This link will expire in 1 hour.</p>
        <p>If you didn't request this reset, please ignore this email.</p>
    ";

            try
            {
                await _emailService.SendEmailAsync(model.Email, emailSubject, emailMessage);
                TempData["SuccessMessage"] = "If the email address exists in our system, we've sent a password reset link.";
                return RedirectToAction("ForgotPasswordConfirmation");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Failed to send email. Please try again later.");
                return View(model);
            }
        }

        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // GET: /Account/ResetPasswordEmail
        [AllowAnonymous]
        public IActionResult ResetPasswordEmail(string email, string token)
        {
            // Validate token
            var storedToken = _memoryCache.Get<string>($"reset_token_{email}");
            if (storedToken == null || storedToken != token)
            {
                TempData["ErrorMessage"] = "Invalid or expired reset token.";
                return RedirectToAction("ForgotPassword");
            }

            var model = new ResetPasswordEmailViewModel
            {
                Email = email,
                Token = token
            };

            return View(model);
        }

        // POST: /Account/ResetPasswordEmail
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPasswordEmail(ResetPasswordEmailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Validate token
            var storedToken = _memoryCache.Get<string>($"reset_token_{model.Email}");
            if (storedToken == null || storedToken != model.Token)
            {
                TempData["ErrorMessage"] = "Invalid or expired reset token.";
                return RedirectToAction("ForgotPassword");
            }

            // Find user by email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("ForgotPassword");
            }

            // Find user account
            var userAccount = await _context.UserAccounts
                .FirstOrDefaultAsync(ua => ua.DoctorId == user.Id || ua.PatientId == user.Id);

            if (userAccount == null)
            {
                TempData["ErrorMessage"] = "User account not found.";
                return RedirectToAction("ForgotPassword");
            }

            // Update password
            userAccount.PasswordHash = HashPassword(model.NewPassword);
            _context.UserAccounts.Update(userAccount);

            try
            {
                await _context.SaveChangesAsync();

                // Remove used token
                _memoryCache.Remove($"reset_token_{model.Email}");

                // Send confirmation email
                var emailSubject = "Password Reset Successful";
                var emailMessage = $@"
            <h3>Password Reset Successful</h3>
            <p>Your password has been successfully reset.</p>
            <p>If you didn't perform this action, please contact support immediately.</p>
        ";

                await _emailService.SendEmailAsync(model.Email, emailSubject, emailMessage);

                TempData["SuccessMessage"] = "Your password has been reset successfully. You can now login with your new password.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An error occurred while resetting your password. Please try again.");
                return View(model);
            }
        }

        private string GenerateResetToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}