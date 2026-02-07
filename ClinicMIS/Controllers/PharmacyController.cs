using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ClinicMIS.Data;
using ClinicMIS.Models.Entities;
using ClinicMIS.Models.ViewModels;
using ClinicMIS.Services;
using System.Security.Claims;
using Microsoft.Data.SqlClient;

namespace ClinicMIS.Controllers;

/// <summary>
/// Pharmacy management controller
/// Handles prescription dispensing and drug inventory
/// </summary>
[Authorize(Policy = "CanDispense")]
public class PharmacyController : Controller
{
    private readonly ClinicDbContext _context;
    private readonly IPharmacyService _pharmacyService;
    private readonly IPrescriptionService _prescriptionService;
    private readonly ILogger<PharmacyController> _logger;

    public PharmacyController(
        ClinicDbContext context,
        IPharmacyService pharmacyService,
        IPrescriptionService prescriptionService,
        ILogger<PharmacyController> logger)
    {
        _context = context;
        _pharmacyService = pharmacyService;
        _prescriptionService = prescriptionService;
        _logger = logger;
    }

    /// <summary>
    /// GET: Pharmacy - Prescription queue
    /// </summary>
    public async Task<IActionResult> Index(string? searchTerm)
    {
        var queue = await _pharmacyService.GetPharmacyQueueAsync(searchTerm);
        return View(queue);
    }

