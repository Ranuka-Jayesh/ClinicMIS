using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicMIS.Models.Entities;

/// <summary>
/// Represents a patient registered in the clinic system
/// </summary>
[Table("Patients")]
public class Patient : BaseEntity
{
    [Key]
    public int PatientId { get; set; }

    /// <summary>
    /// Auto-generated unique clinic number (e.g., CLN-2024-00001)
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Display(Name = "Clinic Number")]
    public string ClinicNumber { get; set; } = string.Empty;

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
    [MaxLength(10)]
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
    public string Address { get; set; } = string.Empty;

    [MaxLength(50)]
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

    [Display(Name = "Registration Date")]
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

    // Computed property
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";

    [NotMapped]
    public int Age => DateTime.Today.Year - DateOfBirth.Year - 
        (DateTime.Today.DayOfYear < DateOfBirth.DayOfYear ? 1 : 0);

    // Navigation properties
    public virtual ICollection<Visit> Visits { get; set; } = new List<Visit>();
    public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    public virtual ICollection<Billing> Billings { get; set; } = new List<Billing>();
}
