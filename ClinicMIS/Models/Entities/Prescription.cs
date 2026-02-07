using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicMIS.Models.Entities;

/// <summary>
/// Represents a prescription written by a doctor
/// </summary>
[Table("Prescriptions")]
public class Prescription : BaseEntity
{
    [Key]
    public int PrescriptionId { get; set; }

    /// <summary>
    /// Prescription reference number (auto-generated)
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Display(Name = "Prescription Number")]
    public string PrescriptionNumber { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Patient")]
    public int PatientId { get; set; }

    [Required]
    [Display(Name = "Prescribing Doctor")]
    public int DoctorId { get; set; }

    [Display(Name = "Visit")]
    public int? VisitId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Prescription Date")]
    public DateTime PrescriptionDate { get; set; } = DateTime.Today;

    [Required]
    [Display(Name = "Status")]
    public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Draft;

    [MaxLength(500)]
    [Display(Name = "Diagnosis")]
    public string? Diagnosis { get; set; }

    [MaxLength(1000)]
    [Display(Name = "Special Instructions")]
    public string? SpecialInstructions { get; set; }

    /// <summary>
    /// Time prescription was sent to pharmacy
    /// </summary>
    [Display(Name = "Sent to Pharmacy At")]
    public DateTime? SentToPharmacyAt { get; set; }

    /// <summary>
    /// Pharmacist who dispensed the prescription
    /// </summary>
    [Display(Name = "Dispensed By")]
    public int? DispensedByStaffId { get; set; }

    [Display(Name = "Dispensed At")]
    public DateTime? DispensedAt { get; set; }

    // Navigation properties
    [ForeignKey("PatientId")]
    public virtual Patient Patient { get; set; } = null!;

    [ForeignKey("DoctorId")]
    public virtual Staff Doctor { get; set; } = null!;

    [ForeignKey("VisitId")]
    public virtual Visit? Visit { get; set; }

    [ForeignKey("DispensedByStaffId")]
    public virtual Staff? DispensedByStaff { get; set; }

    public virtual ICollection<PrescriptionItem> PrescriptionItems { get; set; } = new List<PrescriptionItem>();
    public virtual Billing? Billing { get; set; }
}
