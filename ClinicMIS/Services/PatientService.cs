using Microsoft.EntityFrameworkCore;
using ClinicMIS.Data;
using ClinicMIS.Models.Entities;
using ClinicMIS.Models.ViewModels;

namespace ClinicMIS.Services;

public class PatientService : IPatientService
{
    private readonly ClinicDbContext _context;

    public PatientService(ClinicDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get paginated list of patients with search and sort
    /// </summary>
    public async Task<PatientListViewModel> GetPatientsAsync(
        string? searchTerm, string? sortBy, bool sortDesc, int page, int pageSize)
    {
        var query = _context.Patients.AsQueryable();

        // Search filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(p =>
                p.ClinicNumber.ToLower().Contains(searchTerm) ||
                p.FirstName.ToLower().Contains(searchTerm) ||
                p.LastName.ToLower().Contains(searchTerm) ||
                p.PhoneNumber.Contains(searchTerm) ||
                (p.Email != null && p.Email.ToLower().Contains(searchTerm)));
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Sorting
        query = sortBy?.ToLower() switch
        {
            "name" => sortDesc 
                ? query.OrderByDescending(p => p.LastName).ThenByDescending(p => p.FirstName)
                : query.OrderBy(p => p.LastName).ThenBy(p => p.FirstName),
            "clinicnumber" => sortDesc 
                ? query.OrderByDescending(p => p.ClinicNumber)
                : query.OrderBy(p => p.ClinicNumber),
            "registrationdate" => sortDesc 
                ? query.OrderByDescending(p => p.RegistrationDate)
                : query.OrderBy(p => p.RegistrationDate),
            "dateofbirth" => sortDesc 
                ? query.OrderByDescending(p => p.DateOfBirth)
                : query.OrderBy(p => p.DateOfBirth),
            _ => query.OrderByDescending(p => p.RegistrationDate) // Default: newest first
        };

        // Pagination
        var patients = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PatientListItemViewModel
            {
                PatientId = p.PatientId,
                ClinicNumber = p.ClinicNumber,
                FullName = p.FirstName + " " + p.LastName,
                Age = DateTime.Today.Year - p.DateOfBirth.Year,
                Gender = p.Gender,
                PhoneNumber = p.PhoneNumber,
                RegistrationDate = p.RegistrationDate,
                TotalVisits = p.Visits.Count
            })
            .ToListAsync();

        return new PatientListViewModel
        {
            Patients = patients,
            SearchTerm = searchTerm,
            SortBy = sortBy,
            SortDescending = sortDesc,
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<Patient?> GetByIdAsync(int id)
    {
        return await _context.Patients
            .Include(p => p.Visits)
            .FirstOrDefaultAsync(p => p.PatientId == id);
    }

    public async Task<Patient?> GetByClinicNumberAsync(string clinicNumber)
    {
        return await _context.Patients
            .FirstOrDefaultAsync(p => p.ClinicNumber == clinicNumber);
    }

    public async Task<PatientDetailsViewModel> GetPatientDetailsAsync(int id)
    {
        var patient = await _context.Patients
            .Include(p => p.Visits.OrderByDescending(v => v.VisitDate).Take(10))
                .ThenInclude(v => v.Clinic)
            .Include(p => p.Visits)
                .ThenInclude(v => v.Doctor)
            .Include(p => p.Prescriptions.OrderByDescending(pr => pr.PrescriptionDate).Take(10))
                .ThenInclude(pr => pr.Doctor)
            .Include(p => p.Billings.OrderByDescending(b => b.BillingDate).Take(10))
            .FirstOrDefaultAsync(p => p.PatientId == id);

        if (patient == null)
            throw new KeyNotFoundException($"Patient with ID {id} not found");

        var billingStats = await _context.Billings
            .Where(b => b.PatientId == id)
            .GroupBy(b => b.PatientId)
            .Select(g => new
            {
                TotalBilled = g.Sum(b => b.TotalAmount),
                TotalPaid = g.Sum(b => b.AmountPaid)
            })
            .FirstOrDefaultAsync();

        return new PatientDetailsViewModel
        {
            Patient = patient,
            RecentVisits = patient.Visits,
            RecentPrescriptions = patient.Prescriptions,
            RecentBillings = patient.Billings,
            TotalBilled = billingStats?.TotalBilled ?? 0,
            TotalPaid = billingStats?.TotalPaid ?? 0,
            OutstandingBalance = (billingStats?.TotalBilled ?? 0) - (billingStats?.TotalPaid ?? 0)
        };
    }

    public async Task<Patient> CreateAsync(PatientCreateViewModel model, string createdBy)
    {
        var patient = new Patient
        {
            ClinicNumber = await GenerateClinicNumberAsync(),
            FirstName = model.FirstName,
            LastName = model.LastName,
            DateOfBirth = model.DateOfBirth,
            Gender = model.Gender,
            NationalId = model.NationalId,
            PhoneNumber = model.PhoneNumber,
            Email = model.Email,
            Address = model.Address,
            City = model.City,
            EmergencyContactName = model.EmergencyContactName,
            EmergencyContactPhone = model.EmergencyContactPhone,
            BloodType = model.BloodType,
            Allergies = model.Allergies,
            RegistrationDate = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        return patient;
    }

    public async Task<Patient> UpdateAsync(int id, PatientCreateViewModel model, string updatedBy)
    {
        var patient = await _context.Patients.FindAsync(id);
        if (patient == null)
            throw new KeyNotFoundException($"Patient with ID {id} not found");

        patient.FirstName = model.FirstName;
        patient.LastName = model.LastName;
        patient.DateOfBirth = model.DateOfBirth;
        patient.Gender = model.Gender;
        patient.NationalId = model.NationalId;
        patient.PhoneNumber = model.PhoneNumber;
        patient.Email = model.Email;
        patient.Address = model.Address;
        patient.City = model.City;
        patient.EmergencyContactName = model.EmergencyContactName;
        patient.EmergencyContactPhone = model.EmergencyContactPhone;
        patient.BloodType = model.BloodType;
        patient.Allergies = model.Allergies;
        patient.UpdatedBy = updatedBy;
        patient.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return patient;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var patient = await _context.Patients.FindAsync(id);
        if (patient == null)
            return false;

        // Soft delete (handled in DbContext)
        _context.Patients.Remove(patient);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Generate unique clinic number: CLN-YYYY-NNNNN
    /// </summary>
    public async Task<string> GenerateClinicNumberAsync()
    {
        var year = DateTime.Now.Year;
        var prefix = $"CLN-{year}-";

        // Find highest number for current year
        var lastNumber = await _context.Patients
            .IgnoreQueryFilters() // Include soft-deleted to avoid reusing numbers
            .Where(p => p.ClinicNumber.StartsWith(prefix))
            .OrderByDescending(p => p.ClinicNumber)
            .Select(p => p.ClinicNumber)
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

        return $"{prefix}{nextNumber:D5}";
    }
}
