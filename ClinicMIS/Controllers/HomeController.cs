using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ClinicMIS.Models;
using ClinicMIS.Services;
using System.Diagnostics;

namespace ClinicMIS.Controllers;

/// <summary>
/// Home/Dashboard controller
/// </summary>
[Authorize]
public class HomeController : Controller
{
    private readonly IReportService _reportService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IReportService reportService, ILogger<HomeController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    /// <summary>
    /// Dashboard - shows summary statistics
    /// </summary>
    public async Task<IActionResult> Index()
    {
        try
        {
            var dashboard = await _reportService.GetDashboardDataAsync();
            return View(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard");
            return View("Error", new ErrorViewModel 
            { 
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier 
            });
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel 
        { 
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier 
        });
    }
}
