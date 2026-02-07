using ClinicMIS.Models.Entities;
using ClinicMIS.Models.ViewModels;

namespace ClinicMIS.Services;

/// <summary>
/// Service interface for pharmacy operations
/// </summary>
public interface IPharmacyService
{
    Task<PharmacyQueueViewModel> GetPharmacyQueueAsync(string? searchTerm);
    Task<DispenseViewModel> GetDispenseViewModelAsync(int prescriptionId);
    Task DispensePrescriptionAsync(int prescriptionId, List<DispenseItemViewModel> items, int pharmacistId);
    Task UpdateStockAsync(int drugId, int quantityChange, string reason, int? pharmacistId);
    Task<IEnumerable<Drug>> GetLowStockDrugsAsync();
}
