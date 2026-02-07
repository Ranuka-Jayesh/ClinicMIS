using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ClinicMIS.Models.Entities;
using System.Text.Json;

namespace ClinicMIS.Data;

/// <summary>
/// Main database context for the Clinic MIS application
/// Inherits from IdentityDbContext for user authentication
/// </summary>
public class ClinicDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public ClinicDbContext(DbContextOptions<ClinicDbContext> options, 
        IHttpContextAccessor? httpContextAccessor = null) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // DbSets for all entities
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Clinic> Clinics => Set<Clinic>();
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<Visit> Visits => Set<Visit>();
    public DbSet<Drug> Drugs => Set<Drug>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<Dispensing> Dispensings => Set<Dispensing>();
    public DbSet<Billing> Billings => Set<Billing>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ========== Patient Configuration ==========
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasIndex(p => p.ClinicNumber).IsUnique();
            entity.HasIndex(p => p.NationalId);
            entity.HasIndex(p => p.PhoneNumber);
            entity.HasIndex(p => new { p.LastName, p.FirstName });

            // Soft delete filter
            entity.HasQueryFilter(p => !p.IsDeleted);
        });

        // ========== Clinic Configuration ==========
        modelBuilder.Entity<Clinic>(entity =>
        {
            entity.HasIndex(c => c.Name).IsUnique();
            entity.HasQueryFilter(c => !c.IsDeleted);
        });

        // ========== Staff Configuration ==========
        modelBuilder.Entity<Staff>(entity =>
        {
            entity.HasIndex(s => s.EmployeeNumber).IsUnique();
            entity.HasIndex(s => s.Email).IsUnique();
            entity.HasIndex(s => s.LicenseNumber);

            // Staff belongs to a Clinic
            entity.HasOne(s => s.Clinic)
                .WithMany(c => c.Staff)
                .HasForeignKey(s => s.ClinicId)
                .OnDelete(DeleteBehavior.SetNull);

            // Staff linked to ApplicationUser
            entity.HasOne(s => s.User)
                .WithOne(u => u.Staff)
                .HasForeignKey<Staff>(s => s.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasQueryFilter(s => !s.IsDeleted);
        });

        // ========== Visit Configuration ==========
        modelBuilder.Entity<Visit>(entity =>
        {
            entity.HasIndex(v => v.VisitNumber).IsUnique();
            entity.HasIndex(v => v.VisitDate);
            entity.HasIndex(v => v.Status);

            // Visit belongs to Patient
            entity.HasOne(v => v.Patient)
                .WithMany(p => p.Visits)
                .HasForeignKey(v => v.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Visit belongs to Clinic
            entity.HasOne(v => v.Clinic)
                .WithMany(c => c.Visits)
                .HasForeignKey(v => v.ClinicId)
                .OnDelete(DeleteBehavior.Restrict);

            // Visit has a Doctor (Staff)
            entity.HasOne(v => v.Doctor)
                .WithMany(s => s.DoctorVisits)
                .HasForeignKey(v => v.DoctorId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasQueryFilter(v => !v.IsDeleted);
        });

        // ========== Drug Configuration ==========
        modelBuilder.Entity<Drug>(entity =>
        {
            entity.HasIndex(d => d.DrugCode).IsUnique();
            entity.HasIndex(d => d.Name);
            entity.HasIndex(d => d.Category);

            entity.HasQueryFilter(d => !d.IsDeleted);
        });

        // ========== Prescription Configuration ==========
        modelBuilder.Entity<Prescription>(entity =>
        {
            entity.HasIndex(p => p.PrescriptionNumber).IsUnique();
            entity.HasIndex(p => p.PrescriptionDate);
            entity.HasIndex(p => p.Status);

            // Prescription belongs to Patient
            entity.HasOne(p => p.Patient)
                .WithMany(pt => pt.Prescriptions)
                .HasForeignKey(p => p.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Prescription written by Doctor (Staff)
            entity.HasOne(p => p.Doctor)
                .WithMany(s => s.Prescriptions)
                .HasForeignKey(p => p.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Prescription linked to Visit (optional)
            entity.HasOne(p => p.Visit)
                .WithMany(v => v.Prescriptions)
                .HasForeignKey(p => p.VisitId)
                .OnDelete(DeleteBehavior.SetNull);

            // Dispensed by pharmacist (optional)
            entity.HasOne(p => p.DispensedByStaff)
                .WithMany()
                .HasForeignKey(p => p.DispensedByStaffId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasQueryFilter(p => !p.IsDeleted);
        });

        // ========== PrescriptionItem Configuration ==========
        modelBuilder.Entity<PrescriptionItem>(entity =>
        {
            // Item belongs to Prescription
            entity.HasOne(pi => pi.Prescription)
                .WithMany(p => p.PrescriptionItems)
                .HasForeignKey(pi => pi.PrescriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Item references Drug
            entity.HasOne(pi => pi.Drug)
                .WithMany(d => d.PrescriptionItems)
                .HasForeignKey(pi => pi.DrugId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(pi => !pi.IsDeleted);
        });

        // ========== Dispensing Configuration ==========
        modelBuilder.Entity<Dispensing>(entity =>
        {
            entity.HasIndex(d => d.DispensingNumber).IsUnique();
            entity.HasIndex(d => d.DispensingDate);

            // Dispensing for a Drug
            entity.HasOne(d => d.Drug)
                .WithMany(dr => dr.Dispensings)
                .HasForeignKey(d => d.DrugId)
                .OnDelete(DeleteBehavior.Restrict);

            // Dispensed by Pharmacist
            entity.HasOne(d => d.Pharmacist)
                .WithMany(s => s.Dispensings)
                .HasForeignKey(d => d.PharmacistId)
                .OnDelete(DeleteBehavior.Restrict);

            // Optional link to PrescriptionItem
            entity.HasOne(d => d.PrescriptionItem)
                .WithMany()
                .HasForeignKey(d => d.PrescriptionItemId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasQueryFilter(d => !d.IsDeleted);
        });

        // ========== Billing Configuration ==========
        modelBuilder.Entity<Billing>(entity =>
        {
            entity.HasIndex(b => b.InvoiceNumber).IsUnique();
            entity.HasIndex(b => b.BillingDate);
            entity.HasIndex(b => b.PaymentStatus);

            // Billing for a Patient
            entity.HasOne(b => b.Patient)
                .WithMany(p => p.Billings)
                .HasForeignKey(b => b.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Billing linked to Prescription (optional)
            entity.HasOne(b => b.Prescription)
                .WithOne(p => p.Billing)
                .HasForeignKey<Billing>(b => b.PrescriptionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasQueryFilter(b => !b.IsDeleted);
        });

        // ========== AuditLog Configuration ==========
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(a => a.Timestamp);
            entity.HasIndex(a => a.UserId);
            entity.HasIndex(a => a.EntityName);
            entity.HasIndex(a => a.Action);
        });

        // Seed initial data
        SeedData(modelBuilder);
    }

    /// <summary>
    /// Seed initial data for clinics and admin user
    /// </summary>
    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed Clinics
        modelBuilder.Entity<Clinic>().HasData(
            new Clinic { ClinicId = 1, Name = "Cardiology", Description = "Heart and cardiovascular diseases", Location = "Building A, Floor 2", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Clinic { ClinicId = 2, Name = "Oncology", Description = "Cancer treatment and care", Location = "Building B, Floor 1", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Clinic { ClinicId = 3, Name = "Neurology", Description = "Brain and nervous system disorders", Location = "Building A, Floor 3", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Clinic { ClinicId = 4, Name = "Orthopedics", Description = "Bone and joint disorders", Location = "Building C, Floor 1", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Clinic { ClinicId = 5, Name = "Pediatrics", Description = "Child healthcare", Location = "Building A, Floor 1", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Clinic { ClinicId = 6, Name = "General Medicine", Description = "General healthcare services", Location = "Building A, Ground Floor", IsActive = true, CreatedAt = DateTime.UtcNow }
        );
    }

    /// <summary>
    /// Override SaveChanges to implement automatic auditing
    /// </summary>
    public override int SaveChanges()
    {
        BeforeSaveChanges();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        BeforeSaveChanges();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Handle audit logging and timestamps before saving
    /// </summary>
    private void BeforeSaveChanges()
    {
        var userName = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "System";
        
        // ToList() is critical - it materializes the collection before we modify it
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity && 
                   (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
            .ToList();

        foreach (var entry in entries)
        {
            var entity = (BaseEntity)entry.Entity;

            switch (entry.State)
            {
                case EntityState.Added:
                    entity.CreatedAt = DateTime.UtcNow;
                    entity.CreatedBy = userName;
                    AddAuditLog(AuditAction.Create, entry);
                    break;

                case EntityState.Modified:
                    entity.UpdatedAt = DateTime.UtcNow;
                    entity.UpdatedBy = userName;
                    AddAuditLog(AuditAction.Update, entry);
                    break;

                case EntityState.Deleted:
                    // Convert delete to soft delete
                    entry.State = EntityState.Modified;
                    entity.IsDeleted = true;
                    entity.UpdatedAt = DateTime.UtcNow;
                    entity.UpdatedBy = userName;
                    AddAuditLog(AuditAction.Delete, entry);
                    break;
            }
        }
    }

    /// <summary>
    /// Add audit log entry for an entity change
    /// </summary>
    private void AddAuditLog(AuditAction action, Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var entityName = entry.Entity.GetType().Name;
        var primaryKey = entry.Properties
            .FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString();

        var auditLog = new AuditLog
        {
            UserId = _httpContextAccessor?.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            UserName = _httpContextAccessor?.HttpContext?.User?.Identity?.Name,
            Action = action,
            EntityName = entityName,
            EntityId = primaryKey,
            Timestamp = DateTime.UtcNow,
            IpAddress = _httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
            UserAgent = _httpContextAccessor?.HttpContext?.Request?.Headers["User-Agent"].ToString()
        };

        if (action == AuditAction.Update)
        {
            var changedProps = entry.Properties
                .Where(p => p.IsModified)
                .Select(p => p.Metadata.Name)
                .ToList();
            
            auditLog.ChangedProperties = string.Join(", ", changedProps);
            
            var oldValues = entry.Properties
                .Where(p => p.IsModified)
                .ToDictionary(p => p.Metadata.Name, p => p.OriginalValue);
            
            var newValues = entry.Properties
                .Where(p => p.IsModified)
                .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);

            auditLog.OldValues = JsonSerializer.Serialize(oldValues);
            auditLog.NewValues = JsonSerializer.Serialize(newValues);
        }
        else if (action == AuditAction.Create)
        {
            var newValues = entry.Properties
                .Where(p => !p.Metadata.IsPrimaryKey())
                .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
            auditLog.NewValues = JsonSerializer.Serialize(newValues);
        }

        AuditLogs.Add(auditLog);
    }
}
