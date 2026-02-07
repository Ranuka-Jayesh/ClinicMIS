using ClinicMIS.Models.Entities;
using ClinicMIS.Models.ViewModels;

namespace ClinicMIS.Services;

public interface IPatientService
{
    Task<PatientListViewModel> GetPatientsAsync(string? searchTerm, string? sortBy, bool sortDesc, int page, int pageSize);
    Task<Patient?> GetByIdAsync(int id);
    Task<Patient?> GetByClinicNumberAsync(string clinicNumber);
    Task<PatientDetailsViewModel> GetPatientDetailsAsync(int id);
    Task<Patient> CreateAsync(PatientCreateViewModel model, string createdBy);
    Task<Patient> UpdateAsync(int id, PatientCreateViewModel model, string updatedBy);
    Task<bool> DeleteAsync(int id);
    Task<string> GenerateClinicNumberAsync();
}
