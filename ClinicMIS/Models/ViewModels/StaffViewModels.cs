using System.ComponentModel.DataAnnotations;
using ClinicMIS.Models.Entities;

namespace ClinicMIS.Models.ViewModels;

/// <summary>
/// ViewModel for creating/editing staff
/// </summary>
public class StaffViewModel
{
    public int StaffId { get; set; }

    [Required(ErrorMessage = "First name is required")]
    [MaxLength(50)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [MaxLength(50)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required")]
    [Display(Name = "Role")]
    public StaffRole Role { get; set; }

    [MaxLength(100)]
    [Display(Name = "Specialization")]
    public string? Specialization { get; set; }

    [MaxLength(50)]
    [Display(Name = "License Number")]
    public string? LicenseNumber { get; set; }

    [Required(ErrorMessage = "Phone number is required")]
    [Phone]
    [MaxLength(20)]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    [MaxLength(100)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Hire Date")]
    public DateTime HireDate { get; set; } = DateTime.Today;

    [Display(Name = "Assigned Clinic")]
    public int? ClinicId { get; set; }

    [Display(Name = "Create User Account")]
    public bool CreateUserAccount { get; set; } = true;

    [DataType(DataType.Password)]
    [Display(Name = "Initial Password")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string? InitialPassword { get; set; }

    // For dropdown
    public IEnumerable<Clinic>? AvailableClinics { get; set; }
}

/// <summary>
/// ViewModel for staff list
/// </summary>
public class StaffListViewModel
{
    public IEnumerable<StaffListItem> StaffMembers { get; set; } = new List<StaffListItem>();
    
    public string? SearchTerm { get; set; }
    public StaffRole? Role { get; set; }
    public int? ClinicId { get; set; }
    public bool? ActiveOnly { get; set; } = true;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }

    // Pagination
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    // For dropdowns
    public IEnumerable<Clinic>? AvailableClinics { get; set; }
}

public class StaffListItem
{
    public int StaffId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public StaffRole Role { get; set; }
    public string? Specialization { get; set; }
    public string? ClinicName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool HasUserAccount { get; set; }
}

/// <summary>
/// ViewModel for user login
/// </summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

/// <summary>
/// ViewModel for creating a user account for existing staff
/// </summary>
public class CreateStaffUserAccountViewModel
{
    public int StaffId { get; set; }
    
    public string StaffName { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public StaffRole Role { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm the password")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// ViewModel for changing password
/// </summary>
public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Current password is required")]
    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required")]
    [DataType(DataType.Password)]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your new password")]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    [Display(Name = "Confirm New Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
