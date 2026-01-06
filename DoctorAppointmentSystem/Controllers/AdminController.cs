using DoctorAppointmentSystem.Data;
using DoctorAppointmentSystem.Models;
using DoctorAppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace DoctorAppointmentSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            // Get current year
            var currentYear = DateTime.Now.Year;

            // Monthly appointments data
            var monthlyAppointments = await _context.Appointments
                .Where(a => a.AppointmentDate.Year == currentYear)
                .GroupBy(a => new { a.AppointmentDate.Month })
                .Select(g => new AppointmentChartData
                {
                    Month = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(g.Key.Month),
                    Count = g.Count()
                })
                .ToListAsync();

            // Appointment status data
            var appointmentStatuses = await _context.Appointments
                .GroupBy(a => a.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            // Get recent appointments (last 10)
            var recentAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .OrderByDescending(a => a.AppointmentDate)
                .Take(10)
                .ToListAsync();

            var dashboardData = new DashboardViewModel
            {
                TotalPatients = await _context.Patients.CountAsync(),
                ActiveDoctors = await _context.Doctors.CountAsync(),
                TodaysAppointments = await _context.Appointments
                    .Where(a => a.AppointmentDate.Date == DateTime.Today)
                    .CountAsync(),
                MonthlyRevenue = await _context.Payments
                    .Where(p => p.PaymentDate.Month == DateTime.Now.Month &&
                               p.PaymentDate.Year == DateTime.Now.Year)
                    .SumAsync(p => p.Amount),
                MonthlyAppointments = monthlyAppointments,
                AppointmentStatuses = appointmentStatuses,
                MonthlyAppointmentLabels = monthlyAppointments.Select(a => a.Month).ToArray(),
                MonthlyAppointmentData = monthlyAppointments.Select(a => a.Count).ToArray(),
                AppointmentStatusLabels = appointmentStatuses.Keys.ToArray(),
                AppointmentStatusData = appointmentStatuses.Values.ToArray(),
                RecentAppointments = recentAppointments
            };

            return View(dashboardData);
        }

        public async Task<IActionResult> Doctors(string searchString, string specialtyFilter)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["SpecialtyFilter"] = specialtyFilter;

            var doctors = from d in _context.Doctors
                          select d;

            if (!string.IsNullOrEmpty(searchString))
            {
                doctors = doctors.Where(d => d.Name.Contains(searchString)
                                           || d.Email.Contains(searchString)
                                           || d.Specialty.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(specialtyFilter))
            {
                doctors = doctors.Where(d => d.Specialty == specialtyFilter);
            }

            // Get distinct specialties for dropdown
            var specialties = await _context.Doctors
                .Select(d => d.Specialty)
                .Distinct()
                .ToListAsync();

            ViewBag.Specialties = specialties;

            return View(await doctors.ToListAsync());
        }

        public async Task<IActionResult> Patients(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;

            var patients = from p in _context.Patients
                           select p;

            if (!string.IsNullOrEmpty(searchString))
            {
                patients = patients.Where(p => p.Name.Contains(searchString)
                                         || p.Email.Contains(searchString)
                                         || p.PhoneNumber.Contains(searchString));
            }

            return View(await patients.ToListAsync());
        }

        public async Task<IActionResult> Appointments(
            string statusFilter,
            string dateRangeFilter,
            int? doctorId,
            int? patientId)
        {
            // Set up ViewData for filters
            ViewData["StatusFilter"] = statusFilter;
            ViewData["DateRangeFilter"] = dateRangeFilter;
            ViewData["DoctorId"] = doctorId;
            ViewData["PatientId"] = patientId;

            // Get base query with includes
            var appointments = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .AsQueryable();

            // Apply status filter
            if (!string.IsNullOrEmpty(statusFilter))
            {
                appointments = appointments.Where(a => a.Status == statusFilter);
            }

            // Apply date range filter
            if (!string.IsNullOrEmpty(dateRangeFilter))
            {
                var today = DateTime.Today;
                switch (dateRangeFilter)
                {
                    case "Today":
                        appointments = appointments.Where(a => a.AppointmentDate.Date == today);
                        break;
                    case "ThisWeek":
                        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                        var endOfWeek = startOfWeek.AddDays(7);
                        appointments = appointments.Where(a => a.AppointmentDate >= startOfWeek && a.AppointmentDate < endOfWeek);
                        break;
                    case "ThisMonth":
                        appointments = appointments.Where(a => a.AppointmentDate.Month == today.Month && a.AppointmentDate.Year == today.Year);
                        break;
                }
            }

            // Apply doctor filter
            if (doctorId.HasValue)
            {
                appointments = appointments.Where(a => a.DoctorId == doctorId.Value);
            }

            // Apply patient filter
            if (patientId.HasValue)
            {
                appointments = appointments.Where(a => a.PatientId == patientId.Value);
            }

            // Get dropdown data
            ViewBag.Doctors = await _context.Doctors.ToListAsync();
            ViewBag.Patients = await _context.Patients.ToListAsync();
            ViewBag.Statuses = new List<string> { "Pending", "Confirmed", "Completed", "Cancelled" };

            return View(await appointments.OrderBy(a => a.AppointmentDate).ToListAsync());
        }

        public async Task<IActionResult> DoctorSchedule(
            int? doctorId,
            string dateRangeFilter,
            bool? isAvailable)
        {
            // Set up ViewData for filters
            ViewData["DoctorId"] = doctorId;
            ViewData["DateRangeFilter"] = dateRangeFilter;
            ViewData["IsAvailable"] = isAvailable;

            var schedules = _context.DoctorSchedules
                .Include(ds => ds.Doctor)
                .AsQueryable();

            // Apply doctor filter
            if (doctorId.HasValue)
            {
                schedules = schedules.Where(ds => ds.DoctorId == doctorId.Value);
            }

            // Apply date range filter
            if (!string.IsNullOrEmpty(dateRangeFilter))
            {
                var today = DateTime.Today;
                switch (dateRangeFilter)
                {
                    case "Today":
                        schedules = schedules.Where(ds => ds.Date.Date == today);
                        break;
                    case "ThisWeek":
                        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                        var endOfWeek = startOfWeek.AddDays(7);
                        schedules = schedules.Where(ds => ds.Date >= startOfWeek && ds.Date < endOfWeek);
                        break;
                    case "ThisMonth":
                        schedules = schedules.Where(ds => ds.Date.Month == today.Month && ds.Date.Year == today.Year);
                        break;
                    case "Upcoming":
                        schedules = schedules.Where(ds => ds.Date >= today);
                        break;
                }
            }

            // Apply availability filter
            if (isAvailable.HasValue)
            {
                schedules = schedules.Where(ds => ds.IsAvailable == isAvailable.Value);
            }

            // Get dropdown data
            ViewBag.Doctors = await _context.Doctors.ToListAsync();

            return View(await schedules.OrderBy(ds => ds.Date).ThenBy(ds => ds.StartTime).ToListAsync());
        }

        public async Task<IActionResult> MedicalRecords(string searchString, string bloodTypeFilter)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["BloodTypeFilter"] = bloodTypeFilter;

            var medicalRecords = _context.MedicalRecords
                .Include(mr => mr.Patient)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                medicalRecords = medicalRecords.Where(mr =>
                    mr.Patient.Name.Contains(searchString) ||
                    mr.Allergies.Contains(searchString) ||
                    mr.CurrentMedications.Contains(searchString) ||
                    mr.PastMedicalHistory.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(bloodTypeFilter))
            {
                medicalRecords = medicalRecords.Where(mr => mr.BloodType == bloodTypeFilter);
            }

            // Get distinct blood types for dropdown
            var bloodTypes = await _context.MedicalRecords
                .Select(mr => mr.BloodType)
                .Distinct()
                .Where(bt => !string.IsNullOrEmpty(bt))
                .ToListAsync();

            ViewBag.BloodTypes = new SelectList(bloodTypes);

            return View(await medicalRecords.OrderByDescending(mr => mr.CreatedDate).ToListAsync());
        }

        public async Task<IActionResult> Reviews(
            int? ratingFilter,
            int? doctorId,
            string dateRangeFilter)
        {
            ViewData["RatingFilter"] = ratingFilter;
            ViewData["DoctorId"] = doctorId;
            ViewData["DateRangeFilter"] = dateRangeFilter;

            var reviews = _context.Reviews
                .Include(r => r.Patient)
                .Include(r => r.Doctor)
                .AsQueryable();

            if (ratingFilter.HasValue)
            {
                reviews = reviews.Where(r => r.Rating == ratingFilter.Value);
            }

            if (doctorId.HasValue)
            {
                reviews = reviews.Where(r => r.DoctorId == doctorId.Value);
            }

            if (!string.IsNullOrEmpty(dateRangeFilter))
            {
                var today = DateTime.Today;
                switch (dateRangeFilter)
                {
                    case "Today":
                        reviews = reviews.Where(r => r.Date.Date == today);
                        break;
                    case "ThisWeek":
                        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                        var endOfWeek = startOfWeek.AddDays(7);
                        reviews = reviews.Where(r => r.Date >= startOfWeek && r.Date < endOfWeek);
                        break;
                    case "ThisMonth":
                        reviews = reviews.Where(r => r.Date.Month == today.Month && r.Date.Year == today.Year);
                        break;
                    case "ThisYear":
                        reviews = reviews.Where(r => r.Date.Year == today.Year);
                        break;
                }
            }

            ViewBag.Doctors = await _context.Doctors.ToListAsync();

            return View(await reviews.OrderByDescending(r => r.Date).ToListAsync());
        }

        // GET: Admin/DeleteReview/5 (Show confirmation page)
        public async Task<IActionResult> DeleteReview(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var review = await _context.Reviews
                .Include(r => r.Patient)
                .Include(r => r.Doctor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (review == null)
            {
                return NotFound();
            }

            return View(review);
        }

        // POST: Admin/DeleteReview/5 (Handle actual deletion)
        [HttpPost, ActionName("DeleteReview")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReviewConfirmed(int id)
        {
            try
            {
                var review = await _context.Reviews.FindAsync(id);
                if (review == null)
                {
                    TempData["ErrorMessage"] = "Review not found.";
                    return RedirectToAction(nameof(Reviews));
                }

                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Review deleted successfully!";
                return RedirectToAction(nameof(Reviews));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting review: {ex.Message}";
                return RedirectToAction(nameof(Reviews));
            }
        }

        // A method to get review details
        public async Task<IActionResult> ReviewDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var review = await _context.Reviews
                .Include(r => r.Patient)
                .Include(r => r.Doctor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (review == null)
            {
                return NotFound();
            }

            return View(review);
        }

        public async Task<IActionResult> Payments(
            string statusFilter,
            string dateRangeFilter,
            string paymentMethodFilter)
        {
            ViewData["StatusFilter"] = statusFilter;
            ViewData["DateRangeFilter"] = dateRangeFilter;
            ViewData["PaymentMethodFilter"] = paymentMethodFilter;

            var payments = _context.Payments
                .Include(p => p.Appointment)
                    .ThenInclude(a => a.Patient)
                .Include(p => p.Appointment)
                    .ThenInclude(a => a.Doctor)
                .AsQueryable();

            if (!string.IsNullOrEmpty(statusFilter))
            {
                payments = payments.Where(p => p.Status == statusFilter);
            }

            if (!string.IsNullOrEmpty(paymentMethodFilter))
            {
                payments = payments.Where(p => p.PaymentMethod == paymentMethodFilter);
            }

            if (!string.IsNullOrEmpty(dateRangeFilter))
            {
                var today = DateTime.Today;
                switch (dateRangeFilter)
                {
                    case "Today":
                        payments = payments.Where(p => p.PaymentDate.Date == today);
                        break;
                    case "ThisWeek":
                        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                        var endOfWeek = startOfWeek.AddDays(7);
                        payments = payments.Where(p => p.PaymentDate >= startOfWeek && p.PaymentDate < endOfWeek);
                        break;
                    case "ThisMonth":
                        payments = payments.Where(p => p.PaymentDate.Month == today.Month && p.PaymentDate.Year == today.Year);
                        break;
                }
            }

            // Get filter options - FIXED: Handle potential null TransactionId
            ViewBag.Statuses = await _context.Payments
                .Where(p => p.Status != null)
                .Select(p => p.Status)
                .Distinct()
                .ToListAsync();

            ViewBag.PaymentMethods = await _context.Payments
                .Where(p => p.PaymentMethod != null)
                .Select(p => p.PaymentMethod)
                .Distinct()
                .ToListAsync();

            return View(await payments.OrderByDescending(p => p.PaymentDate).ToListAsync());
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new DoctorViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DoctorViewModel model)
        {
            if (_context.Doctors.Any(d => d.Email == model.Email))
            {
                ModelState.AddModelError("Email", "This email is already in use.");
            }

            if (_context.Doctors.Any(d => d.PhoneNumber == model.PhoneNumber))
            {
                ModelState.AddModelError("PhoneNumber", "This phone number is already in use.");
            }
            if (ModelState.IsValid)
            {
                var doctor = new Doctor
                {
                    Name = model.Name,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    Gender = model.Gender,
                    Specialty = model.Specialty,
                    ProfileImage = model.ProfileImage,
                    Role = "Doctor"
                };

                _context.Doctors.Add(doctor);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Doctor added successfully!";
                return RedirectToAction(nameof(Doctors));
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditDoctor(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null)
            {
                return NotFound();
            }

            var model = new DoctorViewModel
            {
                Id = doctor.Id,
                Name = doctor.Name,
                Email = doctor.Email,
                PhoneNumber = doctor.PhoneNumber,
                Gender = doctor.Gender,
                Specialty = doctor.Specialty,
                ProfileImage = doctor.ProfileImage
            };

            // Get specialties for dropdown
            var specialties = await _context.Doctors
                .Select(d => d.Specialty)
                .Distinct()
                .ToListAsync();
            ViewBag.Specialties = specialties;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDoctor(int id, DoctorViewModel model)
        {
            if (_context.Doctors.Any(d => d.Email == model.Email && d.Id != model.Id))
            {
                ModelState.AddModelError("Email", "This email is already in use.");
            }

            if (_context.Doctors.Any(d => d.PhoneNumber == model.PhoneNumber && d.Id != model.Id))
            {
                ModelState.AddModelError("PhoneNumber", "This phone number is already in use.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var doctor = await _context.Doctors.FindAsync(id);
                    if (doctor == null)
                    {
                        return NotFound();
                    }

                    doctor.Name = model.Name;
                    doctor.Email = model.Email;
                    doctor.PhoneNumber = model.PhoneNumber;
                    doctor.Gender = model.Gender;
                    doctor.Specialty = model.Specialty;
                    doctor.ProfileImage = model.ProfileImage;

                    _context.Update(doctor);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Doctor updated successfully!";
                    return RedirectToAction(nameof(Doctors));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DoctorExists(model.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(model);
        }

        public async Task<IActionResult> DetailsDoctor(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(m => m.Id == id);

            if (doctor == null)
            {
                return NotFound();
            }

            return View(doctor);
        }

        public async Task<IActionResult> DeleteDoctor(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(m => m.Id == id);

            if (doctor == null)
            {
                return NotFound();
            }

            // Check for active appointments
            var hasActiveAppointments = await _context.Appointments
                .Where(a => a.DoctorId == id && a.Status != null)
                .AnyAsync();

            ViewBag.HasActiveAppointments = hasActiveAppointments;

            return View(doctor);
        }

        [HttpPost, ActionName("DeleteDoctor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDoctorConfirmed(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var doctor = await _context.Doctors.FindAsync(id);
                if (doctor == null)
                {
                    TempData["ErrorMessage"] = "Doctor not found.";
                    return RedirectToAction(nameof(Doctors));
                }

                // Check for non-cancelled appointments
                var activeAppointments = await _context.Appointments
                    .Where(a => a.DoctorId == id && a.Status != null)
                    .AnyAsync();

                if (activeAppointments)
                {
                    TempData["ErrorMessage"] = "Cannot delete doctor. There are active (non-cancelled) appointments. Please delete all appointments first.";
                    return RedirectToAction(nameof(DeleteDoctor), new { id });
                }

                // Remove related UserAccount
                var userAccount = await _context.UserAccounts
                    .FirstOrDefaultAsync(ua => ua.DoctorId == id);

                if (userAccount != null)
                {
                    _context.UserAccounts.Remove(userAccount);
                }

                // Remove doctor
                _context.Doctors.Remove(doctor);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Doctor deleted successfully!";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"Error deleting doctor: {ex.Message}";
            }

            return RedirectToAction(nameof(Doctors));
        }

        private bool DoctorExists(int id)
        {
            return _context.Doctors.Any(e => e.Id == id);
        }

        [HttpGet]
        public IActionResult CreatePatient()
        {
            return View(new PatientViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePatient(PatientViewModel model)
        {
            if (_context.Patients.Any(p => p.Email == model.Email))
            {
                ModelState.AddModelError("Email", "This email is already in use.");
            }

            if (_context.Patients.Any(p => p.PhoneNumber == model.PhoneNumber))
            {
                ModelState.AddModelError("PhoneNumber", "This phone number is already in use.");
            }

            if (ModelState.IsValid)
            {
                var patient = new Patient
                {
                    Name = model.Name,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    Gender = model.Gender,
                    DateOfBirth = model.DateOfBirth,
                    ProfileImage = model.ProfileImage,
                    Address = model.Address,
                    Role = "Patient"
                };

                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Patient added successfully!";
                return RedirectToAction(nameof(Patients));
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditPatient(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients.FindAsync(id);
            if (patient == null)
            {
                return NotFound();
            }

            var model = new PatientViewModel
            {
                Id = patient.Id,
                Name = patient.Name,
                Email = patient.Email,
                PhoneNumber = patient.PhoneNumber,
                Address = patient.Address,
                Gender = patient.Gender,
                DateOfBirth = patient.DateOfBirth,
                ProfileImage = patient.ProfileImage
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPatient(int id, PatientViewModel model)
        {
            if (_context.Patients.Any(p => p.Email == model.Email && p.Id != model.Id))
            {
                ModelState.AddModelError("Email", "This email is already in use.");
            }

            if (_context.Patients.Any(p => p.PhoneNumber == model.PhoneNumber && p.Id != model.Id))
            {
                ModelState.AddModelError("PhoneNumber", "This phone number is already in use.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var patient = await _context.Patients.FindAsync(id);
                    if (patient == null)
                    {
                        return NotFound();
                    }

                    patient.Name = model.Name;
                    patient.Email = model.Email;
                    patient.PhoneNumber = model.PhoneNumber;
                    patient.Address = model.Address;
                    patient.Gender = model.Gender;
                    patient.DateOfBirth = model.DateOfBirth;
                    patient.ProfileImage = model.ProfileImage;

                    _context.Update(patient);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Patient updated successfully!";
                    return RedirectToAction(nameof(Patients));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PatientExists(model.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(model);
        }

        public async Task<IActionResult> DetailsPatient(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .FirstOrDefaultAsync(m => m.Id == id);

            if (patient == null)
            {
                return NotFound();
            }

            // Calculate age
            ViewBag.Age = patient.DateOfBirth != null ?
                DateTime.Now.Year - patient.DateOfBirth.Year : 0;

            return View(patient);
        }

        public async Task<IActionResult> DeletePatient(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .FirstOrDefaultAsync(m => m.Id == id);

            if (patient == null)
            {
                return NotFound();
            }

            // Check for active appointments
            var hasActiveAppointments = await _context.Appointments
                .Where(a => a.PatientId == id && a.Status != null)
                .AnyAsync();

            ViewBag.HasActiveAppointments = hasActiveAppointments;

            // Calculate age
            ViewBag.Age = patient.DateOfBirth != null ?
                DateTime.Now.Year - patient.DateOfBirth.Year : 0;

            return View(patient);
        }

        [HttpPost, ActionName("DeletePatient")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePatientConfirmed(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var patient = await _context.Patients.FindAsync(id);
                if (patient == null)
                {
                    TempData["ErrorMessage"] = "Patient not found.";
                    return RedirectToAction(nameof(Patients));
                }

                // Check for non-cancelled appointments
                var activeAppointments = await _context.Appointments
                    .Where(a => a.PatientId == id && a.Status != null)
                    .AnyAsync();

                if (activeAppointments)
                {
                    TempData["ErrorMessage"] = "Cannot delete patient. There are active (non-cancelled) appointments. Please delete all appointments first.";
                    return RedirectToAction(nameof(DeletePatient), new { id });
                }


                // Remove related UserAccount
                var userAccount = await _context.UserAccounts
                    .FirstOrDefaultAsync(ua => ua.PatientId == id);

                if (userAccount != null)
                {
                    _context.UserAccounts.Remove(userAccount);
                }

                // Remove patient
                _context.Patients.Remove(patient);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Patient deleted successfully!";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"Error deleting patient: {ex.Message}";
            }

            return RedirectToAction(nameof(Patients));
        }

        private bool PatientExists(int id)
        {
            return _context.Patients.Any(e => e.Id == id);
        }

        [HttpGet]
        public async Task<IActionResult> CreateAppointment()
        {
            ViewBag.Patients = await _context.Patients.ToListAsync();
            ViewBag.Doctors = await _context.Doctors.ToListAsync();
            return View(new AppointmentViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAppointment(AppointmentViewModel model)
        {
            if (ModelState.IsValid)
            {
                var appointment = new Appointment
                {
                    PatientId = model.PatientId,
                    DoctorId = model.DoctorId,
                    AppointmentDate = model.AppointmentDate,
                    Notes = model.Notes,
                    Status = "Confirmed"
                };

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Appointment created successfully!";
                return RedirectToAction(nameof(Appointments));
            }

            ViewBag.Patients = await _context.Patients.ToListAsync();
            ViewBag.Doctors = await _context.Doctors.ToListAsync();
            return View(model);
        }

        public async Task<IActionResult> AppointmentDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
        }

        [HttpGet]
        public async Task<IActionResult> EditAppointment(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            ViewBag.Patients = await _context.Patients.ToListAsync();
            ViewBag.Doctors = await _context.Doctors.ToListAsync();
            ViewBag.Statuses = new List<string> { "Pending", "Confirmed", "Completed", "Cancelled" };

            return View(appointment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAppointment(int id, [Bind("Id,PatientId,DoctorId,AppointmentDate,Status,Notes")] Appointment appointment)
        {
            if (id != appointment.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(appointment);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Appointment updated successfully!";
                    return RedirectToAction(nameof(Appointments));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AppointmentExists(appointment.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewBag.Patients = await _context.Patients.ToListAsync();
            ViewBag.Doctors = await _context.Doctors.ToListAsync();
            ViewBag.Statuses = new List<string> { "Pending", "Confirmed", "Completed", "Cancelled" };
            return View(appointment);
        }

        public async Task<IActionResult> DeleteAppointment(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
        }

        [HttpPost, ActionName("DeleteAppointment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAppointmentConfirmed(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Appointment deleted successfully!";
            }
            return RedirectToAction(nameof(Appointments));
        }

        private bool AppointmentExists(int id)
        {
            return _context.Appointments.Any(e => e.Id == id);
        }

        [HttpGet]
        public async Task<IActionResult> CreateSchedule()
        {
            ViewBag.Doctors = await _context.Doctors.ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSchedule(DoctorSchedule model)
        {
            if (ModelState.IsValid)
            {
                _context.DoctorSchedules.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Schedule created successfully!";
                return RedirectToAction(nameof(DoctorSchedule));
            }

            ViewBag.Doctors = await _context.Doctors.ToListAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditSchedule(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var schedule = await _context.DoctorSchedules
                .Include(ds => ds.Doctor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (schedule == null)
            {
                return NotFound();
            }

            ViewBag.Doctors = await _context.Doctors.ToListAsync();
            return View(schedule);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSchedule(int id, DoctorSchedule model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(model);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Schedule updated successfully!";
                    return RedirectToAction(nameof(DoctorSchedule));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ScheduleExists(model.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewBag.Doctors = await _context.Doctors.ToListAsync();
            return View(model);
        }

        public async Task<IActionResult> DetailsSchedule(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var schedule = await _context.DoctorSchedules
                .Include(ds => ds.Doctor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (schedule == null)
            {
                return NotFound();
            }

            return View(schedule);
        }

        public async Task<IActionResult> DeleteSchedule(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var schedule = await _context.DoctorSchedules
                .Include(ds => ds.Doctor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (schedule == null)
            {
                return NotFound();
            }

            return View(schedule);
        }

        [HttpPost, ActionName("DeleteSchedule")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteScheduleConfirmed(int id)
        {
            var schedule = await _context.DoctorSchedules.FindAsync(id);
            if (schedule != null)
            {
                _context.DoctorSchedules.Remove(schedule);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Schedule deleted successfully!";
            }
            return RedirectToAction(nameof(DoctorSchedule));
        }

        private bool ScheduleExists(int id)
        {
            return _context.DoctorSchedules.Any(e => e.Id == id);
        }

        // Medical Records CRUD operations
        [HttpGet]
        public async Task<IActionResult> CreateMedicalRecord()
        {
            ViewBag.Patients = await _context.Patients.ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMedicalRecord(MedicalRecord model)
        {
            if (ModelState.IsValid)
            {
                model.CreatedDate = DateTime.Now;
                _context.MedicalRecords.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Medical record created successfully!";
                return RedirectToAction(nameof(MedicalRecords));
            }

            ViewBag.Patients = await _context.Patients.ToListAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditMedicalRecord(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medicalRecord = await _context.MedicalRecords
                .Include(mr => mr.Patient)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (medicalRecord == null)
            {
                return NotFound();
            }

            ViewBag.Patients = await _context.Patients.ToListAsync();
            return View(medicalRecord);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMedicalRecord(int id, MedicalRecord model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingRecord = await _context.MedicalRecords.FindAsync(id);
                    if (existingRecord == null)
                    {
                        return NotFound();
                    }

                    existingRecord.PatientId = model.PatientId;
                    existingRecord.BloodType = model.BloodType;
                    existingRecord.Height = model.Height;
                    existingRecord.Weight = model.Weight;
                    existingRecord.Allergies = model.Allergies;
                    existingRecord.CurrentMedications = model.CurrentMedications;
                    existingRecord.PastMedicalHistory = model.PastMedicalHistory;
                    existingRecord.UpdatedDate = DateTime.Now;

                    _context.Update(existingRecord);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Medical record updated successfully!";
                    return RedirectToAction(nameof(MedicalRecords));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MedicalRecordExists(model.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewBag.Patients = await _context.Patients.ToListAsync();
            return View(model);
        }

        public async Task<IActionResult> MedicalRecordDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medicalRecord = await _context.MedicalRecords
                .Include(mr => mr.Patient)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (medicalRecord == null)
            {
                return NotFound();
            }

            return View(medicalRecord);
        }

        public async Task<IActionResult> DeleteMedicalRecord(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medicalRecord = await _context.MedicalRecords
                .Include(mr => mr.Patient)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (medicalRecord == null)
            {
                return NotFound();
            }

            return View(medicalRecord);
        }

        [HttpPost, ActionName("DeleteMedicalRecord")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMedicalRecordConfirmed(int id)
        {
            var medicalRecord = await _context.MedicalRecords.FindAsync(id);
            if (medicalRecord != null)
            {
                _context.MedicalRecords.Remove(medicalRecord);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Medical record deleted successfully!";
            }
            return RedirectToAction(nameof(MedicalRecords));
        }

        private bool MedicalRecordExists(int id)
        {
            return _context.MedicalRecords.Any(e => e.Id == id);
        }

        public async Task<IActionResult> GetDoctorsAjax(
            string searchString,
            string specialtyFilter,
            string sortBy = "Name",
            string sortOrder = "asc",
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                var doctorsQuery = _context.Doctors.AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(searchString))
                {
                    doctorsQuery = doctorsQuery.Where(d =>
                        d.Name.Contains(searchString) ||
                        d.Email.Contains(searchString) ||
                        d.Specialty.Contains(searchString));
                }

                // Apply specialty filter
                if (!string.IsNullOrEmpty(specialtyFilter) && specialtyFilter != "All Specialties")
                {
                    doctorsQuery = doctorsQuery.Where(d => d.Specialty == specialtyFilter);
                }

                // Apply sorting
                doctorsQuery = sortBy.ToLower() switch
                {
                    "id" => sortOrder == "asc" ?
                        doctorsQuery.OrderBy(d => d.Id) :
                        doctorsQuery.OrderByDescending(d => d.Id),
                    "name" => sortOrder == "asc" ?
                        doctorsQuery.OrderBy(d => d.Name) :
                        doctorsQuery.OrderByDescending(d => d.Name),
                    "specialty" => sortOrder == "asc" ?
                        doctorsQuery.OrderBy(d => d.Specialty) :
                        doctorsQuery.OrderByDescending(d => d.Specialty),
                    "email" => sortOrder == "asc" ?
                        doctorsQuery.OrderBy(d => d.Email) :
                        doctorsQuery.OrderByDescending(d => d.Email),
                    _ => doctorsQuery.OrderBy(d => d.Name)
                };

                // Get total count for pagination
                var totalCount = await doctorsQuery.CountAsync();

                // Apply pagination
                var doctors = await doctorsQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Get distinct specialties for dropdown
                var specialties = await _context.Doctors
                    .Select(d => d.Specialty)
                    .Distinct()
                    .ToListAsync();

                return Json(new
                {
                    Success = true,
                    Doctors = doctors,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    Specialties = specialties
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Success = false,
                    Message = "Error loading doctors: " + ex.Message
                });
            }
        }

        public async Task<IActionResult> GetPatientsAjax(
            string searchString,
            string sortBy = "Name",
            string sortOrder = "asc",
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                var patientsQuery = _context.Patients.AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(searchString))
                {
                    patientsQuery = patientsQuery.Where(p =>
                        p.Name.Contains(searchString) ||
                        p.Email.Contains(searchString) ||
                        p.PhoneNumber.Contains(searchString) ||
                        p.Gender.Contains(searchString));
                }

                // Apply sorting
                patientsQuery = sortBy.ToLower() switch
                {
                    "id" => sortOrder == "asc" ?
                        patientsQuery.OrderBy(p => p.Id) :
                        patientsQuery.OrderByDescending(p => p.Id),
                    "name" => sortOrder == "asc" ?
                        patientsQuery.OrderBy(p => p.Name) :
                        patientsQuery.OrderByDescending(p => p.Name),
                    "gender" => sortOrder == "asc" ?
                        patientsQuery.OrderBy(p => p.Gender) :
                        patientsQuery.OrderByDescending(p => p.Gender),
                    "email" => sortOrder == "asc" ?
                        patientsQuery.OrderBy(p => p.Email) :
                        patientsQuery.OrderByDescending(p => p.Email),
                    "phone" => sortOrder == "asc" ?
                        patientsQuery.OrderBy(p => p.PhoneNumber) :
                        patientsQuery.OrderByDescending(p => p.PhoneNumber),
                    "age" => sortOrder == "asc" ?
                        patientsQuery.OrderBy(p => p.DateOfBirth) :
                        patientsQuery.OrderByDescending(p => p.DateOfBirth),
                    _ => patientsQuery.OrderBy(p => p.Name)
                };

                // Get total count for pagination
                var totalCount = await patientsQuery.CountAsync();

                // Apply pagination
                var patients = await patientsQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Json(new
                {
                    Success = true,
                    Patients = patients,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Success = false,
                    Message = "Error loading patients: " + ex.Message
                });
            }
        }

        public async Task<IActionResult> GetAppointmentsAjax(
            string statusFilter,
            string dateRangeFilter,
            int? doctorId,
            int? patientId,
            string sortBy = "AppointmentDate",
            string sortOrder = "asc",
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                var appointmentsQuery = _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .AsQueryable();

                // Apply status filter
                if (!string.IsNullOrEmpty(statusFilter))
                {
                    appointmentsQuery = appointmentsQuery.Where(a => a.Status == statusFilter);
                }

                // Apply date range filter
                if (!string.IsNullOrEmpty(dateRangeFilter))
                {
                    var today = DateTime.Today;
                    switch (dateRangeFilter)
                    {
                        case "Today":
                            appointmentsQuery = appointmentsQuery.Where(a => a.AppointmentDate.Date == today);
                            break;
                        case "ThisWeek":
                            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                            var endOfWeek = startOfWeek.AddDays(7);
                            appointmentsQuery = appointmentsQuery.Where(a => a.AppointmentDate >= startOfWeek && a.AppointmentDate < endOfWeek);
                            break;
                        case "ThisMonth":
                            appointmentsQuery = appointmentsQuery.Where(a => a.AppointmentDate.Month == today.Month && a.AppointmentDate.Year == today.Year);
                            break;
                    }
                }

                // Apply doctor filter
                if (doctorId.HasValue)
                {
                    appointmentsQuery = appointmentsQuery.Where(a => a.DoctorId == doctorId.Value);
                }

                // Apply patient filter
                if (patientId.HasValue)
                {
                    appointmentsQuery = appointmentsQuery.Where(a => a.PatientId == patientId.Value);
                }

                // Apply sorting
                appointmentsQuery = sortBy.ToLower() switch
                {
                    "id" => sortOrder == "asc" ?
                        appointmentsQuery.OrderBy(a => a.Id) :
                        appointmentsQuery.OrderByDescending(a => a.Id),
                    "patient" => sortOrder == "asc" ?
                        appointmentsQuery.OrderBy(a => a.Patient.Name) :
                        appointmentsQuery.OrderByDescending(a => a.Patient.Name),
                    "doctor" => sortOrder == "asc" ?
                        appointmentsQuery.OrderBy(a => a.Doctor.Name) :
                        appointmentsQuery.OrderByDescending(a => a.Doctor.Name),
                    "date" => sortOrder == "asc" ?
                        appointmentsQuery.OrderBy(a => a.AppointmentDate) :
                        appointmentsQuery.OrderByDescending(a => a.AppointmentDate),
                    "status" => sortOrder == "asc" ?
                        appointmentsQuery.OrderBy(a => a.Status) :
                        appointmentsQuery.OrderByDescending(a => a.Status),
                    _ => appointmentsQuery.OrderBy(a => a.AppointmentDate)
                };

                // Get total count for pagination
                var totalCount = await appointmentsQuery.CountAsync();

                // Apply pagination
                var appointments = await appointmentsQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Get dropdown data
                var doctors = await _context.Doctors.ToListAsync();
                var patients = await _context.Patients.ToListAsync();
                var statuses = new List<string> { "Pending", "Confirmed", "Completed", "Cancelled" };

                return Json(new
                {
                    Success = true,
                    Appointments = appointments,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    Doctors = doctors,
                    Patients = patients,
                    Statuses = statuses
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Success = false,
                    Message = "Error loading appointments: " + ex.Message
                });
            }
        }
        // Medical Records AJAX
        public async Task<IActionResult> GetMedicalRecordsAjax(
            string searchString,
            string bloodTypeFilter,
            string sortBy = "CreatedDate",
            string sortOrder = "desc",
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                var medicalRecordsQuery = _context.MedicalRecords
                    .Include(mr => mr.Patient)
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(searchString))
                {
                    medicalRecordsQuery = medicalRecordsQuery.Where(mr =>
                        (mr.Patient != null && mr.Patient.Name.Contains(searchString)) ||
                        (!string.IsNullOrEmpty(mr.Allergies) && mr.Allergies.Contains(searchString)) ||
                        (!string.IsNullOrEmpty(mr.CurrentMedications) && mr.CurrentMedications.Contains(searchString)) ||
                        (!string.IsNullOrEmpty(mr.PastMedicalHistory) && mr.PastMedicalHistory.Contains(searchString)));
                }

                // Apply blood type filter
                if (!string.IsNullOrEmpty(bloodTypeFilter))
                {
                    medicalRecordsQuery = medicalRecordsQuery.Where(mr => mr.BloodType == bloodTypeFilter);
                }

                // Apply sorting - FIXED: Handle null Patient references
                medicalRecordsQuery = sortBy.ToLower() switch
                {
                    "id" => sortOrder == "asc" ?
                        medicalRecordsQuery.OrderBy(mr => mr.Id) :
                        medicalRecordsQuery.OrderByDescending(mr => mr.Id),
                    "patient" => sortOrder == "asc" ?
                        medicalRecordsQuery.OrderBy(mr => mr.Patient != null ? mr.Patient.Name : "") :
                        medicalRecordsQuery.OrderByDescending(mr => mr.Patient != null ? mr.Patient.Name : ""),
                    "bloodtype" => sortOrder == "asc" ?
                        medicalRecordsQuery.OrderBy(mr => mr.BloodType) :
                        medicalRecordsQuery.OrderByDescending(mr => mr.BloodType),
                    "created" => sortOrder == "asc" ?
                        medicalRecordsQuery.OrderBy(mr => mr.CreatedDate) :
                        medicalRecordsQuery.OrderByDescending(mr => mr.CreatedDate),
                    _ => medicalRecordsQuery.OrderByDescending(mr => mr.CreatedDate)
                };

                var totalCount = await medicalRecordsQuery.CountAsync();

                // Apply pagination
                var medicalRecords = await medicalRecordsQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(mr => new
                    {
                        mr.Id,
                        mr.BloodType,
                        mr.Height,
                        mr.Weight,
                        mr.Allergies,
                        mr.CurrentMedications,
                        mr.PastMedicalHistory,
                        mr.CreatedDate,
                        mr.UpdatedDate,
                        Patient = mr.Patient != null ? new { mr.Patient.Id, mr.Patient.Name, mr.Patient.ProfileImage } : null
                    })
                    .ToListAsync();

                // Get distinct blood types
                var bloodTypes = await _context.MedicalRecords
                    .Where(mr => !string.IsNullOrEmpty(mr.BloodType))
                    .Select(mr => mr.BloodType)
                    .Distinct()
                    .ToListAsync();

                return Json(new
                {
                    Success = true,
                    MedicalRecords = medicalRecords,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    BloodTypes = bloodTypes
                });
            }
            catch (Exception ex)
            {
                // Log the actual error for debugging
                Console.WriteLine($"Error in GetMedicalRecordsAjax: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                return Json(new
                {
                    Success = false,
                    Message = $"Error loading medical records: {ex.Message}"
                });
            }
        }

        // Doctor Schedules AJAX
        public async Task<IActionResult> GetDoctorSchedulesAjax(
            int? doctorId,
            string dateRangeFilter,
            bool? isAvailable,
            string viewType = "list",
            string sortBy = "Date",
            string sortOrder = "asc",
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                var schedulesQuery = _context.DoctorSchedules
                    .Include(ds => ds.Doctor)
                    .AsQueryable();

                if (doctorId.HasValue)
                {
                    schedulesQuery = schedulesQuery.Where(ds => ds.DoctorId == doctorId.Value);
                }

                if (!string.IsNullOrEmpty(dateRangeFilter))
                {
                    var today = DateTime.Today;
                    switch (dateRangeFilter)
                    {
                        case "Today": schedulesQuery = schedulesQuery.Where(ds => ds.Date.Date == today); break;
                        case "ThisWeek": schedulesQuery = schedulesQuery.Where(ds => ds.Date >= today && ds.Date < today.AddDays(7)); break;
                        case "ThisMonth": schedulesQuery = schedulesQuery.Where(ds => ds.Date.Month == today.Month && ds.Date.Year == today.Year); break;
                        case "Upcoming": schedulesQuery = schedulesQuery.Where(ds => ds.Date >= today); break;
                        case "NextMonth": schedulesQuery = schedulesQuery.Where(ds => ds.Date.Month == today.AddMonths(1).Month && ds.Date.Year == today.AddMonths(1).Year); break;
                    }
                }

                if (isAvailable.HasValue)
                {
                    schedulesQuery = schedulesQuery.Where(ds => ds.IsAvailable == isAvailable.Value);
                }

                schedulesQuery = sortBy.ToLower() switch
                {
                    "id" => sortOrder == "asc" ? schedulesQuery.OrderBy(ds => ds.Id) : schedulesQuery.OrderByDescending(ds => ds.Id),
                    "doctor" => sortOrder == "asc" ? schedulesQuery.OrderBy(ds => ds.Doctor.Name) : schedulesQuery.OrderByDescending(ds => ds.Doctor.Name),
                    "date" => sortOrder == "asc" ? schedulesQuery.OrderBy(ds => ds.Date) : schedulesQuery.OrderByDescending(ds => ds.Date),
                    "starttime" => sortOrder == "asc" ? schedulesQuery.OrderBy(ds => ds.StartTime) : schedulesQuery.OrderByDescending(ds => ds.StartTime),
                    _ => schedulesQuery.OrderBy(ds => ds.Date).ThenBy(ds => ds.StartTime)
                };

                // Get appointments for calendar view
                List<Appointment> appointments = null;
                if (viewType == "calendar" && doctorId.HasValue)
                {
                    appointments = await _context.Appointments
                        .Include(a => a.Patient)
                        .Where(a => a.DoctorId == doctorId.Value)
                        .ToListAsync();
                }

                var totalCount = await schedulesQuery.CountAsync();
                var schedules = await schedulesQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var doctors = await _context.Doctors.ToListAsync();

                return Json(new
                {
                    Success = true,
                    Schedules = schedules,
                    Appointments = appointments,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    Doctors = doctors,
                    ViewType = viewType
                });
            }
            catch (Exception ex)
            {
                return Json(new { Success = false, Message = "Error loading schedules: " + ex.Message });
            }
        }

        // Payments AJAX
        public async Task<IActionResult> GetPaymentsAjax(
            string statusFilter,
            string paymentMethodFilter,
            string dateRangeFilter,
            string sortBy = "PaymentDate",
            string sortOrder = "desc",
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                var paymentsQuery = _context.Payments
                    .Include(p => p.Appointment)
                        .ThenInclude(a => a.Patient)
                    .Include(p => p.Appointment)
                        .ThenInclude(a => a.Doctor)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(statusFilter))
                {
                    paymentsQuery = paymentsQuery.Where(p => p.Status == statusFilter);
                }

                if (!string.IsNullOrEmpty(paymentMethodFilter))
                {
                    paymentsQuery = paymentsQuery.Where(p => p.PaymentMethod == paymentMethodFilter);
                }

                if (!string.IsNullOrEmpty(dateRangeFilter))
                {
                    var today = DateTime.Today;
                    switch (dateRangeFilter)
                    {
                        case "Today": paymentsQuery = paymentsQuery.Where(p => p.PaymentDate.Date == today); break;
                        case "ThisWeek": paymentsQuery = paymentsQuery.Where(p => p.PaymentDate >= today.AddDays(-(int)today.DayOfWeek) && p.PaymentDate < today.AddDays(7 - (int)today.DayOfWeek)); break;
                        case "ThisMonth": paymentsQuery = paymentsQuery.Where(p => p.PaymentDate.Month == today.Month && p.PaymentDate.Year == today.Year); break;
                    }
                }

                paymentsQuery = sortBy.ToLower() switch
                {
                    "id" => sortOrder == "asc" ? paymentsQuery.OrderBy(p => p.Id) : paymentsQuery.OrderByDescending(p => p.Id),
                    "patient" => sortOrder == "asc" ? paymentsQuery.OrderBy(p => p.Appointment.Patient.Name) : paymentsQuery.OrderByDescending(p => p.Appointment.Patient.Name),
                    "date" => sortOrder == "asc" ? paymentsQuery.OrderBy(p => p.PaymentDate) : paymentsQuery.OrderByDescending(p => p.PaymentDate),
                    "amount" => sortOrder == "asc" ? paymentsQuery.OrderBy(p => p.Amount) : paymentsQuery.OrderByDescending(p => p.Amount),
                    "method" => sortOrder == "asc" ? paymentsQuery.OrderBy(p => p.PaymentMethod) : paymentsQuery.OrderByDescending(p => p.PaymentMethod),
                    "status" => sortOrder == "asc" ? paymentsQuery.OrderBy(p => p.Status) : paymentsQuery.OrderByDescending(p => p.Status),
                    _ => paymentsQuery.OrderByDescending(p => p.PaymentDate)
                };

                var totalCount = await paymentsQuery.CountAsync();
                var payments = await paymentsQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new
                    {
                        p.Id,
                        p.AppointmentId,
                        p.Amount,
                        p.PaymentMethod,
                        p.Status,
                        p.PaymentDate,
                        p.TransactionId,
                        Appointment = p.Appointment != null ? new
                        {
                            Patient = p.Appointment.Patient != null ? new
                            {
                                p.Appointment.Patient.Name,
                                p.Appointment.Patient.Email
                            } : null,
                            Doctor = p.Appointment.Doctor != null ? new
                            {
                                p.Appointment.Doctor.Name,
                                p.Appointment.Doctor.Specialty
                            } : null
                        } : null
                    })
                    .ToListAsync();

                var statuses = await _context.Payments
                    .Where(p => p.Status != null)
                    .Select(p => p.Status)
                    .Distinct()
                    .ToListAsync();

                var paymentMethods = await _context.Payments
                    .Where(p => p.PaymentMethod != null)
                    .Select(p => p.PaymentMethod)
                    .Distinct()
                    .ToListAsync();

                return Json(new
                {
                    Success = true,
                    Payments = payments,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    Statuses = statuses,
                    PaymentMethods = paymentMethods
                });
            }
            catch (Exception ex)
            {
                return Json(new { Success = false, Message = "Error loading payments: " + ex.Message });
            }
        }

        // Reviews AJAX
        public async Task<IActionResult> GetReviewsAjax(
            int? ratingFilter,
            int? doctorId,
            string dateRangeFilter,
            string sortBy = "Date",
            string sortOrder = "desc",
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                var reviewsQuery = _context.Reviews
                    .Include(r => r.Patient)
                    .Include(r => r.Doctor)
                    .AsQueryable();

                if (ratingFilter.HasValue)
                {
                    reviewsQuery = reviewsQuery.Where(r => r.Rating == ratingFilter.Value);
                }

                if (doctorId.HasValue)
                {
                    reviewsQuery = reviewsQuery.Where(r => r.DoctorId == doctorId.Value);
                }

                if (!string.IsNullOrEmpty(dateRangeFilter))
                {
                    var today = DateTime.Today;
                    switch (dateRangeFilter)
                    {
                        case "Today": reviewsQuery = reviewsQuery.Where(r => r.Date.Date == today); break;
                        case "ThisWeek": reviewsQuery = reviewsQuery.Where(r => r.Date >= today.AddDays(-(int)today.DayOfWeek) && r.Date < today.AddDays(7 - (int)today.DayOfWeek)); break;
                        case "ThisMonth": reviewsQuery = reviewsQuery.Where(r => r.Date.Month == today.Month && r.Date.Year == today.Year); break;
                        case "ThisYear": reviewsQuery = reviewsQuery.Where(r => r.Date.Year == today.Year); break;
                    }
                }

                reviewsQuery = sortBy.ToLower() switch
                {
                    "id" => sortOrder == "asc" ? reviewsQuery.OrderBy(r => r.Id) : reviewsQuery.OrderByDescending(r => r.Id),
                    "patient" => sortOrder == "asc" ? reviewsQuery.OrderBy(r => r.Patient.Name) : reviewsQuery.OrderByDescending(r => r.Patient.Name),
                    "doctor" => sortOrder == "asc" ? reviewsQuery.OrderBy(r => r.Doctor.Name) : reviewsQuery.OrderByDescending(r => r.Doctor.Name),
                    "rating" => sortOrder == "asc" ? reviewsQuery.OrderBy(r => r.Rating) : reviewsQuery.OrderByDescending(r => r.Rating),
                    "date" => sortOrder == "asc" ? reviewsQuery.OrderBy(r => r.Date) : reviewsQuery.OrderByDescending(r => r.Date),
                    _ => reviewsQuery.OrderByDescending(r => r.Date)
                };

                var totalCount = await reviewsQuery.CountAsync();
                var reviews = await reviewsQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var doctors = await _context.Doctors.ToListAsync();

                return Json(new
                {
                    Success = true,
                    Reviews = reviews,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    Doctors = doctors
                });
            }
            catch (Exception ex)
            {
                return Json(new { Success = false, Message = "Error loading reviews: " + ex.Message });
            }
        }

        public async Task<IActionResult> Invoice(int id)
        {
            try
            {
                var payment = await _context.Payments
                    .Include(p => p.Appointment)
                        .ThenInclude(a => a.Patient)
                    .Include(p => p.Appointment)
                        .ThenInclude(a => a.Doctor)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (payment == null)
                {
                    return NotFound();
                }

                var invoice = new PaymentInvoiceViewModel
                {
                    Id = payment.Id,
                    Amount = payment.Amount,
                    PaymentMethod = payment.PaymentMethod,
                    Status = payment.Status,
                    PaymentDate = payment.PaymentDate,
                    TransactionId = payment.TransactionId ?? $"PY-{payment.Id.ToString("D4")}",

                    AppointmentId = payment.AppointmentId,
                    AppointmentDate = payment.Appointment.AppointmentDate,
                    AppointmentNotes = payment.Appointment.Notes,

                    PatientName = payment.Appointment.Patient?.Name,
                    PatientEmail = payment.Appointment.Patient?.Email,
                    PatientPhone = payment.Appointment.Patient?.PhoneNumber,

                    DoctorName = payment.Appointment.Doctor?.Name,
                    DoctorSpecialty = payment.Appointment.Doctor?.Specialty,
                    DoctorEmail = payment.Appointment.Doctor?.Email,
                    DoctorPhone = payment.Appointment.Doctor?.PhoneNumber
                };

                return View(invoice);
            }
            catch (Exception ex)
            {
                // Log error
                return StatusCode(500, "Error generating invoice: " + ex.Message);
            }
        }


        public async Task<IActionResult> Print(int id)
        {
            try
            {
                var payment = await _context.Payments
                    .Include(p => p.Appointment)
                        .ThenInclude(a => a.Patient)
                    .Include(p => p.Appointment)
                        .ThenInclude(a => a.Doctor)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (payment == null)
                {
                    return NotFound();
                }

                var invoice = new PaymentInvoiceViewModel
                {
                    Id = payment.Id,
                    Amount = payment.Amount,
                    PaymentMethod = payment.PaymentMethod,
                    Status = payment.Status,
                    PaymentDate = payment.PaymentDate,
                    TransactionId = payment.TransactionId ?? $"PY-{payment.Id.ToString("D4")}",

                    AppointmentId = payment.AppointmentId,
                    AppointmentDate = payment.Appointment.AppointmentDate,
                    AppointmentNotes = payment.Appointment.Notes,

                    PatientName = payment.Appointment.Patient?.Name,
                    PatientEmail = payment.Appointment.Patient?.Email,
                    PatientPhone = payment.Appointment.Patient?.PhoneNumber,

                    DoctorName = payment.Appointment.Doctor?.Name,
                    DoctorSpecialty = payment.Appointment.Doctor?.Specialty,
                    DoctorEmail = payment.Appointment.Doctor?.Email,
                    DoctorPhone = payment.Appointment.Doctor?.PhoneNumber
                };

                return View("InvoicePrint", invoice);
            }
            catch (Exception ex)
            {
                // Log error
                return StatusCode(500, "Error generating print view: " + ex.Message);
            }
        }

        public async Task<IActionResult> DownloadPdf(int id)
        {
            try
            {
                var payment = await _context.Payments
                    .Include(p => p.Appointment)
                        .ThenInclude(a => a.Patient)
                    .Include(p => p.Appointment)
                        .ThenInclude(a => a.Doctor)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (payment == null)
                {
                    return NotFound();
                }

                var invoice = new PaymentInvoiceViewModel
                {
                    Id = payment.Id,
                    Amount = payment.Amount,
                    PaymentMethod = payment.PaymentMethod,
                    Status = payment.Status,
                    PaymentDate = payment.PaymentDate,
                    TransactionId = payment.TransactionId ?? $"PY-{payment.Id.ToString("D4")}",

                    AppointmentId = payment.AppointmentId,
                    AppointmentDate = payment.Appointment.AppointmentDate,
                    AppointmentNotes = payment.Appointment.Notes,

                    PatientName = payment.Appointment.Patient?.Name,
                    PatientEmail = payment.Appointment.Patient?.Email,
                    PatientPhone = payment.Appointment.Patient?.PhoneNumber,

                    DoctorName = payment.Appointment.Doctor?.Name,
                    DoctorSpecialty = payment.Appointment.Doctor?.Specialty,
                    DoctorEmail = payment.Appointment.Doctor?.Email,
                    DoctorPhone = payment.Appointment.Doctor?.PhoneNumber
                };

                // For PDF generation, you might want to use a library like Rotativa or DinkToPdf
                // This is a basic implementation that returns the print view
                return View("InvoicePrint", invoice);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error generating PDF: " + ex.Message);
            }
        }

        // Remote validation for Email
        public JsonResult VerifyEmail(string email, int id)
        {
            bool exists = _context.Doctors.Any(d => d.Email == email && d.Id != id);
            if (exists)
            {
                return Json($"Email '{email}' is already taken.");
            }
            return Json(true);
        }

        // Remote validation for Phone
        public JsonResult VerifyPhone(string phoneNumber, int id)
        {
            bool exists = _context.Doctors.Any(d => d.PhoneNumber == phoneNumber && d.Id != id);
            if (exists)
            {
                return Json($"Phone number '{phoneNumber}' is already registered.");
            }
            return Json(true);
        }

        // Remote validation for Patient Email
        public JsonResult VerifyPatientEmail(string email, int id)
        {
            bool exists = _context.Patients.Any(p => p.Email == email && p.Id != id);
            if (exists)
            {
                return Json($"Email '{email}' is already taken by another patient.");
            }
            return Json(true);
        }

        // Remote validation for Patient Phone
        public JsonResult VerifyPatientPhone(string phoneNumber, int id)
        {
            bool exists = _context.Patients.Any(p => p.PhoneNumber == phoneNumber && p.Id != id);
            if (exists)
            {
                return Json($"Phone number '{phoneNumber}' is already registered.");
            }
            return Json(true);
        }
    }
}