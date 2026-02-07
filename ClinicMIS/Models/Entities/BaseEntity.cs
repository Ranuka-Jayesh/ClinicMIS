using System.ComponentModel.DataAnnotations;

namespace ClinicMIS.Models.Entities;

/// <summary>
/// Base entity with common audit properties
/// All entities inherit from this for consistent auditing
/// </summary>
public abstract class BaseEntity
{
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(100)]
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Concurrency token for optimistic concurrency control
    /// </summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; }

    /// <summary>
    /// Soft delete flag - records are never physically deleted
    /// </summary>
    public bool IsDeleted { get; set; } = false;
}
