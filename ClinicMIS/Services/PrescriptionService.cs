using Microsoft.EntityFrameworkCore;
using ClinicMIS.Data;
using ClinicMIS.Models.Entities;
using ClinicMIS.Models.ViewModels;

namespace ClinicMIS.Services;

public class PrescriptionService : IPrescriptionService
{
    private readonly ClinicDbContext _context;

    public PrescriptionService(ClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Prescription?> GetByIdAsync(int id)
    {
        return await _context.Prescriptions
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .Include(p => p.PrescriptionItems)
                .ThenInclude(pi => pi.Drug)
            .FirstOrDefaultAsync(p => p.PrescriptionId == id);
    }

    /// <summary>
    /// Create a new prescription with items
    /// </summary>
    public async Task<Prescription> CreateAsync(PrescriptionCreateViewModel model, int doctorId)
    {
        var prescription = new Prescription
        {
            PrescriptionNumber = await GeneratePrescriptionNumberAsync(),
            PatientId = model.PatientId,
            DoctorId = doctorId,
            VisitId = model.VisitId,
            PrescriptionDate = DateTime.Today,
            Status = PrescriptionStatus.Draft,
            Diagnosis = model.Diagnosis,
            SpecialInstructions = model.SpecialInstructions
        };

        foreach (var item in model.Items)
        {
            var drug = await _context.Drugs.FindAsync(item.DrugId);
            if (drug == null)
                throw new KeyNotFoundException($"Drug with ID {item.DrugId} not found");

            prescription.PrescriptionItems.Add(new PrescriptionItem
            {
                DrugId = item.DrugId,
                Quantity = item.Quantity,
                DosageInstructions = item.DosageInstructions,
                DurationDays = item.DurationDays,
                UnitPrice = drug.UnitPrice, // Capture current price
                Notes = item.Notes
            });
        }

        _context.Prescriptions.Add(prescription);
        await _context.SaveChangesAsync();

        return prescription;
    }

    /// <summary>
    /// Send prescription to pharmacy for processing
    /// </summary>
    public async Task SendToPharmacyAsync(int prescriptionId)
    {
        var prescription = await _context.Prescriptions.FindAsync(prescriptionId);
        if (prescription == null)
            throw new KeyNotFoundException($"Prescription with ID {prescriptionId} not found");

        if (prescription.Status != PrescriptionStatus.Draft)
            throw new InvalidOperationException("Only draft prescriptions can be sent to pharmacy");

        prescription.Status = PrescriptionStatus.SentToPharmacy;
        prescription.SentToPharmacyAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Generate billing for a prescription
    /// </summary>
    public async Task<Billing> GenerateBillingAsync(int prescriptionId, decimal consultationFee = 0)
    {
        var prescription = await _context.Prescriptions
            .Include(p => p.PrescriptionItems)
            .Include(p => p.Patient)
            .Include(p => p.Billing)
            .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId);

        if (prescription == null)
            throw new KeyNotFoundException($"Prescription with ID {prescriptionId} not found");

        // Check if billing already exists for this prescription
        if (prescription.Billing != null)
        {
            // Return existing billing instead of creating a new one
            return prescription.Billing;
        }

        // Double-check if billing exists (in case of race condition)
        var existingBilling = await _context.Billings
            .FirstOrDefaultAsync(b => b.PrescriptionId == prescriptionId);
        
        if (existingBilling != null)
        {
            return existingBilling;
        }

        // Validate patient exists
        if (prescription.PatientId <= 0)
            throw new InvalidOperationException("Prescription must have a valid patient.");

        // Calculate medication cost from dispensed items
        var medicationCost = prescription.PrescriptionItems
            .Sum(pi => (pi.QuantityDispensed ?? pi.Quantity) * pi.UnitPrice);

        var billing = new Billing
        {
            InvoiceNumber = await GenerateInvoiceNumberAsync(),
            PatientId = prescription.PatientId,
            PrescriptionId = prescriptionId,
            BillingDate = DateTime.Today,
            ConsultationFee = consultationFee,
            MedicationCost = medicationCost,
            TotalAmount = consultationFee + medicationCost,
            PaymentStatus = PaymentStatus.Pending
        };

        _context.Billings.Add(billing);
        await _context.SaveChangesAsync();

        return billing;
    }

    public async Task<string> GeneratePrescriptionNumberAsync()
    {
        var today = DateTime.Today;
        var prefix = $"RX-{today:yyyyMMdd}-";

        var lastNumber = await _context.Prescriptions
            .IgnoreQueryFilters()
            .Where(p => p.PrescriptionNumber.StartsWith(prefix))
            .OrderByDescending(p => p.PrescriptionNumber)
            .Select(p => p.PrescriptionNumber)
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
