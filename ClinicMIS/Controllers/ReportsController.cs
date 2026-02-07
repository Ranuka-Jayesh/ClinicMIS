using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ClinicMIS.Services;

namespace ClinicMIS.Controllers;

/// <summary>
/// Reports controller
/// Generates various management reports
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class ReportsController : Controller
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportService reportService, ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    /// <summary>
    /// Reports menu/index
    /// </summary>
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// GET: Reports/DailyVisits
    /// </summary>
    public async Task<IActionResult> DailyVisits(DateTime? date)
    {
        var reportDate = date ?? DateTime.Today;
        
        try
        {
            var report = await _reportService.GetDailyVisitsReportAsync(reportDate);
            return View(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating daily visits report for {Date}", reportDate);
            TempData["ErrorMessage"] = "An error occurred while generating the report.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// GET: Reports/MonthlyRevenue
    /// </summary>
    public async Task<IActionResult> MonthlyRevenue(int? year, int? month)
    {
        var reportYear = year ?? DateTime.Today.Year;
        var reportMonth = month ?? DateTime.Today.Month;

        try
        {
            var report = await _reportService.GetMonthlyRevenueReportAsync(reportYear, reportMonth);
            return View(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating monthly revenue report for {Year}/{Month}", reportYear, reportMonth);
            TempData["ErrorMessage"] = "An error occurred while generating the report.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// GET: Reports/LowStock
    /// </summary>
    public async Task<IActionResult> LowStock()
    {
        try
        {
            var report = await _reportService.GetLowStockReportAsync();
            return View(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating low stock report");
            TempData["ErrorMessage"] = "An error occurred while generating the report.";
            return RedirectToAction(nameof(Index));
        }
    }
}
