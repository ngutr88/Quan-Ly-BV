using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using QuanLyBenhVien.Data;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();
if (string.IsNullOrEmpty(builder.Environment.WebRootPath) || !Directory.Exists(builder.Environment.WebRootPath))
{
    var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
    while (currentDir != null)
    {
        var candidate = Path.Combine(currentDir.FullName, "wwwroot");
        if (Directory.Exists(candidate))
        {
            builder.Environment.WebRootPath = candidate;
            break;
        }
        currentDir = currentDir.Parent;
    }
}

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddControllersWithViews();

var dataProtectionKeysPath = builder.Configuration["DataProtectionKeysPath"]
    ?? Path.Combine(Path.GetTempPath(), "QuanLyBenhVien", "data-protection-keys");
Directory.CreateDirectory(dataProtectionKeysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("QuanLyBenhVien");

// Render terminates TLS at its proxy and forwards requests to the container over HTTP.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Register Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    var provider = builder.Configuration["Database:Provider"]
        ?? (connectionString?.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase) == true
            ? "Sqlite"
            : "SqlServer");

    if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        options.UseSqlServer(connectionString);
    else if (provider.Equals("MySql", StringComparison.OrdinalIgnoreCase))
        options.UseMySQL(connectionString);
    else
        options.UseSqlite(connectionString);
    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

// Register Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
    });

// Register Session Services
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

app.UseForwardedHeaders();

// Seed Database on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        // The legacy migrations in this project were generated for SQLite
        // (they contain SQLite-specific column annotations). For SQL Server
        // and MySQL, build the schema from the current model instead of
        // replaying those migrations against an existing database.
        var providerName = context.Database.ProviderName ?? string.Empty;
        var usesModelCreation = context.Database.IsSqlServer() ||
                                providerName.Contains("MySql", StringComparison.OrdinalIgnoreCase);
        if (usesModelCreation)
        {
            context.Database.EnsureCreated();
        }
        else
        {
            // Keep the runtime database schema in sync before the seeder or any
            // controller queries newly introduced tables (for example patient
            // documents on /Patient/Record).
            context.Database.Migrate();
        }

        // The patient-document entity was added to the model in an earlier
        // SQLite migration that only altered existing columns. Repair SQLite
        // databases created from that migration where the table is missing.
        if (context.Database.IsSqlite())
        {
            context.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS TaiLieuBenhNhan (
                Id INTEGER NOT NULL CONSTRAINT PK_TaiLieuBenhNhan PRIMARY KEY AUTOINCREMENT,
                BenhNhanId INTEGER NOT NULL,
                TenTaiLieu TEXT NOT NULL,
                LoaiTaiLieu TEXT NOT NULL,
                TenLuuTru TEXT NOT NULL,
                ContentType TEXT NOT NULL,
                KichThuoc INTEGER NOT NULL,
                GhiChu TEXT NULL,
                NgayTaiLen TEXT NOT NULL,
                CONSTRAINT FK_TaiLieuBenhNhan_BenhNhan_BenhNhanId
                    FOREIGN KEY (BenhNhanId) REFERENCES BenhNhan (Id) ON DELETE CASCADE
            );");
        }
        DbSeeder.Seed(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Map Area Routing (must be placed before default route)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
