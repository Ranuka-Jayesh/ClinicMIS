using Microsoft.EntityFrameworkCore;
using ClinicMIS.Data;
using ClinicMIS.Models.Entities;
using ClinicMIS.Models.ViewModels;

namespace ClinicMIS.Services;

/// <summary>
/// Service for generating reports with LINQ queries
/// </summary>
public class ReportService : IReportService
{
    private readonly ClinicDbContext _context;

    public ReportService(ClinicDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// REPORT 1: Daily Visits Report
    /// Shows all visits for a specific date with breakdown by clinic and doctor
    /// </summary>
    public async Task<DailyVisitsReportViewModel> GetDailyVisitsReportAsync(DateTime date)
    {
        var targetDate = date.Date;

        // Get all visits for the date
        var visitsQuery = _context.Visits
            .Where(v => v.VisitDate.Date == targetDate);

        // Total counts by status
        var statusCounts = await visitsQuery
            .GroupBy(v => v.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        // Visits by Clinic with revenue
        var visitsByClinic = await _context.Visits
            .Where(v => v.VisitDate.Date == targetDate)
            .GroupBy(v => v.Clinic.Name)
            .Select(g => new ClinicVisitSummary
            {
                ClinicName = g.Key,
                VisitCount = g.Count(),
                CompletedCount = g.Count(v => v.Status == VisitStatus.Completed),
                RevenueGenerated = g
                    .SelectMany(v => _context.Billings.Where(b => b.VisitId == v.VisitId))
                    .Sum(b => b.AmountPaid)
            })
            .ToListAsync();

        // Visits by Doctor with prescription count
        var visitsByDoctor = await _context.Visits
            .Where(v => v.VisitDate.Date == targetDate && v.DoctorId != null)
            .GroupBy(v => new { v.Doctor!.FirstName, v.Doctor.LastName, v.Clinic.Name })
            .Select(g => new DoctorVisitSummary
            {
                DoctorName = "Dr. " + g.Key.FirstName + " " + g.Key.LastName,
                ClinicName = g.Key.Name,
                VisitCount = g.Count(),
                PrescriptionCount = g.SelectMany(v => v.Prescriptions).Count()
            })
            .ToListAsync();

        // Detailed visit list
        var visitDetails = await _context.Visits
            .Where(v => v.VisitDate.Date == targetDate)
            .Include(v => v.Patient)
            .Include(v => v.Clinic)
            .Include(v => v.Doctor)
            .OrderBy(v => v.CheckInTime)
            .Select(v => new VisitDetailItem
            {
                VisitNumber = v.VisitNumber,
                PatientName = v.Patient.FirstName + " " + v.Patient.LastName,
                ClinicName = v.Clinic.Name,
                DoctorName = v.Doctor != null 
                    ? "Dr. " + v.Doctor.FirstName + " " + v.Doctor.LastName 
                    : "Not Assigned",
                CheckInTime = v.CheckInTime,
                CheckOutTime = v.CheckOutTime,
                Status = v.Status,
                Diagnosis = v.Diagnosis
            })
            .ToListAsync();

        return new DailyVisitsReportViewModel
        {
            ReportDate = targetDate,
            TotalVisits = statusCounts.Sum(s => s.Count),
            CompletedVisits = statusCounts.FirstOrDefault(s => s.Status == VisitStatus.Completed)?.Count ?? 0,
            CancelledVisits = statusCounts.FirstOrDefault(s => s.Status == VisitStatus.Cancelled)?.Count ?? 0,
            NoShowVisits = statusCounts.FirstOrDefault(s => s.Status == VisitStatus.NoShow)?.Count ?? 0,
            VisitsByClinic = visitsByClinic,
            VisitsByDoctor = visitsByDoctor,
            VisitDetails = visitDetails
        };
    }

    /// <summary>
    /// REPORT 2: Monthly Revenue Report
    /// Shows revenue breakdown for a specific month
    /// </summary>
    public async Task<MonthlyRevenueReportViewModel> GetMonthlyRevenueReportAsync(int year, int month)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        // Base query for the month
        var billingsQuery = _context.Billings
            .Where(b => b.BillingDate >= startDate && b.BillingDate <= endDate);

        // Overall totals using LINQ aggregation
        var totals = await billingsQuery
            .GroupBy(b => 1) // Group all
            .Select(g => new
            {
                TotalRevenue = g.Sum(b => b.AmountPaid),
                ConsultationRevenue = g.Sum(b => b.ConsultationFee),
                MedicationRevenue = g.Sum(b => b.MedicationCost),
                OtherRevenue = g.Sum(b => b.OtherCharges),
                TotalDiscounts = g.Sum(b => b.Discount),
                OutstandingAmount = g.Sum(b => b.TotalAmount - b.AmountPaid),
                TotalBillings = g.Count(),
                PaidBillings = g.Count(b => b.PaymentStatus == PaymentStatus.Paid),
                PendingBillings = g.Count(b => b.PaymentStatus == PaymentStatus.Pending)
            })
            .FirstOrDefaultAsync();

        // Daily breakdown
        var dailyBreakdown = await billingsQuery
            .GroupBy(b => b.BillingDate.Date)
            .Select(g => new DailyRevenueSummary
            {
                Date = g.Key,
                BillingCount = g.Count(),
                TotalAmount = g.Sum(b => b.TotalAmount),
                AmountCollected = g.Sum(b => b.AmountPaid)
            })
            .OrderBy(d => d.Date)
            .ToListAsync();

        // Payment method breakdown
        var paymentMethodBreakdown = await billingsQuery
            .Where(b => b.PaymentMethod != null && b.AmountPaid > 0)
            .GroupBy(b => b.PaymentMethod!.Value)
            .Select(g => new PaymentMethodSummary
            {
                PaymentMethod = g.Key,
                TransactionCount = g.Count(),
                TotalAmount = g.Sum(b => b.AmountPaid)
            })
            .ToListAsync();

        // Calculate percentages
        var totalPaid = paymentMethodBreakdown.Sum(p => p.TotalAmount);
        foreach (var pm in paymentMethodBreakdown)
        {
            pm.Percentage = totalPaid > 0 ? (pm.TotalAmount / totalPaid) * 100 : 0;
        }

        return new MonthlyRevenueReportViewModel
        {
            Year = year,
            Month = month,
            TotalRevenue = totals?.TotalRevenue ?? 0,
            ConsultationRevenue = totals?.ConsultationRevenue ?? 0,
            MedicationRevenue = totals?.MedicationRevenue ?? 0,
            OtherRevenue = totals?.OtherRevenue ?? 0,
            TotalDiscounts = totals?.TotalDiscounts ?? 0,
            OutstandingAmount = totals?.OutstandingAmount ?? 0,
            TotalBillings = totals?.TotalBillings ?? 0,
            PaidBillings = totals?.PaidBillings ?? 0,
            PendingBillings = totals?.PendingBillings ?? 0,
            DailyBreakdown = dailyBreakdown,
            PaymentMethodBreakdown = paymentMethodBreakdown
        };
    }

    /// <summary>
    /// REPORT 3: Low Stock Alert Report
    /// Shows drugs that are low in stock, out of stock, or expiring soon
    /// </summary>
    public async Task<LowStockReportViewModel> GetLowStockReportAsync()
    {
        var today = DateTime.Today;
        var thirtyDaysFromNow = today.AddDays(30);

        // Low stock items (stock <= reorder level but > 0)
        var lowStockItems = await _context.Drugs
            .Where(d => d.IsActive && d.QuantityInStock > 0 && d.QuantityInStock <= d.ReorderLevel)
            .OrderBy(d => d.QuantityInStock)
            .Select(d => new LowStockItem
            {
                DrugId = d.DrugId,
                DrugCode = d.DrugCode,
                DrugName = d.Name,
                Category = d.Category,
                CurrentStock = d.QuantityInStock,
                ReorderLevel = d.ReorderLevel,
                UnitPrice = d.UnitPrice
            })
            .ToListAsync();

        // Out of stock items (stock = 0)
        var outOfStockItems = await _context.Drugs
            .Where(d => d.IsActive && d.QuantityInStock == 0)
            .OrderBy(d => d.Name)
            .Select(d => new LowStockItem
            {
                DrugId = d.DrugId,
                DrugCode = d.DrugCode,
                DrugName = d.Name,
                Category = d.Category,
                CurrentStock = 0,
                ReorderLevel = d.ReorderLevel,
                UnitPrice = d.UnitPrice
            })
            .ToListAsync();

        // Expiring items (expiring within 30 days or already expired)
        var expiringItems = await _context.Drugs
            .Where(d => d.IsActive && d.ExpiryDate != null && d.ExpiryDate <= thirtyDaysFromNow)
            .OrderBy(d => d.ExpiryDate)
            .Select(d => new ExpiringDrugItem
            {
                DrugId = d.DrugId,
                DrugCode = d.DrugCode,
                DrugName = d.Name,
                ExpiryDate = d.ExpiryDate!.Value,
                QuantityInStock = d.QuantityInStock,
                StockValue = d.QuantityInStock * d.UnitPrice
            })
            .ToListAsync();

        var totalDrugs = await _context.Drugs.Where(d => d.IsActive).CountAsync();

        return new LowStockReportViewModel
        {
            TotalDrugs = totalDrugs,
            LowStockCount = lowStockItems.Count,
            OutOfStockCount = outOfStockItems.Count,
            ExpiringCount = expiringItems.Count,
            LowStockItems = lowStockItems,
            OutOfStockItems = outOfStockItems,
            ExpiringItems = expiringItems
        };
    }

    /// <summary>
    /// Dashboard summary data for home page
    /// </summary>
    public async Task<DashboardViewModel> GetDashboardDataAsync()
    {
        var today = DateTime.Today;
        var startOfMonth = new DateTime(today.Year, today.Month, 1);

        // Today's statistics
        var todayVisits = await _context.Visits
            .CountAsync(v => v.VisitDate.Date == today);

        var todayNewPatients = await _context.Patients
            .CountAsync(p => p.RegistrationDate.Date == today);

        var pendingPrescriptions = await _context.Prescriptions
            .CountAsync(p => p.Status == PrescriptionStatus.SentToPharmacy || 
                            p.Status == PrescriptionStatus.Processing);

        var lowStockAlerts = await _context.Drugs
            .CountAsync(d => d.IsActive && d.QuantityInStock <= d.ReorderLevel);

        // Monthly statistics
        var monthlyRevenue = await _context.Billings
            .Where(b => b.BillingDate >= startOfMonth && b.BillingDate <= today)
            .SumAsync(b => b.AmountPaid);

        var monthlyVisits = await _context.Visits
            .CountAsync(v => v.VisitDate >= startOfMonth && v.VisitDate <= today);

        var monthlyNewPatients = await _context.Patients
            .CountAsync(p => p.RegistrationDate >= startOfMonth && p.RegistrationDate <= today);

        // Upcoming visits (next 5)
        var upcomingVisits = await _context.Visits
            .Where(v => v.VisitDate >= today && v.Status == VisitStatus.Scheduled)
            .Include(v => v.Patient)
            .Include(v => v.Clinic)
            .Include(v => v.Doctor)
            .OrderBy(v => v.VisitDate)
            .ThenBy(v => v.CheckInTime)
            .Take(5)
            .ToListAsync();

        // Recent prescriptions pending in pharmacy
        var recentPrescriptions = await _context.Prescriptions
            .Where(p => p.Status == PrescriptionStatus.SentToPharmacy || 
                       p.Status == PrescriptionStatus.Processing)
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .Include(p => p.PrescriptionItems)
            .OrderBy(p => p.SentToPharmacyAt)
            .Take(5)
            .Select(p => new PrescriptionQueueItem
            {
                PrescriptionId = p.PrescriptionId,
                PrescriptionNumber = p.PrescriptionNumber,
                PatientName = p.Patient.FirstName + " " + p.Patient.LastName,
                PatientClinicNumber = p.Patient.ClinicNumber,
                DoctorName = "Dr. " + p.Doctor.FirstName + " " + p.Doctor.LastName,
                PrescriptionDate = p.PrescriptionDate,
                SentToPharmacyAt = p.SentToPharmacyAt,
                Status = p.Status,
                ItemCount = p.PrescriptionItems.Count,
                TotalAmount = p.PrescriptionItems.Sum(i => i.Quantity * i.UnitPrice)
            })
            .ToListAsync();

        // Critical stock items (top 5 most critical)
        var criticalStock = await _context.Drugs
            .Where(d => d.IsActive && d.QuantityInStock <= d.ReorderLevel)
            .OrderBy(d => d.QuantityInStock)
            .Take(5)
            .Select(d => new LowStockItem
            {
                DrugId = d.DrugId,
                DrugCode = d.DrugCode,
                DrugName = d.Name,
                Category = d.Category,
                CurrentStock = d.QuantityInStock,
                ReorderLevel = d.ReorderLevel,
                UnitPrice = d.UnitPrice
            })
            .ToListAsync();

        return new DashboardViewModel
        {
            TodayVisits = todayVisits,
            TodayNewPatients = todayNewPatients,
            PendingPrescriptions = pendingPrescriptions,
            LowStockAlerts = lowStockAlerts,
            MonthlyRevenue = monthlyRevenue,
            MonthlyVisits = monthlyVisits,
            MonthlyNewPatients = monthlyNewPatients,
            UpcomingVisits = upcomingVisits,
            RecentPrescriptions = recentPrescriptions,
            CriticalStockItems = criticalStock
        };
    }
}
