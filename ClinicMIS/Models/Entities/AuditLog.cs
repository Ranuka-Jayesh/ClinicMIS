using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicMIS.Models.Entities;

/// <summary>
/// Audit trail for tracking all system actions
/// </summary>
[Table("AuditLogs")]
public class AuditLog
{
    [Key]
    public long AuditLogId { get; set; }

    /// <summary>
    /// User who performed the action
    /// </summary>
    [MaxLength(450)]
    public string? UserId { get; set; }

    [MaxLength(100)]
    [Display(Name = "User Name")]
    public string? UserName { get; set; }

    [Required]
    [Display(Name = "Action")]
    public AuditAction Action { get; set; }

    /// <summary>
    /// Entity/Table name affected
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Display(Name = "Entity Name")]
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// Primary key of the affected record
    /// </summary>
    [MaxLength(50)]
    [Display(Name = "Entity ID")]
    public string? EntityId { get; set; }

    /// <summary>
    /// JSON representation of old values (for updates/deletes)
    /// </summary>
    [Display(Name = "Old Values")]
    public string? OldValues { get; set; }

    /// <summary>
    /// JSON representation of new values (for creates/updates)
    /// </summary>
    [Display(Name = "New Values")]
    public string? NewValues { get; set; }

    /// <summary>
    /// Properties that were changed (comma-separated)
    /// </summary>
    [MaxLength(1000)]
    [Display(Name = "Changed Properties")]
    public string? ChangedProperties { get; set; }

    [Required]
    [Display(Name = "Timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// IP address of the request
    /// </summary>
    [MaxLength(50)]
    [Display(Name = "IP Address")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Browser/client user agent
    /// </summary>
    [MaxLength(500)]
    [Display(Name = "User Agent")]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Additional context or notes
    /// </summary>
    [MaxLength(1000)]
    [Display(Name = "Additional Info")]
    public string? AdditionalInfo { get; set; }
}
