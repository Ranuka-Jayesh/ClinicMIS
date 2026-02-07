using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ClinicMIS.Data;
using ClinicMIS.Models.Entities;

namespace ClinicMIS.Controllers;

/// <summary>
/// Clinic/Department management controller
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class ClinicsController : Controller
{
    private readonly ClinicDbContext _context;
    private readonly ILogger<ClinicsController> _logger;

    public ClinicsController(ClinicDbContext context, ILogger<ClinicsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// GET: Clinics
    /// </summary>
    [Authorize]
    public async Task<IActionResult> Index()
    {
        var clinics = await _context.Clinics
            .Include(c => c.Staff.Where(s => s.IsActive))
            .OrderBy(c => c.Name)
            .ToListAsync();

        return View(clinics);
    }

    /// <summary>
    /// GET: Clinics/Details/5
    /// </summary>
    [Authorize]
    public async Task<IActionResult> Details(int id)
    {
        var clinic = await _context.Clinics
            .Include(c => c.Staff.Where(s => s.IsActive))
            .FirstOrDefaultAsync(c => c.ClinicId == id);

        if (clinic == null)
        {
            return NotFound();
        }

        // Get today's visit count
        ViewBag.TodayVisits = await _context.Visits
            .CountAsync(v => v.ClinicId == id && v.VisitDate.Date == DateTime.Today);

        return View(clinic);
    }

    /// <summary>
    /// GET: Clinics/Create
    /// </summary>
    public IActionResult Create()
    {
        return View(new Clinic());
    }

    /// <summary>
    /// POST: Clinics/Create
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Clinic clinic)
    {
        if (!ModelState.IsValid)
        {
            return View(clinic);
        }

        // Check duplicate name
        if (await _context.Clinics.AnyAsync(c => c.Name == clinic.Name))
        {
            ModelState.AddModelError("Name", "A clinic with this name already exists.");
            return View(clinic);
        }

        clinic.IsActive = true;
        _context.Clinics.Add(clinic);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Clinic '{clinic.Name}' created successfully.";
        return RedirectToAction(nameof(Details), new { id = clinic.ClinicId });
    }

    /// <summary>
    /// GET: Clinics/Edit/5
    /// </summary>
    public async Task<IActionResult> Edit(int id)
    {
        var clinic = await _context.Clinics.FindAsync(id);
        if (clinic == null)
        {
            return NotFound();
        }

        return View(clinic);
    }

    /// <summary>
    /// POST: Clinics/Edit/5
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Clinic clinic)
    {
        if (id != clinic.ClinicId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(clinic);
        }

        var existing = await _context.Clinics.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        existing.Name = clinic.Name;
        existing.Description = clinic.Description;
        existing.Location = clinic.Location;
        existing.ContactPhone = clinic.ContactPhone;
        existing.IsActive = clinic.IsActive;

        try
        {
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Clinic updated successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError("", "The record was modified by another user.");
            return View(clinic);
        }
    }

    /// <summary>
    /// POST: Clinics/ToggleActive/5
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var clinic = await _context.Clinics.FindAsync(id);
        if (clinic == null)
        {
            return NotFound();
        }

        clinic.IsActive = !clinic.IsActive;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Clinic {(clinic.IsActive ? "activated" : "deactivated")}.";
        return RedirectToAction(nameof(Index));
    }
}