    /// <summary>
    /// GET: Pharmacy/Dispense/5
    /// </summary>
    public async Task<IActionResult> Dispense(int id)
    {
        try
        {
            var model = await _pharmacyService.GetDispenseViewModelAsync(id);
            
            // Mark as processing if just received
            if (model.Prescription.Status == PrescriptionStatus.SentToPharmacy)
            {
                model.Prescription.Status = PrescriptionStatus.Processing;
                await _context.SaveChangesAsync();
            }
            
            return View(model);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// POST: Pharmacy/Dispense/5
    /// Process and dispense the prescription
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Dispense(int id, List<DispenseItemViewModel> items)
    {
        try
        {
            // Validate items
            if (items == null || items.Count == 0)
            {
                TempData["ErrorMessage"] = "No items to dispense. Please select at least one item.";
                return RedirectToAction(nameof(Dispense), new { id });
            }

            // Get pharmacist ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var pharmacist = await _context.Staff.FirstOrDefaultAsync(s => s.UserId == userId);
            
            if (pharmacist == null)
            {
                TempData["ErrorMessage"] = "Unable to identify pharmacist. Your user account is not linked to a staff record. Please contact an administrator to link your account.";
                return RedirectToAction(nameof(Index));
            }

            // Use execution strategy to support retry with transaction
            var strategy = _context.Database.CreateExecutionStrategy();
            var billing = await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await _pharmacyService.DispensePrescriptionAsync(id, items, pharmacist.StaffId);

                    // Generate billing
                    var result = await _prescriptionService.GenerateBillingAsync(id);
                    
                    await transaction.CommitAsync();
                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
            
            TempData["SuccessMessage"] = $"Prescription dispensed successfully. Invoice: {billing.InvoiceNumber}";
            return RedirectToAction("Details", "Billings", new { id = billing.BillingId });
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Dispense), new { id });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, "Resource not found while dispensing prescription {Id}", id);
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Dispense), new { id });
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Database error while dispensing prescription {Id}", id);
            
            // Extract inner exception details
            var errorMessage = "An error occurred while saving the entity changes.";
            if (dbEx.InnerException != null)
            {
                errorMessage = dbEx.InnerException.Message;
                
                // Handle SQL Server specific errors
                if (dbEx.InnerException is SqlException sqlEx)
                {
                    switch (sqlEx.Number)
                    {
                        case 547: // Foreign key constraint violation
                            errorMessage = "Cannot complete the operation due to a data integrity constraint. Please ensure all related records are valid.";
                            break;
                        case 2627: // Unique constraint violation
                        case 2601:
                            errorMessage = "A duplicate record already exists. Please try again.";
                            break;
                        case 515: // Cannot insert NULL value
                            errorMessage = "Required information is missing. Please check all fields are filled.";
                            break;
                        default:
                            errorMessage = $"Database error: {sqlEx.Message}";
                            break;
                    }
                }
            }
            
            TempData["ErrorMessage"] = $"An error occurred while dispensing: {errorMessage}";
            return RedirectToAction(nameof(Dispense), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispensing prescription {Id}", id);
            TempData["ErrorMessage"] = $"An error occurred while dispensing: {ex.Message}";
            return RedirectToAction(nameof(Dispense), new { id });
        }
    }

    /// <summary>
    /// GET: Pharmacy/Inventory - Drug inventory list
    /// </summary>
    public async Task<IActionResult> Inventory(DrugListViewModel filter)
    {
        var query = _context.Drugs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(d =>
                d.DrugCode.ToLower().Contains(term) ||
                d.Name.ToLower().Contains(term) ||
                (d.GenericName != null && d.GenericName.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Category))
        {
            query = query.Where(d => d.Category == filter.Category);
        }

        if (filter.LowStockOnly == true)
        {
            query = query.Where(d => d.QuantityInStock <= d.ReorderLevel);
        }

        if (filter.ExpiringSoonOnly == true)
        {
            var thirtyDays = DateTime.Today.AddDays(30);
            query = query.Where(d => d.ExpiryDate != null && d.ExpiryDate <= thirtyDays);
        }

        filter.TotalCount = await query.CountAsync();

        // Sorting
        query = filter.SortBy?.ToLower() switch
        {
            "name" => filter.SortDescending 
                ? query.OrderByDescending(d => d.Name) 
                : query.OrderBy(d => d.Name),
            "stock" => filter.SortDescending 
                ? query.OrderByDescending(d => d.QuantityInStock) 
                : query.OrderBy(d => d.QuantityInStock),
            "expiry" => filter.SortDescending 
                ? query.OrderByDescending(d => d.ExpiryDate) 
                : query.OrderBy(d => d.ExpiryDate),
            _ => query.OrderBy(d => d.Name)
        };

        filter.Drugs = await query
            .Skip((filter.CurrentPage - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(d => new DrugListItem
            {
                DrugId = d.DrugId,
                DrugCode = d.DrugCode,
                Name = d.Name,
                Category = d.Category,
                DosageForm = d.DosageForm,
                Strength = d.Strength,
                UnitPrice = d.UnitPrice,
                QuantityInStock = d.QuantityInStock,
                ReorderLevel = d.ReorderLevel,
                ExpiryDate = d.ExpiryDate
            })
            .ToListAsync();

        // Get available categories for filter dropdown
        filter.AvailableCategories = await _context.Drugs
            .Select(d => d.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        return View(filter);
    }

    /// <summary>
    /// GET: Pharmacy/LowStock - Low stock alerts
    /// </summary>
    public async Task<IActionResult> LowStock()
    {
        var drugs = await _pharmacyService.GetLowStockDrugsAsync();
        return View(drugs);
    }

    /// <summary>
    /// GET: Pharmacy/CreateDrug
    /// </summary>
    public IActionResult CreateDrug()
    {
        return View(new DrugViewModel());
    }

    /// <summary>
    /// POST: Pharmacy/CreateDrug
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDrug(DrugViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Check for duplicate drug code
        if (await _context.Drugs.AnyAsync(d => d.DrugCode == model.DrugCode))
        {
            ModelState.AddModelError("DrugCode", "This drug code already exists.");
            return View(model);
        }

        var drug = new Drug
        {
            DrugCode = model.DrugCode,
            Name = model.Name,
            GenericName = model.GenericName,
            Manufacturer = model.Manufacturer,
            Category = model.Category,
            DosageForm = model.DosageForm,
            Strength = model.Strength,
            UnitPrice = model.UnitPrice,
            QuantityInStock = model.QuantityInStock,
            ReorderLevel = model.ReorderLevel,
            ExpiryDate = model.ExpiryDate,
            StorageInstructions = model.StorageInstructions,
            RequiresPrescription = model.RequiresPrescription,
            IsActive = true
        };

        _context.Drugs.Add(drug);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Drug '{drug.Name}' added successfully.";
        return RedirectToAction(nameof(Inventory));
    }

    /// <summary>
    /// GET: Pharmacy/EditDrug/5
    /// </summary>
    public async Task<IActionResult> EditDrug(int id)
    {
        var drug = await _context.Drugs.FindAsync(id);
        if (drug == null)
        {
            return NotFound();
        }

        var model = new DrugViewModel
        {
            DrugId = drug.DrugId,
            DrugCode = drug.DrugCode,
            Name = drug.Name,
            GenericName = drug.GenericName,
            Manufacturer = drug.Manufacturer,
            Category = drug.Category,
            DosageForm = drug.DosageForm,
            Strength = drug.Strength,
            UnitPrice = drug.UnitPrice,
            QuantityInStock = drug.QuantityInStock,
            ReorderLevel = drug.ReorderLevel,
            ExpiryDate = drug.ExpiryDate,
            StorageInstructions = drug.StorageInstructions,
            RequiresPrescription = drug.RequiresPrescription
        };

        return View(model);
    }

    /// <summary>
    /// POST: Pharmacy/EditDrug/5
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDrug(int id, DrugViewModel model)
    {
        if (id != model.DrugId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var drug = await _context.Drugs.FindAsync(id);
        if (drug == null)
        {
            return NotFound();
        }

        drug.Name = model.Name;
        drug.GenericName = model.GenericName;
        drug.Manufacturer = model.Manufacturer;
        drug.Category = model.Category;
        drug.DosageForm = model.DosageForm;
        drug.Strength = model.Strength;
        drug.UnitPrice = model.UnitPrice;
        drug.ReorderLevel = model.ReorderLevel;
        drug.ExpiryDate = model.ExpiryDate;
        drug.StorageInstructions = model.StorageInstructions;
        drug.RequiresPrescription = model.RequiresPrescription;

        try
        {
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Drug updated successfully.";
            return RedirectToAction(nameof(Inventory));
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError("", "The record was modified by another user.");
            return View(model);
        }
    }

    /// <summary>
    /// POST: Pharmacy/AdjustStock
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustStock(StockAdjustmentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Invalid stock adjustment data.";
            return RedirectToAction(nameof(Inventory));
        }

        try
        {
            var adjustment = model.IsAddition ? model.AdjustmentQuantity : -model.AdjustmentQuantity;
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var pharmacist = await _context.Staff.FirstOrDefaultAsync(s => s.UserId == userId);

            await _pharmacyService.UpdateStockAsync(model.DrugId, adjustment, model.Reason, pharmacist?.StaffId);
            
            TempData["SuccessMessage"] = "Stock adjusted successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjusting stock for drug {DrugId}", model.DrugId);
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Inventory));
    }
}
