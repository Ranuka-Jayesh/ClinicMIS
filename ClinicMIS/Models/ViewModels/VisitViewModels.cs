using System.ComponentModel.DataAnnotations;
using ClinicMIS.Models.Entities;

namespace ClinicMIS.Models.ViewModels;

/// <summary>
/// ViewModel for creating a new visit
/// </summary>
public class VisitCreateViewModel
{
    [Required(ErrorMessage = "Patient is required")]
    [Display(Name = "Patient")]
    public int PatientId { get; set; }

    [Required(ErrorMessage = "Clinic is required")]
    [Display(Name = "Clinic")]
    public int ClinicId { get; set; }

    [Display(Name = "Attending Doctor")]
    public int? DoctorId { get; set; }

    [Required(ErrorMessage = "Visit date is required")]
    [DataType(DataType.Date)]
    [Display(Name = "Visit Date")]
    public DateTime VisitDate { get; set; } = DateTime.Today;

    [MaxLength(500)]
    [Display(Name = "Reason for Visit")]
    public string? ReasonForVisit { get; set; }

    // For dropdowns
    public IEnumerable<Patient>? AvailablePatients { get; set; }
    public IEnumerable<Clinic>? AvailableClinics { get; set; }
    public IEnumerable<Staff>? AvailableDoctors { get; set; }
}

/// <summary>
/// ViewModel for doctor consultation (updating visit details)
/// </summary>
public class ConsultationViewModel
{
    public int VisitId { get; set; }
    public Visit Visit { get; set; } = null!;
    public Patient Patient { get; set; } = null!;

    // Vital signs
    [MaxLength(10)]
    [Display(Name = "Blood Pressure")]
    public string? BloodPressure { get; set; }

    [Display(Name = "Temperature (°C)")]
    [Range(35, 42)]
    public decimal? Temperature { get; set; }

    [Display(Name = "Pulse Rate")]
    [Range(40, 200)]
    public int? PulseRate { get; set; }

    [Display(Name = "Weight (kg)")]
    [Range(0.5, 500)]
    public decimal? Weight { get; set; }

    [Display(Name = "Height (cm)")]
    [Range(30, 250)]
    public decimal? Height { get; set; }

    // Consultation details
    [MaxLength(1000)]
    [Display(Name = "Symptoms")]
    public string? Symptoms { get; set; }

    [MaxLength(1000)]
    [Display(Name = "Diagnosis")]
    public string? Diagnosis { get; set; }

    [MaxLength(2000)]
    [Display(Name = "Doctor Notes")]
    public string? DoctorNotes { get; set; }

    [Display(Name = "Follow-up Required")]
    public bool FollowUpRequired { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Follow-up Date")]
    public DateTime? FollowUpDate { get; set; }

    // Prescription creation during consultation
    public bool CreatePrescription { get; set; }
    public PrescriptionCreateViewModel? NewPrescription { get; set; }
}

/// <summary>
/// ViewModel for visit list with filtering
/// </summary>
public class VisitListViewModel
{
    public IEnumerable<VisitListItem> Visits { get; set; } = new List<VisitListItem>();
    
    // Filters
    [DataType(DataType.Date)]
    [Display(Name = "From Date")]
    public DateTime? FromDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "To Date")]
    public DateTime? ToDate { get; set; }

    [Display(Name = "Clinic")]
    public int? ClinicId { get; set; }

    [Display(Name = "Doctor")]
    public int? DoctorId { get; set; }

    [Display(Name = "Status")]
    public VisitStatus? Status { get; set; }

    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }

    // Pagination
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    // For dropdowns
    public IEnumerable<Clinic>? AvailableClinics { get; set; }
    public IEnumerable<Staff>? AvailableDoctors { get; set; }
}

public class VisitListItem
{
    public int VisitId { get; set; }
    public string VisitNumber { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string PatientClinicNumber { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public DateTime VisitDate { get; set; }
    public VisitStatus Status { get; set; }
    public bool HasPrescription { get; set; }
}
