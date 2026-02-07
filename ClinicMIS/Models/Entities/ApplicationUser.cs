using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ClinicMIS.Models.Entities;

/// <summary>
/// Extended Identity user for the clinic system
/// </summary>
public class ApplicationUser : IdentityUser
{
    [MaxLength(50)]
    [Display(Name = "First Name")]
    public string? FirstName { get; set; }

    [MaxLength(50)]
    [Display(Name = "Last Name")]
    public string? LastName { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Number of failed login attempts
    /// </summary>
    public int FailedLoginAttempts { get; set; } = 0;

    /// <summary>
    /// Account locked until this time
    /// </summary>
    public DateTime? LockedUntil { get; set; }

    // Navigation property
    public virtual Staff? Staff { get; set; }
}
