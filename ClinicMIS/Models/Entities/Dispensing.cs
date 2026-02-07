using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicMIS.Models.Entities;

/// <summary>
/// Tracks drug dispensing from pharmacy - separate from prescription for stock management
/// </summary>
[Table("Dispensings")]
public class Dispensing : BaseEntity
{
    [Key]
    public int DispensingId { get; set; }

    [Required]
    [MaxLength(20)]
    [Display(Name = "Dispensing Number")]
    public string DispensingNumber { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Drug")]
    public int DrugId { get; set; }

    [Required]
    [Display(Name = "Pharmacist")]
    public int PharmacistId { get; set; }

    /// <summary>
    /// Optional link to prescription item
    /// </summary>
    [Display(Name = "Prescription Item")]
    public int? PrescriptionItemId { get; set; }

    [Required]
    [Display(Name = "Quantity Dispensed")]
    [Range(1, int.MaxValue)]
    public int QuantityDispensed { get; set; }

    [Required]
    [Display(Name = "Unit Price")]
    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; }

    [Required]
    [Display(Name = "Dispensing Date")]
    public DateTime DispensingDate { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    /// <summary>
    /// Stock level before dispensing (for audit)
    /// </summary>
    [Display(Name = "Stock Before")]
    public int StockBefore { get; set; }

    /// <summary>
    /// Stock level after dispensing (for audit)
    /// </summary>
    [Display(Name = "Stock After")]
    public int StockAfter { get; set; }

    // Computed property
    [NotMapped]
    public decimal TotalAmount => QuantityDispensed * UnitPrice;

    // Navigation properties
    [ForeignKey("DrugId")]
    public virtual Drug Drug { get; set; } = null!;

    [ForeignKey("PharmacistId")]
    public virtual Staff Pharmacist { get; set; } = null!;

    [ForeignKey("PrescriptionItemId")]
    public virtual PrescriptionItem? PrescriptionItem { get; set; }
}
