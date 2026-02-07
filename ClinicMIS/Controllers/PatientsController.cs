using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ClinicMIS.Data;
using ClinicMIS.Models.Entities;
using ClinicMIS.Models.ViewModels;
using ClinicMIS.Services;

namespace ClinicMIS.Controllers;

/// <summary>
/// Patient management controller
/// Handles patient registration, search, and profile management
/// </summary>
[Authorize(Policy = "CanViewPatients")]
public class PatientsController : Controller
{
    private readonly IPatientService _patientService;
    private readonly ClinicDbContext _context;
    private readonly ILogger<PatientsController> _logger;

    public PatientsController(
        IPatientService patientService,
        ClinicDbContext context,
        ILogger<PatientsController> logger)
    {
        _patientService = patientService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// GET: Patients - List with search and pagination
    /// </summary>
    public async Task<IActionResult> Index(
        string? searchTerm,
        string? sortBy,
        bool sortDesc = false,
        int page = 1,
        int pageSize = 10)
    {
        var model = await _patientService.GetPatientsAsync(searchTerm, sortBy, sortDesc, page, pageSize);
        return View(model);
    }

    /// <summary>
    /// GET: Patients/Details/5
    /// </summary>
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var model = await _patientService.GetPatientDetailsAsync(id);
            return View(model);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// GET: Patients/Create
    /// </summary>
    [Authorize(Policy = "CanEditPatients")]
    public IActionResult Create()
    {
        return View(new PatientCreateViewModel());
    }

    /// <summary>
    /// POST: Patients/Create
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanEditPatients")]
    public async Task<IActionResult> Create(PatientCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var patient = await _patientService.CreateAsync(model, User.Identity?.Name ?? "System");
            TempData["SuccessMessage"] = $"Patient registered successfully. Clinic Number: {patient.ClinicNumber}";
            return RedirectToAction(nameof(Details), new { id = patient.PatientId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating patient");
            ModelState.AddModelError("", "An error occurred while registering the patient.");
            return View(model);
        }
    }

    /// <summary>
    /// GET: Patients/Edit/5
    /// </summary>
    [Authorize(Policy = "CanEditPatients")]
    public async Task<IActionResult> Edit(int id)
    {
        var patient = await _patientService.GetByIdAsync(id);
        if (patient == null)
        {
            return NotFound();
        }

        var model = new PatientCreateViewModel
        {
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            DateOfBirth = patient.DateOfBirth,
            Gender = patient.Gender,
            NationalId = patient.NationalId,
            PhoneNumber = patient.PhoneNumber,
            Email = patient.Email,
            Address = patient.Address,
            City = patient.City,
            EmergencyContactName = patient.EmergencyContactName,
            EmergencyContactPhone = patient.EmergencyContactPhone,
            BloodType = patient.BloodType,
            Allergies = patient.Allergies
        };

        ViewBag.PatientId = id;
        ViewBag.ClinicNumber = patient.ClinicNumber;
        return View(model);
    }

    /// <summary>
    /// POST: Patients/Edit/5
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanEditPatients")]
    public async Task<IActionResult> Edit(int id, PatientCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.PatientId = id;
            return View(model);
        }

        try
        {
            await _patientService.UpdateAsync(id, model, User.Identity?.Name ?? "System");
            TempData["SuccessMessage"] = "Patient updated successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError("", "The record was modified by another user. Please reload and try again.");
            ViewBag.PatientId = id;
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating patient {PatientId}", id);
            ModelState.AddModelError("", "An error occurred while updating the patient.");
            ViewBag.PatientId = id;
            return View(model);
        }
    }

    /// <summary>
    /// GET: Patients/Delete/5
    /// </summary>
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.PatientId == id);
        if (patient == null)
        {
            return NotFound();
        }

        // Check if patient has any related records
        var visitCount = await _context.Visits.CountAsync(v => v.PatientId == id);
        var prescriptionCount = await _context.Prescriptions.CountAsync(p => p.PatientId == id);

        ViewBag.VisitCount = visitCount;
        ViewBag.PrescriptionCount = prescriptionCount;
        ViewBag.HasRelatedRecords = visitCount > 0 || prescriptionCount > 0;

        return View(patient);
    }

    /// <summary>
    /// POST: Patients/Delete/5
    /// </summary>
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await _patientService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Patient record deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting patient {PatientId}", id);
            TempData["ErrorMessage"] = "An error occurred while deleting the patient.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    /// <summary>
    /// API: Search patients (for AJAX autocomplete)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Search(string term)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
        {
            return Json(new List<object>());
        }

        var patients = await _context.Patients
            .Where(p => p.ClinicNumber.Contains(term) ||
                       p.FirstName.Contains(term) ||
                       p.LastName.Contains(term) ||
                       p.PhoneNumber.Contains(term))
            .Take(10)
            .Select(p => new
            {
                p.PatientId,
                p.ClinicNumber,
                FullName = p.FirstName + " " + p.LastName,
                p.PhoneNumber
            })
            .ToListAsync();

        return Json(patients);
    }
}
