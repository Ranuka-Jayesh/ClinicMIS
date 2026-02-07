using ClinicMIS.Models.Entities;
using ClinicMIS.Models.ViewModels;

namespace ClinicMIS.Services;

public interface IPrescriptionService
{
    Task<Prescription?> GetByIdAsync(int id);
    Task<Prescription> CreateAsync(PrescriptionCreateViewModel model, int doctorId);
    Task SendToPharmacyAsync(int prescriptionId);
    Task<Billing> GenerateBillingAsync(int prescriptionId, decimal consultationFee = 0);
    Task<string> GeneratePrescriptionNumberAsync();
}
