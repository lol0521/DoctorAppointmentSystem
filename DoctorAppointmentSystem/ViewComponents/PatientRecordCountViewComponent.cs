using Microsoft.AspNetCore.Mvc;
using DoctorAppointmentSystem.Data;

namespace DoctorAppointmentSystem.ViewComponents
{
    public class PatientRecordCountViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public PatientRecordCountViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke(int Id)
        {
            var count = _context.MedicalRecords.Count(mr => mr.Id == Id);
            return View(count);
        }
    }
}