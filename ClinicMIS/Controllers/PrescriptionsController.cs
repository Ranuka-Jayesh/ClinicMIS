using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ClinicMIS.Data;
using ClinicMIS.Models.Entities;
using ClinicMIS.Models.ViewModels;
using ClinicMIS.Services;
using System.Security.Claims;

namespace ClinicMIS.Controllers;

/// <summary>
/// Prescription management controller
/// Handles prescription creation by doctors
/// </summary>
[Authorize(Policy = "CanPrescribe")]
public class PrescriptionsController : Controller
{
    private readonly ClinicDbContext _context;
    private readonly IPrescriptionService _prescriptionService;
    private readonly ILogger<PrescriptionsController> _logger;

    public PrescriptionsController(
        ClinicDbContext context,
        IPrescriptionService prescriptionService,
        ILogger<PrescriptionsController> logger)
    {
        _context = context;
        _prescriptionService = prescriptionService;
        _logger = logger;
    }

    /// <summary>
    /// GET: Prescriptions - List all prescriptions
    /// </summary>
    public async Task<IActionResult> Index(
        string? searchTerm,
        PrescriptionStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        int page = 1)
    {
        var query = _context.Prescriptions
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .Include(p => p.PrescriptionItems)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(p =>
                p.PrescriptionNumber.ToLower().Contains(term) ||
                p.Patient.ClinicNumber.ToLower().Contains(term) ||
                p.Patient.FirstName.ToLower().Contains(term) ||
                p.Patient.LastName.ToLower().Contains(term));
        }

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (fromDate.HasValue)
            query = query.Where(p => p.PrescriptionDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(p => p.PrescriptionDate <= toDate.Value);

        var prescriptions = await query
            .OrderByDescending(p => p.PrescriptionDate)
            .ThenByDescending(p => p.CreatedAt)
            .Skip((page - 1) * 10)
            .Take(10)
            .ToListAsync();

        ViewBag.SearchTerm = searchTerm;
        ViewBag.Status = status;
        ViewBag.FromDate = fromDate;
        ViewBag.ToDate = toDate;
        ViewBag.CurrentPage = page;
        ViewBag.TotalCount = await query.CountAsync();

        return View(prescriptions);
    }

    /// <summary>
    /// GET: Prescriptions/Details/5
    /// </summary>
    public async Task<IActionResult> Details(int id)
    {
        var prescription = await _prescriptionService.GetByIdAsync(id);
        if (prescription == null)
        {
            return NotFound();
        }

        return View(prescription);
    }

    /// <summary>
    /// GET: Prescriptions/Create
    /// </summary>
    public async Task<IActionResult> Create(int? patientId, int? visitId)
    {
        // Check if current user is a doctor
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var currentDoctor = await _context.Staff.FirstOrDefaultAsync(s => s.UserId == userId && s.Role == StaffRole.Doctor);
        
        var model = new PrescriptionCreateViewModel
        {
            PatientId = patientId ?? 0,
            VisitId = visitId,
            CurrentUserIsDoctor = currentDoctor != null,
            DoctorId = currentDoctor?.StaffId,
            AvailableDrugs = await _context.Drugs
                .Where(d => d.IsActive && d.QuantityInStock > 0)
                .OrderBy(d => d.Name)
                .ToListAsync(),
            AvailableDoctors = await _context.Staff
                .Where(s => s.IsActive && s.Role == StaffRole.Doctor)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync()
        };

        if (patientId.HasValue)
        {
            // Pre-selected patient
            model.AvailablePatients = await _context.Patients
                .Where(p => p.PatientId == patientId.Value)
                .ToListAsync();
        }
        else
        {
            // Load all patients for selection
            model.AvailablePatients = await _context.Patients
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToListAsync();
        }

        return View(model);
    }

    /// <summary>
    /// POST: Prescriptions/Create
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PrescriptionCreateViewModel model)
    {
        if (!ModelState.IsValid || !model.Items.Any())
        {
            if (!model.Items.Any())
            {
                ModelState.AddModelError("", "Please add at least one medication.");
            }

            await PopulateCreateViewModelAsync(model);
            return View(model);
        }

        try
        {
            int doctorId;
            
            // Check if a doctor was explicitly selected
            if (model.DoctorId.HasValue && model.DoctorId.Value > 0)
            {
                // Verify the selected doctor exists and is active
                var selectedDoctor = await _context.Staff
                    .FirstOrDefaultAsync(s => s.StaffId == model.DoctorId.Value && s.IsActive && s.Role == StaffRole.Doctor);
                
                if (selectedDoctor == null)
                {
                    ModelState.AddModelError("DoctorId", "Selected doctor is not valid.");
                    await PopulateCreateViewModelAsync(model);
                    return View(model);
                }
                
                doctorId = selectedDoctor.StaffId;
            }
            else
            {
                // Try to get current user's doctor ID
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var currentDoctor = await _context.Staff.FirstOrDefaultAsync(s => s.UserId == userId && s.Role == StaffRole.Doctor);
                
                if (currentDoctor == null)
                {
                    ModelState.AddModelError("DoctorId", "Please select a prescribing doctor.");
                    await PopulateCreateViewModelAsync(model);
                    return View(model);
                }
                
                doctorId = currentDoctor.StaffId;
            }

            var prescription = await _prescriptionService.CreateAsync(model, doctorId);
            TempData["SuccessMessage"] = $"Prescription created successfully. Number: {prescription.PrescriptionNumber}";
            return RedirectToAction(nameof(Details), new { id = prescription.PrescriptionId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating prescription");
            ModelState.AddModelError("", "An error occurred while creating the prescription.");
            await PopulateCreateViewModelAsync(model);
            return View(model);
        }
    }

    /// <summary>
    /// Helper method to populate dropdown data for Create view
    /// </summary>
    private async Task PopulateCreateViewModelAsync(PrescriptionCreateViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var currentDoctor = await _context.Staff.FirstOrDefaultAsync(s => s.UserId == userId && s.Role == StaffRole.Doctor);
        
        model.CurrentUserIsDoctor = currentDoctor != null;
        model.AvailableDrugs = await _context.Drugs
            .Where(d => d.IsActive && d.QuantityInStock > 0)
            .OrderBy(d => d.Name)
            .ToListAsync();
        model.AvailableDoctors = await _context.Staff
            .Where(s => s.IsActive && s.Role == StaffRole.Doctor)
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ToListAsync();
        model.AvailablePatients = await _context.Patients
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync();
    }

    /// <summary>
    /// POST: Prescriptions/SendToPharmacy/5
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendToPharmacy(int id)
    {
        try
        {
            await _prescriptionService.SendToPharmacyAsync(id);
            TempData["SuccessMessage"] = "Prescription sent to pharmacy successfully.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending prescription {Id} to pharmacy", id);
            TempData["ErrorMessage"] = "An error occurred while sending to pharmacy.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// API: Get drug details for prescription form
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDrug(int id)
    {
        var drug = await _context.Drugs.FindAsync(id);
        if (drug == null)
        {
            return NotFound();
        }

        return Json(new
        {
            drug.DrugId,
            drug.Name,
            drug.Strength,
            drug.DosageForm,
            drug.UnitPrice,
            drug.QuantityInStock,
            drug.RequiresPrescription
        });
    }
}
