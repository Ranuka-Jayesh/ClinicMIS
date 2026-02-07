using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ClinicMIS.Data;
using ClinicMIS.Models.Entities;
using ClinicMIS.Models.ViewModels;

namespace ClinicMIS.Controllers;

/// <summary>
/// Staff management controller
/// Admin-only: manages staff records and user accounts
/// </summary>
[Authorize(Policy = "CanManageStaff")]
public class StaffController : Controller
{
    private readonly ClinicDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<StaffController> _logger;

    public StaffController(
        ClinicDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<StaffController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// GET: Staff - List with filtering
    /// </summary>
    public async Task<IActionResult> Index(StaffListViewModel filter)
    {
        var query = _context.Staff
            .Include(s => s.Clinic)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(s =>
                s.EmployeeNumber.ToLower().Contains(term) ||
                s.FirstName.ToLower().Contains(term) ||
                s.LastName.ToLower().Contains(term) ||
                s.Email.ToLower().Contains(term));
        }

        if (filter.Role.HasValue)
            query = query.Where(s => s.Role == filter.Role.Value);

        if (filter.ClinicId.HasValue)
            query = query.Where(s => s.ClinicId == filter.ClinicId.Value);

        if (filter.ActiveOnly == true)
            query = query.Where(s => s.IsActive);

        filter.TotalCount = await query.CountAsync();

        // Sorting
        query = filter.SortBy?.ToLower() switch
        {
            "name" => filter.SortDescending
                ? query.OrderByDescending(s => s.LastName).ThenByDescending(s => s.FirstName)
                : query.OrderBy(s => s.LastName).ThenBy(s => s.FirstName),
            "role" => filter.SortDescending
                ? query.OrderByDescending(s => s.Role)
                : query.OrderBy(s => s.Role),
            "clinic" => filter.SortDescending
                ? query.OrderByDescending(s => s.Clinic!.Name)
                : query.OrderBy(s => s.Clinic!.Name),
            _ => query.OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
        };

        // Pagination
        filter.StaffMembers = await query
            .Skip((filter.CurrentPage - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(s => new StaffListItem
            {
                StaffId = s.StaffId,
                EmployeeNumber = s.EmployeeNumber,
                FullName = s.FirstName + " " + s.LastName,
                Role = s.Role,
                Specialization = s.Specialization,
                ClinicName = s.Clinic != null ? s.Clinic.Name : null,
                Email = s.Email,
                PhoneNumber = s.PhoneNumber,
                IsActive = s.IsActive,
                HasUserAccount = s.UserId != null
            })
            .ToListAsync();

        filter.AvailableClinics = await _context.Clinics.Where(c => c.IsActive).ToListAsync();

        return View(filter);
    }

    /// <summary>
    /// GET: Staff/Details/5
    /// </summary>
    public async Task<IActionResult> Details(int id)
    {
        var staff = await _context.Staff
            .Include(s => s.Clinic)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.StaffId == id);

        if (staff == null)
        {
            return NotFound();
        }

        return View(staff);
    }

    /// <summary>
    /// GET: Staff/Create
    /// </summary>
    public async Task<IActionResult> Create()
    {
        var model = new StaffViewModel
        {
            AvailableClinics = await _context.Clinics.Where(c => c.IsActive).ToListAsync()
        };
        return View(model);
    }

    /// <summary>
    /// POST: Staff/Create
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StaffViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableClinics = await _context.Clinics.Where(c => c.IsActive).ToListAsync();
            return View(model);
        }

        // Check duplicate email
        if (await _context.Staff.AnyAsync(s => s.Email == model.Email))
        {
            ModelState.AddModelError("Email", "This email is already in use.");
            model.AvailableClinics = await _context.Clinics.Where(c => c.IsActive).ToListAsync();
            return View(model);
        }

