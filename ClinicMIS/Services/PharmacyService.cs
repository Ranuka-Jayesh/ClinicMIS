using Microsoft.EntityFrameworkCore;
using ClinicMIS.Data;
using ClinicMIS.Models.Entities;
using ClinicMIS.Models.ViewModels;

namespace ClinicMIS.Services;

/// <summary>
/// Service for pharmacy operations including dispensing and stock management
/// </summary>
public class PharmacyService : IPharmacyService
{
    private readonly ClinicDbContext _context;

    public PharmacyService(ClinicDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get pharmacy queue with prescriptions grouped by status
    /// </summary>
    public async Task<PharmacyQueueViewModel> GetPharmacyQueueAsync(string? searchTerm)
    {
        var query = _context.Prescriptions
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .Include(p => p.PrescriptionItems)
            .Where(p => p.Status != PrescriptionStatus.Draft && 
                       p.Status != PrescriptionStatus.Cancelled);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(p =>
                p.PrescriptionNumber.ToLower().Contains(searchTerm) ||
                p.Patient.ClinicNumber.ToLower().Contains(searchTerm) ||
                p.Patient.FirstName.ToLower().Contains(searchTerm) ||
                p.Patient.LastName.ToLower().Contains(searchTerm));
        }

        var prescriptions = await query
            .Select(p => new PrescriptionQueueItem
            {
                PrescriptionId = p.PrescriptionId,
                PrescriptionNumber = p.PrescriptionNumber,
                PatientName = p.Patient.FirstName + " " + p.Patient.LastName,
                PatientClinicNumber = p.Patient.ClinicNumber,
                DoctorName = "Dr. " + p.Doctor.FirstName + " " + p.Doctor.LastName,
                PrescriptionDate = p.PrescriptionDate,
                SentToPharmacyAt = p.SentToPharmacyAt,
                Status = p.Status,
                ItemCount = p.PrescriptionItems.Count,
                TotalAmount = p.PrescriptionItems.Sum(i => i.Quantity * i.UnitPrice)
            })
            .ToListAsync();

        return new PharmacyQueueViewModel
        {
            SearchTerm = searchTerm,
            PendingPrescriptions = prescriptions
                .Where(p => p.Status == PrescriptionStatus.SentToPharmacy)
                .OrderBy(p => p.SentToPharmacyAt),
            ProcessingPrescriptions = prescriptions
                .Where(p => p.Status == PrescriptionStatus.Processing)
                .OrderBy(p => p.SentToPharmacyAt),
            ReadyPrescriptions = prescriptions
                .Where(p => p.Status == PrescriptionStatus.ReadyForPickup)
                .OrderByDescending(p => p.SentToPharmacyAt)
        };
    }

    /// <summary>
    /// Get prescription details for dispensing
    /// </summary>
    public async Task<DispenseViewModel> GetDispenseViewModelAsync(int prescriptionId)
    {
        var prescription = await _context.Prescriptions
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .Include(p => p.PrescriptionItems)
                .ThenInclude(pi => pi.Drug)
            .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId);

        if (prescription == null)
            throw new KeyNotFoundException($"Prescription with ID {prescriptionId} not found");

        var items = prescription.PrescriptionItems.Select(pi => new DispenseItemViewModel
        {
            PrescriptionItemId = pi.PrescriptionItemId,
            DrugId = pi.DrugId,
            DrugName = pi.Drug.DisplayName,
            DosageInstructions = pi.DosageInstructions,
            QuantityPrescribed = pi.Quantity,
            QuantityToDispense = pi.Quantity,
            AvailableStock = pi.Drug.QuantityInStock,
            UnitPrice = pi.UnitPrice,
            Notes = pi.Notes
        }).ToList();

