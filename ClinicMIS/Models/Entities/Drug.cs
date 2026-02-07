using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicMIS.Models.Entities;

/// <summary>
/// Represents a drug/medication in the pharmacy inventory
/// </summary>
[Table("Drugs")]
public class Drug : BaseEntity
{
    [Key]
    public int DrugId { get; set; }

    /// <summary>
    /// Drug code for quick lookup
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Display(Name = "Drug Code")]
    public string DrugCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Drug name is required")]
    [MaxLength(200)]
    [Display(Name = "Drug Name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    [Display(Name = "Generic Name")]
    public string? GenericName { get; set; }

    [MaxLength(100)]
    [Display(Name = "Manufacturer")]
    public string? Manufacturer { get; set; }

    [Required(ErrorMessage = "Category is required")]
    [MaxLength(50)]
    [Display(Name = "Category")]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Dosage form: Tablet, Capsule, Syrup, Injection, etc.
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Display(Name = "Dosage Form")]
    public string DosageForm { get; set; } = string.Empty;

    /// <summary>
    /// Strength: 500mg, 250mg/5ml, etc.
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Display(Name = "Strength")]
    public string Strength { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Unit Price")]
    [Column(TypeName = "decimal(10,2)")]
    [Range(0.01, 999999.99, ErrorMessage = "Price must be greater than 0")]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Current quantity in stock
    /// </summary>
    [Required]
    [Display(Name = "Quantity in Stock")]
    [Range(0, int.MaxValue)]
    public int QuantityInStock { get; set; }

    /// <summary>
    /// Minimum stock level for low-stock alerts
    /// </summary>
    [Required]
    [Display(Name = "Reorder Level")]
    [Range(1, int.MaxValue)]
    public int ReorderLevel { get; set; } = 10;

    [DataType(DataType.Date)]
    [Display(Name = "Expiry Date")]
    public DateTime? ExpiryDate { get; set; }

    [MaxLength(500)]
    [Display(Name = "Storage Instructions")]
    public string? StorageInstructions { get; set; }

    [Display(Name = "Requires Prescription")]
    public bool RequiresPrescription { get; set; } = true;

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    // Computed properties
    [NotMapped]
    public bool IsLowStock => QuantityInStock <= ReorderLevel;

    [NotMapped]
    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.Today;

    [NotMapped]
    public string DisplayName => $"{Name} ({Strength})";

    // Navigation properties
    public virtual ICollection<PrescriptionItem> PrescriptionItems { get; set; } = new List<PrescriptionItem>();
    public virtual ICollection<Dispensing> Dispensings { get; set; } = new List<Dispensing>();
}
