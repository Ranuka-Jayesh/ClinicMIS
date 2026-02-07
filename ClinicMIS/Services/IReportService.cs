using ClinicMIS.Models.ViewModels;

namespace ClinicMIS.Services;

/// <summary>
/// Service interface for generating reports
/// </summary>
public interface IReportService
{
    Task<DailyVisitsReportViewModel> GetDailyVisitsReportAsync(DateTime date);
    Task<MonthlyRevenueReportViewModel> GetMonthlyRevenueReportAsync(int year, int month);
    Task<LowStockReportViewModel> GetLowStockReportAsync();
    Task<DashboardViewModel> GetDashboardDataAsync();
}
