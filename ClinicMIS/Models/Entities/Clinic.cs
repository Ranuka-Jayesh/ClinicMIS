using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicMIS.Models.Entities;

/// <summary>
/// Represents a medical clinic/department (Heart, Cancer, Neurology, etc.)
/// </summary>
[Table("Clinics")]
public class Clinic : BaseEntity
{
    [Key]
    public int ClinicId { get; set; }

    [Required(ErrorMessage = "Clinic name is required")]
    [MaxLength(100)]
    [Display(Name = "Clinic Name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [MaxLength(50)]
    [Display(Name = "Location/Building")]
    public string? Location { get; set; }

    [Phone]
    [MaxLength(20)]
    [Display(Name = "Contact Phone")]
    public string? ContactPhone { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<Staff> Staff { get; set; } = new List<Staff>();
    public virtual ICollection<Visit> Visits { get; set; } = new List<Visit>();
}
