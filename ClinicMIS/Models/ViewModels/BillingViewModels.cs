using System.ComponentModel.DataAnnotations;
using ClinicMIS.Models.Entities;

namespace ClinicMIS.Models.ViewModels;

/// <summary>
/// ViewModel for creating a billing record
/// </summary>
public class BillingCreateViewModel
{
    [Required(ErrorMessage = "Patient is required")]
    [Display(Name = "Patient")]
    public int PatientId { get; set; }

    [Display(Name = "Prescription")]
    public int? PrescriptionId { get; set; }

    [Display(Name = "Visit")]
    public int? VisitId { get; set; }

    [Display(Name = "Consultation Fee")]
    [Range(0, 999999.99)]
    public decimal ConsultationFee { get; set; }

    [Display(Name = "Medication Cost")]
    [Range(0, 999999.99)]
    public decimal MedicationCost { get; set; }

    [Display(Name = "Other Charges")]
    [Range(0, 999999.99)]
    public decimal OtherCharges { get; set; }

    [Display(Name = "Discount")]
    [Range(0, 999999.99)]
    public decimal Discount { get; set; }

    [Display(Name = "Tax")]
    [Range(0, 999999.99)]
    public decimal Tax { get; set; }

    [MaxLength(500)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    // For display
    public Patient? Patient { get; set; }
    public IEnumerable<Patient>? AvailablePatients { get; set; }
}

/// <summary>
/// ViewModel for recording a payment
/// </summary>
public class PaymentViewModel
{
    public int BillingId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal BalanceDue { get; set; }

    [Required(ErrorMessage = "Payment amount is required")]
    [Display(Name = "Payment Amount")]
    [Range(0.01, 999999.99, ErrorMessage = "Amount must be greater than 0")]
    public decimal PaymentAmount { get; set; }

    [Required(ErrorMessage = "Payment method is required")]
    [Display(Name = "Payment Method")]
    public PaymentMethod PaymentMethod { get; set; }

    [MaxLength(500)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }
}

/// <summary>
/// ViewModel for billing list
/// </summary>
public class BillingListViewModel
{
    public IEnumerable<BillingListItem> Billings { get; set; } = new List<BillingListItem>();
    
    // Filters
    [DataType(DataType.Date)]
    [Display(Name = "From Date")]
    public DateTime? FromDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "To Date")]
    public DateTime? ToDate { get; set; }

    [Display(Name = "Payment Status")]
    public PaymentStatus? Status { get; set; }

    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }

    // Pagination
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    // Summary
    public decimal TotalBilled { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalOutstanding { get; set; }
}

public class BillingListItem
{
    public int BillingId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string PatientClinicNumber { get; set; } = string.Empty;
    public DateTime BillingDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal BalanceDue { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public bool HasPrescription { get; set; }
}

/// <summary>
/// ViewModel for invoice/receipt printout
/// </summary>
public class InvoiceViewModel
{
    public Billing Billing { get; set; } = null!;
    public Patient Patient { get; set; } = null!;
    public Prescription? Prescription { get; set; }
    public IEnumerable<PrescriptionItem>? PrescriptionItems { get; set; }
    
    // Clinic details for header
    public string ClinicName { get; set; } = "University Clinic";
    public string ClinicAddress { get; set; } = "123 University Road";
    public string ClinicPhone { get; set; } = "+1 234 567 8900";
    public string ClinicEmail { get; set; } = "info@universityclinic.edu";
}
