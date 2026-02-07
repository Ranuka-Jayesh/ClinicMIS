using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ClinicMIS.Data;
using ClinicMIS.Models.Entities;
using ClinicMIS.Services;

var builder = WebApplication.CreateBuilder(args);

// ========== DATABASE CONFIGURATION ==========
builder.Services.AddDbContext<ClinicDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        }));

// ========== IDENTITY CONFIGURATION (Authentication & Authorization) ==========
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 4;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ClinicDbContext>()
.AddDefaultTokenProviders();

// ========== COOKIE CONFIGURATION ==========
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// ========== AUTHORIZATION POLICIES ==========
builder.Services.AddAuthorization(options =>
{
    // Role-based policies
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("DoctorOnly", policy => policy.RequireRole("Doctor", "Admin"));
    options.AddPolicy("PharmacistOnly", policy => policy.RequireRole("Pharmacist", "Admin"));
    options.AddPolicy("NurseOnly", policy => policy.RequireRole("Nurse", "Doctor", "Admin"));
    options.AddPolicy("ReceptionistOnly", policy => policy.RequireRole("Receptionist", "Admin"));
    
    // Combined policies for different modules
    options.AddPolicy("CanViewPatients", policy => 
        policy.RequireRole("Admin", "Doctor", "Nurse", "Receptionist"));
    options.AddPolicy("CanEditPatients", policy => 
        policy.RequireRole("Admin", "Doctor", "Receptionist"));
    options.AddPolicy("CanPrescribe", policy => 
        policy.RequireRole("Admin", "Doctor"));
    options.AddPolicy("CanDispense", policy => 
        policy.RequireRole("Admin", "Pharmacist"));
    options.AddPolicy("CanManageStaff", policy => 
        policy.RequireRole("Admin"));
    options.AddPolicy("CanViewReports", policy => 
        policy.RequireRole("Admin", "Doctor"));
    options.AddPolicy("CanViewBillings", policy => 
        policy.RequireRole("Admin", "Pharmacist", "Receptionist"));
    options.AddPolicy("CanEditBillings", policy => 
        policy.RequireRole("Admin", "Pharmacist", "Receptionist"));
});

// ========== REGISTER APPLICATION SERVICES ==========
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IPrescriptionService, PrescriptionService>();
builder.Services.AddScoped<IPharmacyService, PharmacyService>();
builder.Services.AddScoped<IReportService, ReportService>();

// HttpContextAccessor for audit logging
builder.Services.AddHttpContextAccessor();

// ========== MVC CONFIGURATION ==========
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// ========== SECURITY HEADERS & ANTI-FORGERY ==========
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

var app = builder.Build();

// ========== EXCEPTION HANDLING ==========
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// ========== SECURITY MIDDLEWARE ==========
app.UseHttpsRedirection();
app.UseStaticFiles();

// Add security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

app.UseRouting();

// Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

// ========== ROUTE CONFIGURATION ==========
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// ========== DATABASE INITIALIZATION ==========
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ClinicDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        
        // Apply pending migrations
        await context.Database.MigrateAsync();
        
        // Seed roles and admin user
        await SeedRolesAndAdminAsync(roleManager, userManager, context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

app.Run();

// ========== SEEDING HELPER METHOD ==========
static async Task SeedRolesAndAdminAsync(
    RoleManager<IdentityRole> roleManager,
    UserManager<ApplicationUser> userManager,
    ClinicDbContext context)
{
    // Create roles if they don't exist
    string[] roles = { "Admin", "Doctor", "Nurse", "Pharmacist", "Receptionist" };
    
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // Create default admin user if it doesn't exist
    var adminEmail = "admin@clinic.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "System",
            LastName = "Administrator",
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await userManager.CreateAsync(adminUser, "Admin@123");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
            
            // Create corresponding staff record
            var adminStaff = new Staff
            {
                EmployeeNumber = "EMP-0001",
                FirstName = "System",
                LastName = "Administrator",
                Role = StaffRole.Admin,
                Email = adminEmail,
                PhoneNumber = "0000000000",
                HireDate = DateTime.Today,
                IsActive = true,
                UserId = adminUser.Id
            };
            
            context.Staff.Add(adminStaff);
            await context.SaveChangesAsync();
        }
    }
}
