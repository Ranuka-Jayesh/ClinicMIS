using System.ComponentModel.DataAnnotations;
using ClinicMIS.Models.Entities;

namespace ClinicMIS.Models.ViewModels;

/// <summary>
/// ViewModel for Daily Visits Report
/// </summary>
public class DailyVisitsReportViewModel
{
    [DataType(DataType.Date)]
    [Display(Name = "Report Date")]
    public DateTime ReportDate { get; set; } = DateTime.Today;

    public int TotalVisits { get; set; }
    public int CompletedVisits { get; set; }
    public int CancelledVisits { get; set; }
    public int NoShowVisits { get; set; }

    public IEnumerable<ClinicVisitSummary> VisitsByClinic { get; set; } = new List<ClinicVisitSummary>();
    public IEnumerable<DoctorVisitSummary> VisitsByDoctor { get; set; } = new List<DoctorVisitSummary>();
    public IEnumerable<VisitDetailItem> VisitDetails { get; set; } = new List<VisitDetailItem>();
}

public class ClinicVisitSummary
{
    public string ClinicName { get; set; } = string.Empty;
    public int VisitCount { get; set; }
    public int CompletedCount { get; set; }
    public decimal RevenueGenerated { get; set; }
}

public class DoctorVisitSummary
{
    public string DoctorName { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;
    public int VisitCount { get; set; }
    public int PrescriptionCount { get; set; }
}

public class VisitDetailItem
{
    public string VisitNumber { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public TimeSpan? CheckInTime { get; set; }
    public TimeSpan? CheckOutTime { get; set; }
    public VisitStatus Status { get; set; }
    public string? Diagnosis { get; set; }
}

/// <summary>
/// ViewModel for Monthly Revenue Report
/// </summary>
public class MonthlyRevenueReportViewModel
{
    [Display(Name = "Year")]
    public int Year { get; set; } = DateTime.Today.Year;

    [Display(Name = "Month")]
    public int Month { get; set; } = DateTime.Today.Month;

    public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy");

    public decimal TotalRevenue { get; set; }
    public decimal ConsultationRevenue { get; set; }
    public decimal MedicationRevenue { get; set; }
    public decimal OtherRevenue { get; set; }
    public decimal TotalDiscounts { get; set; }
    public decimal OutstandingAmount { get; set; }

    public int TotalBillings { get; set; }
    public int PaidBillings { get; set; }
    public int PendingBillings { get; set; }

    public IEnumerable<DailyRevenueSummary> DailyBreakdown { get; set; } = new List<DailyRevenueSummary>();
    public IEnumerable<PaymentMethodSummary> PaymentMethodBreakdown { get; set; } = new List<PaymentMethodSummary>();
}

public class DailyRevenueSummary
{
    public DateTime Date { get; set; }
    public int BillingCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountCollected { get; set; }
}

public class PaymentMethodSummary
{
    public PaymentMethod PaymentMethod { get; set; }
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// ViewModel for Low Stock Alert Report
/// </summary>
public class LowStockReportViewModel
{
    public int TotalDrugs { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public int ExpiringCount { get; set; }

    public IEnumerable<LowStockItem> LowStockItems { get; set; } = new List<LowStockItem>();
    public IEnumerable<LowStockItem> OutOfStockItems { get; set; } = new List<LowStockItem>();
    public IEnumerable<ExpiringDrugItem> ExpiringItems { get; set; } = new List<ExpiringDrugItem>();
}

public class LowStockItem
{
    public int DrugId { get; set; }
    public string DrugCode { get; set; } = string.Empty;
    public string DrugName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int ReorderLevel { get; set; }
    public int ShortfallQuantity => ReorderLevel - CurrentStock;
    public decimal UnitPrice { get; set; }
    public decimal ReorderCost => ShortfallQuantity > 0 ? ShortfallQuantity * UnitPrice : 0;
}

public class ExpiringDrugItem
{
    public int DrugId { get; set; }
    public string DrugCode { get; set; } = string.Empty;
    public string DrugName { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public int DaysUntilExpiry => (ExpiryDate - DateTime.Today).Days;
    public int QuantityInStock { get; set; }
    public decimal StockValue { get; set; }
    public bool IsExpired => ExpiryDate < DateTime.Today;
}

/// <summary>
/// Dashboard summary ViewModel
/// </summary>
public class DashboardViewModel
{
    // Today's statistics
    public int TodayVisits { get; set; }
    public int TodayNewPatients { get; set; }
    public int PendingPrescriptions { get; set; }
    public int LowStockAlerts { get; set; }

    // Monthly statistics
    public decimal MonthlyRevenue { get; set; }
    public int MonthlyVisits { get; set; }
    public int MonthlyNewPatients { get; set; }

    // Quick lists
    public IEnumerable<Visit> UpcomingVisits { get; set; } = new List<Visit>();
    public IEnumerable<PrescriptionQueueItem> RecentPrescriptions { get; set; } = new List<PrescriptionQueueItem>();
    public IEnumerable<LowStockItem> CriticalStockItems { get; set; } = new List<LowStockItem>();
}
