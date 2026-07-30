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
builder.Services.AddScoped<QuanLyBenhVien.Helpers.ModulePermissionFilter>();
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.AddService<QuanLyBenhVien.Helpers.ModulePermissionFilter>();
});
builder.Services.AddScoped<QuanLyBenhVien.Services.ExcelExportService>();

// Previously defaulted to Path.GetTempPath() ("%TEMP%\QuanLyBenhVien\..."):
// the OS temp folder is expected to be cleared at any time (disk cleanup,
// low-space maintenance...), and every time that happened here, ALL existing
// login cookies/antiforgery tokens instantly became undecryptable ("key was
// not found in the key ring"), forcing every logged-in user out with a
// cryptic error. Anchor to the stable project directory instead (same
// "walk up to find the .csproj" trick used for the Sqlite path below), so
// keys only disappear if someone deletes them on purpose.
var dataProtectionKeysPath = builder.Configuration["DataProtectionKeysPath"]
    ?? Path.Combine(FindProjectRootOrBaseDirectory(), ".dataprotection-keys");
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

// A relative Sqlite "Data Source=..." path resolves against the process's
// current directory, which differs between launch methods: `dotnet run`/
// `dotnet ef` use the project directory, while Visual Studio's F5 debugger
// runs the built .exe directly from bin/Debug/netX.0. Left unpinned, this
// silently created and diverged TWO separate hms.db files (one per launch
// method) with different real data - anchor relative Sqlite paths to the
// directory containing the .csproj (the actual project root) so every
// launch method reads/writes the exact same file. Falls back to the app's
// own base directory when no .csproj is present (published/Docker builds),
// which matches the previous, already-correct behavior there.
// Directory containing the .csproj (found by walking up from the running
// process's own base directory), used as a stable anchor for local dev state
// that must stay put regardless of how/where the app was launched from -
// falls back to the app's own base directory when no .csproj is present
// (published/Docker builds), matching the previous, already-correct behavior
// there.
static string FindProjectRootOrBaseDirectory()
{
    var projectDir = new DirectoryInfo(AppContext.BaseDirectory);
    while (projectDir != null && projectDir.GetFiles("*.csproj").Length == 0)
    {
        projectDir = projectDir.Parent;
    }
    return projectDir?.FullName ?? AppContext.BaseDirectory;
}

// A relative Sqlite "Data Source=..." path resolves against the process's
// current directory, which differs between launch methods: `dotnet run`/
// `dotnet ef` use the project directory, while Visual Studio's F5 debugger
// runs the built .exe directly from bin/Debug/netX.0. Left unpinned, this
// silently created and diverged TWO separate hms.db files (one per launch
// method) with different real data - anchor relative Sqlite paths to the
// project root so every launch method reads/writes the exact same file.
static string ResolveSqliteConnectionString(string? connectionString)
{
    const string prefix = "Data Source=";
    if (string.IsNullOrEmpty(connectionString) ||
        !connectionString.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        return connectionString ?? string.Empty;

    var dataSource = connectionString.Substring(prefix.Length).Split(';')[0].Trim();
    if (string.IsNullOrEmpty(dataSource) || Path.IsPathRooted(dataSource))
        return connectionString;

    return $"Data Source={Path.Combine(FindProjectRootOrBaseDirectory(), dataSource)}";
}

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
        options.UseSqlite(ResolveSqliteConnectionString(connectionString));
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
