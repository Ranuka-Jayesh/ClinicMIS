using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicMIS.Models.Entities;

/// <summary>
/// Represents clinic staff members (Doctor, Nurse, Pharmacist, Admin)
/// </summary>
[Table("Staff")]
public class Staff : BaseEntity
{
    [Key]
    public int StaffId { get; set; }

    /// <summary>
    /// Staff employee number (auto-generated)
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Display(Name = "Employee Number")]
    public string EmployeeNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "First name is required")]
    [MaxLength(50)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [MaxLength(50)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required")]
    [Display(Name = "Role")]
    public StaffRole Role { get; set; }

    /// <summary>
    /// Specialization for doctors (e.g., Cardiologist, Oncologist)
    /// </summary>
    [MaxLength(100)]
    public string? Specialization { get; set; }

    /// <summary>
    /// Medical license number for doctors/nurses
    /// </summary>
    [MaxLength(50)]
    [Display(Name = "License Number")]
    public string? LicenseNumber { get; set; }

    [Required(ErrorMessage = "Phone number is required")]
    [Phone]
    [MaxLength(20)]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Hire Date")]
    public DateTime HireDate { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Foreign key to the clinic/department this staff belongs to
    /// </summary>
    [Display(Name = "Assigned Clinic")]
    public int? ClinicId { get; set; }

    /// <summary>
    /// Link to ASP.NET Identity user for authentication
    /// </summary>
    [MaxLength(450)]
    public string? UserId { get; set; }

    // Computed property
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";

    [NotMapped]
    public string DisplayTitle => Role == StaffRole.Doctor ? $"Dr. {FullName}" : FullName;

    // Navigation properties
    [ForeignKey("ClinicId")]
    public virtual Clinic? Clinic { get; set; }

    [ForeignKey("UserId")]
    public virtual ApplicationUser? User { get; set; }

    // Staff as doctor
    public virtual ICollection<Visit> DoctorVisits { get; set; } = new List<Visit>();
    public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();

    // Staff as pharmacist
    public virtual ICollection<Dispensing> Dispensings { get; set; } = new List<Dispensing>();
}
