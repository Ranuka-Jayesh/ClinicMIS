using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ClinicMIS.Data;
using ClinicMIS.Models.Entities;
using ClinicMIS.Models.ViewModels;

namespace ClinicMIS.Controllers;

/// <summary>
/// Billing and payment management controller
/// </summary>
[Authorize]
[Authorize(Policy = "CanViewBillings")]
public class BillingsController : Controller
{
    private readonly ClinicDbContext _context;
    private readonly ILogger<BillingsController> _logger;

    public BillingsController(ClinicDbContext context, ILogger<BillingsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// GET: Billings - List with filtering
    /// </summary>
    public async Task<IActionResult> Index(BillingListViewModel filter)
    {
        var query = _context.Billings
            .Include(b => b.Patient)
            .Include(b => b.Prescription)
            .AsQueryable();

        // Apply filters
        if (filter.FromDate.HasValue)
            query = query.Where(b => b.BillingDate >= filter.FromDate.Value);
        
        if (filter.ToDate.HasValue)
            query = query.Where(b => b.BillingDate <= filter.ToDate.Value);
        
        if (filter.Status.HasValue)
            query = query.Where(b => b.PaymentStatus == filter.Status.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(b =>
                b.InvoiceNumber.ToLower().Contains(term) ||
                b.Patient.ClinicNumber.ToLower().Contains(term) ||
                b.Patient.FirstName.ToLower().Contains(term) ||
                b.Patient.LastName.ToLower().Contains(term));
        }

        // Calculate summary
        filter.TotalBilled = await query.SumAsync(b => b.TotalAmount);
        filter.TotalPaid = await query.SumAsync(b => b.AmountPaid);
        filter.TotalOutstanding = filter.TotalBilled - filter.TotalPaid;
        filter.TotalCount = await query.CountAsync();

        // Sorting
        query = filter.SortBy?.ToLower() switch
        {
            "patient" => filter.SortDescending
                ? query.OrderByDescending(b => b.Patient.LastName)
                : query.OrderBy(b => b.Patient.LastName),
            "amount" => filter.SortDescending
                ? query.OrderByDescending(b => b.TotalAmount)
                : query.OrderBy(b => b.TotalAmount),
            "status" => filter.SortDescending
                ? query.OrderByDescending(b => b.PaymentStatus)
                : query.OrderBy(b => b.PaymentStatus),
            _ => filter.SortDescending
                ? query.OrderByDescending(b => b.BillingDate)
                : query.OrderBy(b => b.BillingDate)
        };

        // Pagination
        filter.Billings = await query
            .Skip((filter.CurrentPage - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(b => new BillingListItem
            {
                BillingId = b.BillingId,
                InvoiceNumber = b.InvoiceNumber,
                PatientName = b.Patient.FirstName + " " + b.Patient.LastName,
                PatientClinicNumber = b.Patient.ClinicNumber,
                BillingDate = b.BillingDate,
                TotalAmount = b.TotalAmount,
                AmountPaid = b.AmountPaid,
                BalanceDue = b.TotalAmount - b.AmountPaid,
                PaymentStatus = b.PaymentStatus,
                PaymentMethod = b.PaymentMethod,
                HasPrescription = b.PrescriptionId != null
            })
            .ToListAsync();

        return View(filter);
    }

    /// <summary>
    /// GET: Billings/Details/5
    /// </summary>
    public async Task<IActionResult> Details(int id)
    {
        var billing = await _context.Billings
            .Include(b => b.Patient)
            .Include(b => b.Prescription)
                .ThenInclude(p => p!.PrescriptionItems)
                    .ThenInclude(pi => pi.Drug)
            .Include(b => b.Prescription)
                .ThenInclude(p => p!.Doctor)
            .FirstOrDefaultAsync(b => b.BillingId == id);

        if (billing == null)
        {
            return NotFound();
        }

        return View(billing);
    }

    /// <summary>
    /// GET: Billings/Invoice/5 - Printable invoice
    /// </summary>
    public async Task<IActionResult> Invoice(int id)
    {
        var billing = await _context.Billings
            .Include(b => b.Patient)
            .Include(b => b.Prescription)
                .ThenInclude(p => p!.PrescriptionItems)
                    .ThenInclude(pi => pi.Drug)
            .FirstOrDefaultAsync(b => b.BillingId == id);

        if (billing == null)
        {
            return NotFound();
        }

        var model = new InvoiceViewModel
        {
            Billing = billing,
            Patient = billing.Patient,
            Prescription = billing.Prescription,
            PrescriptionItems = billing.Prescription?.PrescriptionItems
        };

        return View(model);
    }

    /// <summary>
    /// GET: Billings/Payment/5
    /// </summary>
    public async Task<IActionResult> Payment(int id)
    {
        var billing = await _context.Billings
            .Include(b => b.Patient)
            .FirstOrDefaultAsync(b => b.BillingId == id);

        if (billing == null)
        {
            return NotFound();
        }

        if (billing.PaymentStatus == PaymentStatus.Paid)
        {
            TempData["ErrorMessage"] = "This invoice is already fully paid.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = new PaymentViewModel
        {
            BillingId = billing.BillingId,
            InvoiceNumber = billing.InvoiceNumber,
            PatientName = billing.Patient.FullName,
            TotalAmount = billing.TotalAmount,
            AmountPaid = billing.AmountPaid,
            BalanceDue = billing.TotalAmount - billing.AmountPaid,
            PaymentAmount = billing.TotalAmount - billing.AmountPaid
        };

        return View(model);
    }

    /// <summary>
    /// POST: Billings/Payment/5
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Payment(int id, PaymentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var billing = await _context.Billings.FindAsync(id);
        if (billing == null)
        {
            return NotFound();
        }

        if (model.PaymentAmount > (billing.TotalAmount - billing.AmountPaid))
        {
            ModelState.AddModelError("PaymentAmount", "Payment amount exceeds balance due.");
            model.BalanceDue = billing.TotalAmount - billing.AmountPaid;
            return View(model);
        }

        billing.AmountPaid += model.PaymentAmount;
        billing.PaymentMethod = model.PaymentMethod;
        billing.PaymentDate = DateTime.Today;

        // Update payment status
        if (billing.AmountPaid >= billing.TotalAmount)
        {
            billing.PaymentStatus = PaymentStatus.Paid;
        }
        else if (billing.AmountPaid > 0)
        {
            billing.PaymentStatus = PaymentStatus.PartiallyPaid;
        }

        if (!string.IsNullOrWhiteSpace(model.Notes))
        {
            billing.Notes = string.IsNullOrWhiteSpace(billing.Notes)
                ? model.Notes
                : billing.Notes + "\n" + model.Notes;
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Payment recorded successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// GET: Billings/Create
    /// </summary>
    public async Task<IActionResult> Create(int? patientId)
    {
        var model = new BillingCreateViewModel();

        if (patientId.HasValue && patientId.Value > 0)
        {
            model.PatientId = patientId.Value;
            var patient = await _context.Patients.FindAsync(patientId.Value);
            if (patient != null)
            {
                model.Patient = patient;
            }
        }

        model.AvailablePatients = await _context.Patients
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync();

        return View(model);
    }

    /// <summary>
    /// POST: Billings/Create
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanEditBillings")]
    public async Task<IActionResult> Create(BillingCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailablePatients = await _context.Patients
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToListAsync();
            return View(model);
        }

        // Validate patient exists
        var patient = await _context.Patients.FindAsync(model.PatientId);
        if (patient == null)
        {
            ModelState.AddModelError("PatientId", "Selected patient not found.");
            model.AvailablePatients = await _context.Patients
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToListAsync();
            return View(model);
        }

        // Check if billing already exists for prescription (if provided)
        if (model.PrescriptionId.HasValue)
        {
            var existingBilling = await _context.Billings
                .FirstOrDefaultAsync(b => b.PrescriptionId == model.PrescriptionId.Value);
            
            if (existingBilling != null)
            {
                TempData["ErrorMessage"] = "A billing already exists for this prescription.";
                return RedirectToAction(nameof(Details), new { id = existingBilling.BillingId });
            }
        }

        // Generate invoice number
        var invoiceNumber = await GenerateInvoiceNumberAsync();

        // Calculate total
        var subtotal = model.ConsultationFee + model.MedicationCost + model.OtherCharges;
        var total = subtotal - model.Discount + model.Tax;

        var billing = new Billing
        {
            InvoiceNumber = invoiceNumber,
            PatientId = model.PatientId,
            PrescriptionId = model.PrescriptionId,
            VisitId = model.VisitId,
            BillingDate = DateTime.Today,
            ConsultationFee = model.ConsultationFee,
            MedicationCost = model.MedicationCost,
            OtherCharges = model.OtherCharges,
            Discount = model.Discount,
            Tax = model.Tax,
            TotalAmount = total,
            PaymentStatus = PaymentStatus.Pending,
            Notes = model.Notes
        };

        _context.Billings.Add(billing);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Invoice {billing.InvoiceNumber} created successfully.";
        return RedirectToAction(nameof(Details), new { id = billing.BillingId });
    }

    /// <summary>
    /// GET: Billings/Pending - Quick view of pending payments
    /// </summary>
    public async Task<IActionResult> Pending()
    {
        var pending = await _context.Billings
            .Include(b => b.Patient)
            .Where(b => b.PaymentStatus == PaymentStatus.Pending || 
                       b.PaymentStatus == PaymentStatus.PartiallyPaid)
            .OrderBy(b => b.BillingDate)
            .Select(b => new BillingListItem
            {
                BillingId = b.BillingId,
                InvoiceNumber = b.InvoiceNumber,
                PatientName = b.Patient.FirstName + " " + b.Patient.LastName,
                PatientClinicNumber = b.Patient.ClinicNumber,
                BillingDate = b.BillingDate,
                TotalAmount = b.TotalAmount,
                AmountPaid = b.AmountPaid,
                BalanceDue = b.TotalAmount - b.AmountPaid,
                PaymentStatus = b.PaymentStatus
            })
            .ToListAsync();

        return View(pending);
    }

    /// <summary>
    /// Generate a unique invoice number
    /// </summary>
    private async Task<string> GenerateInvoiceNumberAsync()
    {
        var today = DateTime.Today;
        var prefix = $"INV-{today:yyyyMMdd}-";

        var lastNumber = await _context.Billings
            .IgnoreQueryFilters()
            .Where(b => b.InvoiceNumber.StartsWith(prefix))
            .OrderByDescending(b => b.InvoiceNumber)
            .Select(b => b.InvoiceNumber)
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
