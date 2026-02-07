using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicMIS.Models.Entities;

/// <summary>
/// Represents a billing/invoice for services and medications
/// </summary>
[Table("Billings")]
public class Billing : BaseEntity
{
    [Key]
    public int BillingId { get; set; }

    /// <summary>
    /// Invoice number (auto-generated)
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Display(Name = "Invoice Number")]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Patient")]
    public int PatientId { get; set; }

    /// <summary>
    /// Link to prescription if billing is for medication
    /// </summary>
    [Display(Name = "Prescription")]
    public int? PrescriptionId { get; set; }

    /// <summary>
    /// Link to visit if billing is for consultation
    /// </summary>
    [Display(Name = "Visit")]
    public int? VisitId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Billing Date")]
    public DateTime BillingDate { get; set; } = DateTime.Today;

    /// <summary>
    /// Consultation fee
    /// </summary>
    [Display(Name = "Consultation Fee")]
    [Column(TypeName = "decimal(10,2)")]
    public decimal ConsultationFee { get; set; } = 0;

    /// <summary>
    /// Total medication cost
    /// </summary>
    [Display(Name = "Medication Cost")]
    [Column(TypeName = "decimal(10,2)")]
    public decimal MedicationCost { get; set; } = 0;

    /// <summary>
    /// Any additional services
    /// </summary>
    [Display(Name = "Other Charges")]
    [Column(TypeName = "decimal(10,2)")]
    public decimal OtherCharges { get; set; } = 0;

    /// <summary>
    /// Discount amount
    /// </summary>
    [Display(Name = "Discount")]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Discount { get; set; } = 0;

    /// <summary>
    /// Tax amount
    /// </summary>
    [Display(Name = "Tax")]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Tax { get; set; } = 0;

    /// <summary>
    /// Total amount to be paid
    /// </summary>
    [Required]
    [Display(Name = "Total Amount")]
    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Amount paid so far
    /// </summary>
    [Display(Name = "Amount Paid")]
    [Column(TypeName = "decimal(10,2)")]
    public decimal AmountPaid { get; set; } = 0;

    [Required]
    [Display(Name = "Payment Status")]
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    [Display(Name = "Payment Method")]
    public PaymentMethod? PaymentMethod { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Payment Date")]
    public DateTime? PaymentDate { get; set; }

    [MaxLength(500)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    // Computed properties
    [NotMapped]
    public decimal SubTotal => ConsultationFee + MedicationCost + OtherCharges;

    [NotMapped]
    public decimal GrandTotal => SubTotal - Discount + Tax;

    [NotMapped]
    public decimal BalanceDue => TotalAmount - AmountPaid;

    [NotMapped]
    public bool IsPaid => PaymentStatus == PaymentStatus.Paid;

    // Navigation properties
    [ForeignKey("PatientId")]
    public virtual Patient Patient { get; set; } = null!;

    [ForeignKey("PrescriptionId")]
    public virtual Prescription? Prescription { get; set; }

    [ForeignKey("VisitId")]
    public virtual Visit? Visit { get; set; }
}
