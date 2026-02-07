using System.ComponentModel.DataAnnotations;
using ClinicMIS.Models.Entities;

namespace ClinicMIS.Models.ViewModels;

/// <summary>
/// ViewModel for patient registration form
/// </summary>
public class PatientCreateViewModel
{
    [Required(ErrorMessage = "First name is required")]
    [MaxLength(50)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [MaxLength(50)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Date of birth is required")]
    [DataType(DataType.Date)]
    [Display(Name = "Date of Birth")]
    public DateTime DateOfBirth { get; set; }

    [Required(ErrorMessage = "Gender is required")]
    [Display(Name = "Gender")]
    public string Gender { get; set; } = string.Empty;

    [MaxLength(20)]
    [Display(Name = "National ID/Passport")]
    public string? NationalId { get; set; }

    [Required(ErrorMessage = "Phone number is required")]
    [Phone]
    [MaxLength(20)]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress]
    [MaxLength(100)]
    [Display(Name = "Email Address")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Address is required")]
    [MaxLength(200)]
    [Display(Name = "Address")]
    public string Address { get; set; } = string.Empty;

    [MaxLength(50)]
    [Display(Name = "City")]
    public string? City { get; set; }

    [MaxLength(100)]
    [Display(Name = "Emergency Contact Name")]
    public string? EmergencyContactName { get; set; }

    [Phone]
    [MaxLength(20)]
    [Display(Name = "Emergency Contact Phone")]
    public string? EmergencyContactPhone { get; set; }

    [MaxLength(10)]
    [Display(Name = "Blood Type")]
    public string? BloodType { get; set; }

    [MaxLength(500)]
    [Display(Name = "Known Allergies")]
    public string? Allergies { get; set; }
}

/// <summary>
/// ViewModel for patient list with search and pagination
/// </summary>
public class PatientListViewModel
{
    public IEnumerable<PatientListItemViewModel> Patients { get; set; } = new List<PatientListItemViewModel>();
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class PatientListItemViewModel
{
    public int PatientId { get; set; }
    public string ClinicNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime RegistrationDate { get; set; }
    public int TotalVisits { get; set; }
}

/// <summary>
/// ViewModel for patient details/profile
/// </summary>
public class PatientDetailsViewModel
{
    public Patient Patient { get; set; } = null!;
    public IEnumerable<Visit> RecentVisits { get; set; } = new List<Visit>();
    public IEnumerable<Prescription> RecentPrescriptions { get; set; } = new List<Prescription>();
    public IEnumerable<Billing> RecentBillings { get; set; } = new List<Billing>();
    public decimal TotalBilled { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal OutstandingBalance { get; set; }
}
