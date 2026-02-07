using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicMIS.Models.Entities;

/// <summary>
/// Represents a patient visit to a clinic
/// </summary>
[Table("Visits")]
public class Visit : BaseEntity
{
    [Key]
    public int VisitId { get; set; }

    /// <summary>
    /// Visit reference number (auto-generated)
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Display(Name = "Visit Number")]
    public string VisitNumber { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Patient")]
    public int PatientId { get; set; }

    [Required]
    [Display(Name = "Clinic")]
    public int ClinicId { get; set; }

    [Display(Name = "Attending Doctor")]
    public int? DoctorId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Visit Date")]
    public DateTime VisitDate { get; set; }

    [DataType(DataType.Time)]
    [Display(Name = "Check-in Time")]
    public TimeSpan? CheckInTime { get; set; }

    [DataType(DataType.Time)]
    [Display(Name = "Check-out Time")]
    public TimeSpan? CheckOutTime { get; set; }

    [Required]
    [Display(Name = "Status")]
    public VisitStatus Status { get; set; } = VisitStatus.Scheduled;

    [MaxLength(500)]
    [Display(Name = "Reason for Visit")]
    public string? ReasonForVisit { get; set; }

    [MaxLength(1000)]
    [Display(Name = "Symptoms")]
    public string? Symptoms { get; set; }

    [MaxLength(1000)]
    [Display(Name = "Diagnosis")]
    public string? Diagnosis { get; set; }

    [MaxLength(2000)]
    [Display(Name = "Doctor Notes")]
    public string? DoctorNotes { get; set; }

    /// <summary>
    /// Vital signs recorded during visit
    /// </summary>
    [MaxLength(10)]
    [Display(Name = "Blood Pressure")]
    public string? BloodPressure { get; set; }

    [Display(Name = "Temperature (°C)")]
    [Column(TypeName = "decimal(4,1)")]
    public decimal? Temperature { get; set; }

    [Display(Name = "Pulse Rate")]
    public int? PulseRate { get; set; }

    [Display(Name = "Weight (kg)")]
    [Column(TypeName = "decimal(5,2)")]
    public decimal? Weight { get; set; }

    [Display(Name = "Height (cm)")]
    [Column(TypeName = "decimal(5,2)")]
    public decimal? Height { get; set; }

    [Display(Name = "Follow-up Required")]
    public bool FollowUpRequired { get; set; } = false;

    [DataType(DataType.Date)]
    [Display(Name = "Follow-up Date")]
    public DateTime? FollowUpDate { get; set; }

    // Navigation properties
    [ForeignKey("PatientId")]
    public virtual Patient Patient { get; set; } = null!;

    [ForeignKey("ClinicId")]
    public virtual Clinic Clinic { get; set; } = null!;

    [ForeignKey("DoctorId")]
    public virtual Staff? Doctor { get; set; }

    public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
}
