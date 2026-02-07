using System.ComponentModel.DataAnnotations;
using ClinicMIS.Models.Entities;

namespace ClinicMIS.Models.ViewModels;

/// <summary>
/// ViewModel for creating a new prescription
/// </summary>
public class PrescriptionCreateViewModel
{
    [Required(ErrorMessage = "Patient is required")]
    [Display(Name = "Patient")]
    public int PatientId { get; set; }

    [Display(Name = "Prescribing Doctor")]
    public int? DoctorId { get; set; }

    [Display(Name = "Visit")]
    public int? VisitId { get; set; }

    [MaxLength(500)]
    [Display(Name = "Diagnosis")]
    public string? Diagnosis { get; set; }

    [MaxLength(1000)]
    [Display(Name = "Special Instructions")]
    public string? SpecialInstructions { get; set; }

    public List<PrescriptionItemViewModel> Items { get; set; } = new();

    // For dropdowns
    public IEnumerable<Patient>? AvailablePatients { get; set; }
    public IEnumerable<Drug>? AvailableDrugs { get; set; }
    public IEnumerable<Staff>? AvailableDoctors { get; set; }
    
    // Indicates if the current user is a doctor
    public bool CurrentUserIsDoctor { get; set; }
}

/// <summary>
/// ViewModel for prescription item in the form
/// </summary>
public class PrescriptionItemViewModel
{
    [Required]
    [Display(Name = "Drug")]
    public int DrugId { get; set; }

    [Required]
    [Range(1, 1000)]
    [Display(Name = "Quantity")]
    public int Quantity { get; set; }

    [Required]
    [MaxLength(200)]
    [Display(Name = "Dosage Instructions")]
    public string DosageInstructions { get; set; } = string.Empty;

    [Range(1, 365)]
    [Display(Name = "Duration (Days)")]
    public int? DurationDays { get; set; }

    [MaxLength(500)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    // For display
    public string? DrugName { get; set; }
    public decimal UnitPrice { get; set; }
}

/// <summary>
/// ViewModel for prescription list (pharmacy queue)
/// </summary>
public class PharmacyQueueViewModel
{
    public IEnumerable<PrescriptionQueueItem> PendingPrescriptions { get; set; } = new List<PrescriptionQueueItem>();
    public IEnumerable<PrescriptionQueueItem> ProcessingPrescriptions { get; set; } = new List<PrescriptionQueueItem>();
    public IEnumerable<PrescriptionQueueItem> ReadyPrescriptions { get; set; } = new List<PrescriptionQueueItem>();
    public string? SearchTerm { get; set; }
}

public class PrescriptionQueueItem
{
    public int PrescriptionId { get; set; }
    public string PrescriptionNumber { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string PatientClinicNumber { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public DateTime PrescriptionDate { get; set; }
    public DateTime? SentToPharmacyAt { get; set; }
    public PrescriptionStatus Status { get; set; }
    public int ItemCount { get; set; }
    public decimal TotalAmount { get; set; }
    public TimeSpan WaitingTime => SentToPharmacyAt.HasValue 
        ? DateTime.UtcNow - SentToPharmacyAt.Value 
        : TimeSpan.Zero;
}

/// <summary>
/// ViewModel for dispensing a prescription
/// </summary>
public class DispenseViewModel
{
    public Prescription Prescription { get; set; } = null!;
    public List<DispenseItemViewModel> Items { get; set; } = new();
}

public class DispenseItemViewModel
{
    public int PrescriptionItemId { get; set; }
    public int DrugId { get; set; }
    public string DrugName { get; set; } = string.Empty;
    public string DosageInstructions { get; set; } = string.Empty;
    public int QuantityPrescribed { get; set; }
    public int QuantityToDispense { get; set; }
    public int AvailableStock { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Notes { get; set; }
    public bool CanDispense => AvailableStock >= QuantityToDispense;
}