        return new DispenseViewModel
        {
            Prescription = prescription,
            Items = items
        };
    }

    /// <summary>
    /// Dispense a prescription and update stock
    /// </summary>
    public async Task DispensePrescriptionAsync(int prescriptionId, List<DispenseItemViewModel> items, int pharmacistId)
    {
        var prescription = await _context.Prescriptions
            .Include(p => p.PrescriptionItems)
            .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId);

        if (prescription == null)
            throw new KeyNotFoundException($"Prescription with ID {prescriptionId} not found");

        // Check if already dispensed
        if (prescription.Status == PrescriptionStatus.Dispensed)
            throw new InvalidOperationException("This prescription has already been dispensed.");

        // Validate items
        if (items == null || items.Count == 0)
            throw new InvalidOperationException("No items to dispense.");

        // Generate dispensing number
        var dispensingNumber = await GenerateDispensingNumberAsync();

        foreach (var item in items)
        {
            if (item.QuantityToDispense <= 0)
                continue;

            var drug = await _context.Drugs.FindAsync(item.DrugId);
            if (drug == null)
                throw new KeyNotFoundException($"Drug with ID {item.DrugId} not found");

            // Check stock availability
            if (drug.QuantityInStock < item.QuantityToDispense)
                throw new InvalidOperationException(
                    $"Insufficient stock for {drug.Name}. Available: {drug.QuantityInStock}, Requested: {item.QuantityToDispense}");

            // Record stock before dispensing
            var stockBefore = drug.QuantityInStock;

            // Deduct from stock
            drug.QuantityInStock -= item.QuantityToDispense;

            // Update prescription item with dispensed quantity
            var prescriptionItem = prescription.PrescriptionItems
                .FirstOrDefault(pi => pi.PrescriptionItemId == item.PrescriptionItemId);
            
            if (prescriptionItem != null)
            {
                prescriptionItem.QuantityDispensed = item.QuantityToDispense;
            }
            else if (item.PrescriptionItemId > 0)
            {
                // Validate that PrescriptionItemId exists if provided
                var itemExists = await _context.PrescriptionItems
                    .AnyAsync(pi => pi.PrescriptionItemId == item.PrescriptionItemId);
                if (!itemExists)
                {
                    throw new InvalidOperationException(
                        $"Prescription item with ID {item.PrescriptionItemId} not found.");
                }
            }

            // Create dispensing record
            var dispensing = new Dispensing
            {
                DispensingNumber = dispensingNumber,
                DrugId = item.DrugId,
                PharmacistId = pharmacistId,
                PrescriptionItemId = item.PrescriptionItemId > 0 ? item.PrescriptionItemId : null,
                QuantityDispensed = item.QuantityToDispense,
                UnitPrice = item.UnitPrice,
                DispensingDate = DateTime.UtcNow,
                StockBefore = stockBefore,
                StockAfter = drug.QuantityInStock,
                Notes = item.Notes
            };

            _context.Dispensings.Add(dispensing);
        }

        // Update prescription status
        prescription.Status = PrescriptionStatus.Dispensed;
        prescription.DispensedByStaffId = pharmacistId;
        prescription.DispensedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Update drug stock (for adjustments, returns, etc.)
    /// </summary>
    public async Task UpdateStockAsync(int drugId, int quantityChange, string reason, int? pharmacistId)
    {
        var drug = await _context.Drugs.FindAsync(drugId);
        if (drug == null)
            throw new KeyNotFoundException($"Drug with ID {drugId} not found");

        var newQuantity = drug.QuantityInStock + quantityChange;
        if (newQuantity < 0)
            throw new InvalidOperationException("Stock cannot be negative");

        drug.QuantityInStock = newQuantity;
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Get list of drugs with low stock for alerts
    /// Includes: low stock, out of stock, and expiring soon drugs
    /// </summary>
    public async Task<IEnumerable<Drug>> GetLowStockDrugsAsync()
    {
        var today = DateTime.Today;
        var thirtyDaysFromNow = today.AddDays(30);
        
        return await _context.Drugs
            .Where(d => d.IsActive && (
                d.QuantityInStock <= d.ReorderLevel || // Low stock or out of stock
                (d.ExpiryDate.HasValue && d.ExpiryDate.Value <= thirtyDaysFromNow) // Expiring soon or expired
            ))
            .OrderBy(d => d.QuantityInStock)
            .ThenBy(d => d.ExpiryDate ?? DateTime.MaxValue)
            .ToListAsync();
    }

    private async Task<string> GenerateDispensingNumberAsync()
    {
        var today = DateTime.Today;
        var prefix = $"DSP-{today:yyyyMMdd}-";

        var lastNumber = await _context.Dispensings
            .Where(d => d.DispensingNumber.StartsWith(prefix))
            .OrderByDescending(d => d.DispensingNumber)
            .Select(d => d.DispensingNumber)
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
