using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ClinicMIS.Data;
using ClinicMIS.Models.Entities;
using ClinicMIS.Models.ViewModels;

namespace ClinicMIS.Controllers;

/// <summary>
/// Visit management controller
/// Handles patient check-in, consultations, and visit tracking
/// </summary>
[Authorize(Policy = "CanViewPatients")]
public class VisitsController : Controller
{
    private readonly ClinicDbContext _context;
    private readonly ILogger<VisitsController> _logger;

    public VisitsController(ClinicDbContext context, ILogger<VisitsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// GET: Visits - List with filtering
    /// </summary>
    public async Task<IActionResult> Index(VisitListViewModel filter)
    {
        var query = _context.Visits
            .Include(v => v.Patient)
            .Include(v => v.Clinic)
            .Include(v => v.Doctor)
            .AsQueryable();

        // Apply filters
        if (filter.FromDate.HasValue)
            query = query.Where(v => v.VisitDate >= filter.FromDate.Value);
        
        if (filter.ToDate.HasValue)
            query = query.Where(v => v.VisitDate <= filter.ToDate.Value);
        
        if (filter.ClinicId.HasValue)
            query = query.Where(v => v.ClinicId == filter.ClinicId.Value);
        
        if (filter.DoctorId.HasValue)
            query = query.Where(v => v.DoctorId == filter.DoctorId.Value);
        
        if (filter.Status.HasValue)
            query = query.Where(v => v.Status == filter.Status.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(v =>
                v.VisitNumber.ToLower().Contains(term) ||
                v.Patient.ClinicNumber.ToLower().Contains(term) ||
                v.Patient.FirstName.ToLower().Contains(term) ||
                v.Patient.LastName.ToLower().Contains(term));
        }

        // Get total count
        filter.TotalCount = await query.CountAsync();

        // Sorting
        query = filter.SortBy?.ToLower() switch
        {
            "patient" => filter.SortDescending
                ? query.OrderByDescending(v => v.Patient.LastName)
                : query.OrderBy(v => v.Patient.LastName),
            "clinic" => filter.SortDescending
                ? query.OrderByDescending(v => v.Clinic.Name)
                : query.OrderBy(v => v.Clinic.Name),
            "status" => filter.SortDescending
                ? query.OrderByDescending(v => v.Status)
                : query.OrderBy(v => v.Status),
            _ => filter.SortDescending
                ? query.OrderByDescending(v => v.VisitDate)
                : query.OrderBy(v => v.VisitDate)
        };

        // Pagination
        filter.Visits = await query
            .Skip((filter.CurrentPage - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(v => new VisitListItem
            {
                VisitId = v.VisitId,
                VisitNumber = v.VisitNumber,
                PatientName = v.Patient.FirstName + " " + v.Patient.LastName,
                PatientClinicNumber = v.Patient.ClinicNumber,
                ClinicName = v.Clinic.Name,
                DoctorName = v.Doctor != null 
                    ? "Dr. " + v.Doctor.FirstName + " " + v.Doctor.LastName 
                    : "Not Assigned",
                VisitDate = v.VisitDate,
                Status = v.Status,
                HasPrescription = v.Prescriptions.Any()
            })
            .ToListAsync();

        // Populate dropdowns
        filter.AvailableClinics = await _context.Clinics.Where(c => c.IsActive).ToListAsync();
        filter.AvailableDoctors = await _context.Staff
            .Where(s => s.IsActive && s.Role == StaffRole.Doctor)
            .ToListAsync();

        return View(filter);
    }

    /// <summary>
    /// GET: Visits/Create
    /// </summary>
    public async Task<IActionResult> Create(int? patientId)
    {
        var model = new VisitCreateViewModel
        {
            VisitDate = DateTime.Today,
            PatientId = patientId ?? 0,
            AvailableClinics = await _context.Clinics.Where(c => c.IsActive).ToListAsync(),
            AvailableDoctors = await _context.Staff
                .Where(s => s.IsActive && s.Role == StaffRole.Doctor)
                .ToListAsync()
        };

        if (patientId.HasValue)
        {
            model.AvailablePatients = await _context.Patients
                .Where(p => p.PatientId == patientId.Value)
                .ToListAsync();
        }

        return View(model);
    }

    /// <summary>
    /// POST: Visits/Create
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VisitCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableClinics = await _context.Clinics.Where(c => c.IsActive).ToListAsync();
            model.AvailableDoctors = await _context.Staff
                .Where(s => s.IsActive && s.Role == StaffRole.Doctor)
                .ToListAsync();
            return View(model);
        }

        var visit = new Visit
        {
            VisitNumber = await GenerateVisitNumberAsync(),
            PatientId = model.PatientId,
            ClinicId = model.ClinicId,
            DoctorId = model.DoctorId,
            VisitDate = model.VisitDate,
            ReasonForVisit = model.ReasonForVisit,
            Status = VisitStatus.Scheduled
        };

        _context.Visits.Add(visit);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Visit scheduled successfully. Visit Number: {visit.VisitNumber}";
        return RedirectToAction(nameof(Details), new { id = visit.VisitId });
    }

    /// <summary>
    /// GET: Visits/Details/5
    /// </summary>
    public async Task<IActionResult> Details(int id)
    {
        var visit = await _context.Visits
            .Include(v => v.Patient)
            .Include(v => v.Clinic)
            .Include(v => v.Doctor)
            .Include(v => v.Prescriptions)
                .ThenInclude(p => p.PrescriptionItems)
                    .ThenInclude(pi => pi.Drug)
            .FirstOrDefaultAsync(v => v.VisitId == id);

        if (visit == null)
        {
            return NotFound();
        }

        return View(visit);
    }

    /// <summary>
    /// POST: Visits/CheckIn/5
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn(int id)
    {
        var visit = await _context.Visits.FindAsync(id);
        if (visit == null)
        {
            return NotFound();
        }

        visit.Status = VisitStatus.CheckedIn;
        visit.CheckInTime = DateTime.Now.TimeOfDay;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Patient checked in successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// GET: Visits/Consultation/5
    /// Doctor consultation view
    /// </summary>
    [Authorize(Policy = "DoctorOnly")]
    public async Task<IActionResult> Consultation(int id)
    {
        var visit = await _context.Visits
            .Include(v => v.Patient)
            .Include(v => v.Clinic)
            .FirstOrDefaultAsync(v => v.VisitId == id);

        if (visit == null)
        {
            return NotFound();
        }

        var model = new ConsultationViewModel
        {
            VisitId = id,
            Visit = visit,
            Patient = visit.Patient,
            BloodPressure = visit.BloodPressure,
            Temperature = visit.Temperature,
            PulseRate = visit.PulseRate,
            Weight = visit.Weight,
            Height = visit.Height,
            Symptoms = visit.Symptoms,
            Diagnosis = visit.Diagnosis,
            DoctorNotes = visit.DoctorNotes,
            FollowUpRequired = visit.FollowUpRequired,
            FollowUpDate = visit.FollowUpDate
        };

        return View(model);
    }

    /// <summary>
    /// POST: Visits/Consultation/5
    /// Save consultation details
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "DoctorOnly")]
    public async Task<IActionResult> Consultation(int id, ConsultationViewModel model)
    {
        var visit = await _context.Visits.FindAsync(id);
        if (visit == null)
        {
            return NotFound();
        }

        // Update vital signs
        visit.BloodPressure = model.BloodPressure;
        visit.Temperature = model.Temperature;
        visit.PulseRate = model.PulseRate;
        visit.Weight = model.Weight;
        visit.Height = model.Height;

        // Update consultation details
        visit.Symptoms = model.Symptoms;
        visit.Diagnosis = model.Diagnosis;
        visit.DoctorNotes = model.DoctorNotes;
        visit.FollowUpRequired = model.FollowUpRequired;
        visit.FollowUpDate = model.FollowUpDate;

        // Update status
        visit.Status = VisitStatus.InProgress;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Consultation saved successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// POST: Visits/Complete/5
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "DoctorOnly")]
    public async Task<IActionResult> Complete(int id)
    {
        var visit = await _context.Visits.FindAsync(id);
        if (visit == null)
        {
            return NotFound();
        }

        visit.Status = VisitStatus.Completed;
        visit.CheckOutTime = DateTime.Now.TimeOfDay;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Visit completed successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<string> GenerateVisitNumberAsync()
    {
        var today = DateTime.Today;
        var prefix = $"VIS-{today:yyyyMMdd}-";

        var lastNumber = await _context.Visits
            .IgnoreQueryFilters()
            .Where(v => v.VisitNumber.StartsWith(prefix))
            .OrderByDescending(v => v.VisitNumber)
            .Select(v => v.VisitNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (!string.IsNullOrEmpty(lastNumber))
        {
            var numPart = lastNumber.Replace(prefix, "");
            if (int.TryParse(numPart, out int parsed))
            {
                nextNumber = parsed + 1;
            }
        }

        return $"{prefix}{nextNumber:D4}";
    }
}
