using DoctorAppointmentSystem.Data;
using DoctorAppointmentSystem.Models;
using DoctorAppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

public class AppointmentController : Controller
{
    private readonly ApplicationDbContext _context;

    public AppointmentController(ApplicationDbContext context)
    {
        _context = context;
    }

    // STEP 1: Select specialty/doctor/concern
    public IActionResult Step1()
    {
        var specialties = _context.Doctors
            .Where(d => !string.IsNullOrEmpty(d.Specialty))
            .Select(d => d.Specialty)
            .Distinct()
            .ToList();

        ViewBag.Specialties = specialties;
        return View("BookStep1");
    }

    [HttpGet]
    public IActionResult Step2(string specialty, string doctor, string concern)
    {
        // Save Step1 data for later
        TempData["specialty"] = specialty;
        TempData["doctor"] = doctor;
        TempData["concern"] = concern;

        // Get logged-in user
        var userIdClaim = User.FindFirst("UserId");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userAccountId))
        {
            TempData["Error"] = "You must log in before booking.";
            return RedirectToAction("Step1");
        }

        var patient = _context.UserAccounts
            .Include(ua => ua.Patient)
            .FirstOrDefault(ua => ua.Id == userAccountId)?.Patient;

        if (patient == null)
        {
            TempData["Error"] = "Patient record not found.";
            return RedirectToAction("Step1");
        }

        // Pre-fill the form
        var viewModel = new PatientViewModel
        {
            Name = patient.Name,
            DateOfBirth = patient.DateOfBirth,
            PhoneNumber = patient.PhoneNumber,
            Email = patient.Email
        };

        return View("BookStep2", viewModel);
    }

    [HttpPost]
    public IActionResult Step2(PatientViewModel model, string marketing)
    {
        if (!ModelState.IsValid)
            return View("BookStep2", model);

        var userIdClaim = User.FindFirst("UserId");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userAccountId))
        {
            TempData["Error"] = "Session expired. Please log in again.";
            return RedirectToAction("Step1");
        }

        var patient = _context.UserAccounts
            .Include(ua => ua.Patient)
            .FirstOrDefault(ua => ua.Id == userAccountId)?.Patient;

        if (patient == null)
            return RedirectToAction("Step1");

        // Update patient data
        patient.Name = model.Name;
        patient.DateOfBirth = model.DateOfBirth;
        patient.PhoneNumber = model.PhoneNumber;
        patient.Email = model.Email;

        _context.SaveChanges();

        TempData["marketing"] = marketing;
        TempData["PatientId"] = patient.Id;

        return RedirectToAction("Step3");
    }

    // STEP 3: Confirm
    public IActionResult Step3()
    {
        ViewBag.Specialty = TempData["specialty"];
        ViewBag.Doctor = TempData["doctor"];
        ViewBag.Concern = TempData["concern"];
        ViewBag.AppointmentId = TempData["AppointmentId"];

        var patientId = TempData["PatientId"];
        var marketing = TempData["marketing"];

        var patient = _context.Patients.FirstOrDefault(p => p.Id == (int)patientId);

        ViewBag.Patient = patient;
        ViewBag.Marketing = marketing;

        return View("BookStep3");
    }

    // AJAX: Get doctors by specialty
    [HttpGet]
    public IActionResult GetDoctorsBySpecialty(string specialty)
    {
        var doctors = _context.Doctors
            .Where(d => d.Specialty == specialty)
            .Select(d => new { id = d.Id, name = d.Name })
            .ToList();

        return Json(doctors);
    }

    [HttpPost]
    public IActionResult Step1Next(string specialty, int doctor, string concern, DateTime appointmentDate, string timeSlot, int duration)
    {
        var userIdClaim = User.FindFirst("UserId");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userAccountId))
        {
            TempData["Error"] = "You must log in before booking.";
            return RedirectToAction("Step1");
        }

        var patient = _context.UserAccounts
            .Include(ua => ua.Patient)
            .FirstOrDefault(ua => ua.Id == userAccountId)?.Patient;

        if (patient == null)
        {
            TempData["Error"] = "Patient record not found.";
            return RedirectToAction("Step1");
        }

        // Extract start time
        DateTime finalDateTime = appointmentDate;
        if (!string.IsNullOrEmpty(timeSlot))
        {
            var startTime = timeSlot.Split('-')[0].Trim();
            if (TimeSpan.TryParse(startTime, out var start))
            {
                finalDateTime = appointmentDate.Date.Add(start);
            }
        }

        // Save appointment with duration
        var appointment = new Appointment
        {
            DoctorId = doctor,
            PatientId = patient.Id,
            AppointmentDate = finalDateTime,
            Duration = duration,
            Status = "Pending",
            Notes = concern
        };

        _context.Appointments.Add(appointment);
        _context.SaveChanges();

        TempData["AppointmentId"] = appointment.Id;
        TempData["specialty"] = specialty;

        return RedirectToAction("Step2");
    }

    [HttpGet]
    public IActionResult GetAvailableSlots(int doctorId, DateTime date, int duration)
    {
        // Get doctor's schedules for the day
        var schedules = _context.DoctorSchedules
            .Where(s => s.DoctorId == doctorId && s.Date.Date == date.Date && s.IsAvailable)
            .ToList();

        if (!schedules.Any())
            return Json(new List<string>()); // No schedule for this day

        // Get already booked appointments for that doctor on this date
        var bookedAppointments = _context.Appointments
            .Where(a => a.DoctorId == doctorId && a.AppointmentDate.Date == date.Date)
            .Select(a => new
            {
                Start = a.AppointmentDate.TimeOfDay,
                Duration = TimeSpan.FromMinutes(a.Duration) // Now dynamic
            })
            .ToList();

        var availableSlots = new List<string>();

        foreach (var schedule in schedules)
        {
            var start = schedule.StartTime;
            var end = schedule.EndTime;

            while (start.Add(TimeSpan.FromMinutes(duration)) <= end)
            {
                var slotStart = start;
                var slotEnd = start.Add(TimeSpan.FromMinutes(duration));

                bool overlaps = bookedAppointments.Any(b =>
                {
                    var bookedStart = b.Start;
                    var bookedEnd = b.Start.Add(b.Duration);

                    return slotStart < bookedEnd && slotEnd > bookedStart;
                });

                if (!overlaps)
                {
                    availableSlots.Add($"{slotStart:hh\\:mm} - {slotEnd:hh\\:mm}");
                }

                start = start.Add(TimeSpan.FromMinutes(30)); // keep 30 min interval stepping
            }
        }

        return Json(availableSlots);
    }
}