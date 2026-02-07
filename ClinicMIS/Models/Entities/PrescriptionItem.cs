using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicMIS.Models.Entities;

/// <summary>
/// Represents an individual drug item in a prescription
/// </summary>
[Table("PrescriptionItems")]
public class PrescriptionItem : BaseEntity
{
    [Key]
    public int PrescriptionItemId { get; set; }

    [Required]
    [Display(Name = "Prescription")]
    public int PrescriptionId { get; set; }

    [Required]
    [Display(Name = "Drug")]
    public int DrugId { get; set; }

    [Required]
    [Display(Name = "Quantity")]
    [Range(1, 1000, ErrorMessage = "Quantity must be between 1 and 1000")]
    public int Quantity { get; set; }

    /// <summary>
    /// Quantity actually dispensed (may differ from prescribed)
    /// </summary>
    [Display(Name = "Quantity Dispensed")]
    public int? QuantityDispensed { get; set; }

    /// <summary>
    /// Dosage instructions: "1 tablet twice daily", "5ml three times daily"
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Display(Name = "Dosage Instructions")]
    public string DosageInstructions { get; set; } = string.Empty;

    /// <summary>
    /// Duration in days
    /// </summary>
    [Display(Name = "Duration (Days)")]
    [Range(1, 365)]
    public int? DurationDays { get; set; }

    /// <summary>
    /// Unit price at time of prescription (copied from Drug.UnitPrice)
    /// </summary>
    [Required]
    [Display(Name = "Unit Price")]
    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; }

    [MaxLength(500)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    // Computed property
    [NotMapped]
    public decimal TotalPrice => Quantity * UnitPrice;

    [NotMapped]
    public decimal DispensedTotalPrice => (QuantityDispensed ?? 0) * UnitPrice;

    // Navigation properties
    [ForeignKey("PrescriptionId")]
    public virtual Prescription Prescription { get; set; } = null!;

    [ForeignKey("DrugId")]
    public virtual Drug Drug { get; set; } = null!;
}
