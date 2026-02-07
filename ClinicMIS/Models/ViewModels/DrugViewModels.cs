using System.ComponentModel.DataAnnotations;

namespace ClinicMIS.Models.ViewModels;

/// <summary>
/// ViewModel for creating/editing a drug
/// </summary>
public class DrugViewModel
{
    public int DrugId { get; set; }

    [Required(ErrorMessage = "Drug code is required")]
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

    [Required(ErrorMessage = "Dosage form is required")]
    [MaxLength(50)]
    [Display(Name = "Dosage Form")]
    public string DosageForm { get; set; } = string.Empty;

    [Required(ErrorMessage = "Strength is required")]
    [MaxLength(50)]
    [Display(Name = "Strength")]
    public string Strength { get; set; } = string.Empty;

    [Required(ErrorMessage = "Unit price is required")]
    [Display(Name = "Unit Price")]
    [Range(0.01, 999999.99, ErrorMessage = "Price must be greater than 0")]
    public decimal UnitPrice { get; set; }

    [Required(ErrorMessage = "Initial stock is required")]
    [Display(Name = "Initial Stock Quantity")]
    [Range(0, int.MaxValue)]
    public int QuantityInStock { get; set; }

    [Required(ErrorMessage = "Reorder level is required")]
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
}

/// <summary>
/// ViewModel for drug list with search and filtering
/// </summary>
public class DrugListViewModel
{
    public IEnumerable<DrugListItem> Drugs { get; set; } = new List<DrugListItem>();
    
    public string? SearchTerm { get; set; }
    public string? Category { get; set; }
    public bool? LowStockOnly { get; set; }
    public bool? ExpiringSoonOnly { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }

    // Pagination
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    // For dropdown
    public IEnumerable<string>? AvailableCategories { get; set; }
}

public class DrugListItem
{
    public int DrugId { get; set; }
    public string DrugCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string DosageForm { get; set; } = string.Empty;
    public string Strength { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int QuantityInStock { get; set; }
    public int ReorderLevel { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsLowStock => QuantityInStock <= ReorderLevel;
    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.Today;
    public bool IsExpiringSoon => ExpiryDate.HasValue && 
        ExpiryDate.Value >= DateTime.Today && 
        ExpiryDate.Value <= DateTime.Today.AddDays(30);
}

/// <summary>
/// ViewModel for stock adjustment
/// </summary>
public class StockAdjustmentViewModel
{
    public int DrugId { get; set; }
    public string DrugName { get; set; } = string.Empty;
    public int CurrentStock { get; set; }

    [Required(ErrorMessage = "Quantity is required")]
    [Display(Name = "Adjustment Quantity")]
    public int AdjustmentQuantity { get; set; }

    [Required(ErrorMessage = "Reason is required")]
    [MaxLength(500)]
    [Display(Name = "Reason for Adjustment")]
    public string Reason { get; set; } = string.Empty;

    [Display(Name = "Adjustment Type")]
    public bool IsAddition { get; set; } = true; // true = add, false = subtract
}