        try
        {
            // Generate employee number
            var employeeNumber = await GenerateEmployeeNumberAsync();

            var staff = new Staff
            {
                EmployeeNumber = employeeNumber,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Role = model.Role,
                Specialization = model.Specialization,
                LicenseNumber = model.LicenseNumber,
                PhoneNumber = model.PhoneNumber,
                Email = model.Email,
                HireDate = model.HireDate,
                ClinicId = model.ClinicId,
                IsActive = true
            };

            // Create user account if requested
            if (model.CreateUserAccount && !string.IsNullOrEmpty(model.InitialPassword))
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await _userManager.CreateAsync(user, model.InitialPassword);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    model.AvailableClinics = await _context.Clinics.Where(c => c.IsActive).ToListAsync();
                    return View(model);
                }

                // Assign role
                var roleName = model.Role.ToString();
                await _userManager.AddToRoleAsync(user, roleName);

                staff.UserId = user.Id;
            }

            _context.Staff.Add(staff);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Staff member created. Employee Number: {staff.EmployeeNumber}";
            return RedirectToAction(nameof(Details), new { id = staff.StaffId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating staff member");
            ModelState.AddModelError("", "An error occurred while creating the staff member.");
            model.AvailableClinics = await _context.Clinics.Where(c => c.IsActive).ToListAsync();
            return View(model);
        }
    }

    /// <summary>
    /// GET: Staff/Edit/5
    /// </summary>
    public async Task<IActionResult> Edit(int id)
    {
        var staff = await _context.Staff.FindAsync(id);
        if (staff == null)
        {
            return NotFound();
        }

        var model = new StaffViewModel
        {
            StaffId = staff.StaffId,
            FirstName = staff.FirstName,
            LastName = staff.LastName,
            Role = staff.Role,
            Specialization = staff.Specialization,
            LicenseNumber = staff.LicenseNumber,
            PhoneNumber = staff.PhoneNumber,
            Email = staff.Email,
            HireDate = staff.HireDate,
            ClinicId = staff.ClinicId,
            AvailableClinics = await _context.Clinics.Where(c => c.IsActive).ToListAsync()
        };

        return View(model);
    }

    /// <summary>
    /// POST: Staff/Edit/5
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, StaffViewModel model)
    {
        if (id != model.StaffId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            model.AvailableClinics = await _context.Clinics.Where(c => c.IsActive).ToListAsync();
            return View(model);
        }

        var staff = await _context.Staff.FindAsync(id);
        if (staff == null)
        {
            return NotFound();
        }

        staff.FirstName = model.FirstName;
        staff.LastName = model.LastName;
        staff.Role = model.Role;
        staff.Specialization = model.Specialization;
        staff.LicenseNumber = model.LicenseNumber;
        staff.PhoneNumber = model.PhoneNumber;
        staff.ClinicId = model.ClinicId;

        try
        {
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Staff member updated successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError("", "The record was modified by another user.");
            model.AvailableClinics = await _context.Clinics.Where(c => c.IsActive).ToListAsync();
            return View(model);
        }
    }

    /// <summary>
    /// POST: Staff/Deactivate/5
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var staff = await _context.Staff.Include(s => s.User).FirstOrDefaultAsync(s => s.StaffId == id);
        if (staff == null)
        {
            return NotFound();
        }

        staff.IsActive = false;

        // Deactivate user account if exists
        if (staff.User != null)
        {
            staff.User.IsActive = false;
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Staff member deactivated.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// POST: Staff/Activate/5
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(int id)
    {
        var staff = await _context.Staff.Include(s => s.User).FirstOrDefaultAsync(s => s.StaffId == id);
        if (staff == null)
        {
            return NotFound();
        }

        staff.IsActive = true;

        // Activate user account if exists
        if (staff.User != null)
        {
            staff.User.IsActive = true;
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Staff member activated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// GET: Staff/CreateUserAccount/5
    /// Show form to create a user account for an existing staff member
    /// </summary>
    public async Task<IActionResult> CreateUserAccount(int id)
    {
        var staff = await _context.Staff.FindAsync(id);
        if (staff == null)
        {
            return NotFound();
        }

        // Check if staff already has a user account
        if (!string.IsNullOrEmpty(staff.UserId))
        {
            TempData["ErrorMessage"] = "This staff member already has a user account.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = new CreateStaffUserAccountViewModel
        {
            StaffId = staff.StaffId,
            StaffName = staff.FullName,
            Email = staff.Email,
            Role = staff.Role
        };

        return View(model);
    }

    /// <summary>
    /// POST: Staff/CreateUserAccount/5
    /// Create a user account for an existing staff member
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUserAccount(int id, CreateStaffUserAccountViewModel model)
    {
        if (id != model.StaffId)
        {
            return BadRequest();
        }

        var staff = await _context.Staff.FindAsync(id);
        if (staff == null)
        {
            return NotFound();
        }

        // Check if staff already has a user account
        if (!string.IsNullOrEmpty(staff.UserId))
        {
            TempData["ErrorMessage"] = "This staff member already has a user account.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (!ModelState.IsValid)
        {
            model.StaffName = staff.FullName;
            model.Email = staff.Email;
            model.Role = staff.Role;
            return View(model);
        }

        try
        {
            // Check if a user with this email already exists
            var existingUser = await _userManager.FindByEmailAsync(staff.Email);
            if (existingUser != null)
            {
                // Link the existing user to this staff member
                staff.UserId = existingUser.Id;
                
                // Make sure the user has the correct role
                var roleName = staff.Role.ToString();
                if (!await _userManager.IsInRoleAsync(existingUser, roleName))
                {
                    await _userManager.AddToRoleAsync(existingUser, roleName);
                }
                
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Existing user account linked to staff member successfully.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Create new user account
            var user = new ApplicationUser
            {
                UserName = staff.Email,
                Email = staff.Email,
                FirstName = staff.FirstName,
                LastName = staff.LastName,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                model.StaffName = staff.FullName;
                model.Email = staff.Email;
                model.Role = staff.Role;
                return View(model);
            }

            // Assign role based on staff role
            var staffRoleName = staff.Role.ToString();
            await _userManager.AddToRoleAsync(user, staffRoleName);

            // Link user to staff record
            staff.UserId = user.Id;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "User account created successfully. The staff member can now log in.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user account for staff {StaffId}", id);
            ModelState.AddModelError("", "An error occurred while creating the user account.");
            model.StaffName = staff.FullName;
            model.Email = staff.Email;
            model.Role = staff.Role;
            return View(model);
        }
    }

    /// <summary>
    /// GET: Staff/Delete/5
    /// </summary>
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        var staff = await _context.Staff
            .Include(s => s.Clinic)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.StaffId == id);

        if (staff == null)
        {
            return NotFound();
        }

        return View(staff);
    }

    /// <summary>
    /// POST: Staff/Delete/5
    /// </summary>
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var staff = await _context.Staff
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StaffId == id);

            if (staff == null)
            {
                return NotFound();
            }

            // Check if staff has any related records that prevent deletion
            var hasVisits = await _context.Visits.AnyAsync(v => v.DoctorId == id);
            var hasPrescriptions = await _context.Prescriptions.AnyAsync(p => p.DoctorId == id);

            if (hasVisits || hasPrescriptions)
            {
                TempData["ErrorMessage"] = "Cannot delete staff member with existing visits or prescriptions. Consider deactivating instead.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Delete associated user account if exists
            if (staff.User != null)
            {
                await _userManager.DeleteAsync(staff.User);
            }

            // Soft delete (handled by DbContext)
            _context.Staff.Remove(staff);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Staff member deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting staff {StaffId}", id);
            TempData["ErrorMessage"] = "An error occurred while deleting the staff member.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    private async Task<string> GenerateEmployeeNumberAsync()
    {
        var prefix = "EMP-";
        var lastNumber = await _context.Staff
            .IgnoreQueryFilters()
            .OrderByDescending(s => s.EmployeeNumber)
            .Select(s => s.EmployeeNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (!string.IsNullOrEmpty(lastNumber))
        {
            var numPart = lastNumber.Replace(prefix, "");
            if (int.TryParse(numPart, out int parsed))
            {
                nextNumber = parsed + 1;
            }
        }

        return $"{prefix}{nextNumber:D4}";
    }
}
