using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Hubs;
using QuanLyBenhVien.Services;

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
builder.Services.AddScoped<QuanLyBenhVien.Helpers.ForcePasswordChangeFilter>();
builder.Services.AddScoped<QuanLyBenhVien.Helpers.ForceTwoFactorSetupFilter>();
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.AddService<QuanLyBenhVien.Helpers.ModulePermissionFilter>();
    options.Filters.AddService<QuanLyBenhVien.Helpers.ForcePasswordChangeFilter>();
    // Đăng ký SAU ForcePasswordChangeFilter - đổi mật khẩu tạm luôn được xử
    // lý trước khi ép bật 2FA.
    options.Filters.AddService<QuanLyBenhVien.Helpers.ForceTwoFactorSetupFilter>();
});
builder.Services.AddScoped<QuanLyBenhVien.Services.ExcelExportService>();
builder.Services.AddScoped<DoctorDashboardNotifier>();
builder.Services.AddScoped<ConsultationChatNotifier>();
builder.Services.AddScoped<ConsultationChatService>();
builder.Services.AddSignalR();
builder.Services.AddSingleton<QuanLyBenhVien.Services.HospitalSettingsProvider>();
builder.Services.AddScoped<QuanLyBenhVien.Services.AppointmentSlotService>();
builder.Services.AddScoped<QuanLyBenhVien.Services.IEmailSender, QuanLyBenhVien.Services.MockEmailSender>();
builder.Services.AddScoped<QuanLyBenhVien.Services.ISmsSender, QuanLyBenhVien.Services.MockSmsSender>();
builder.Services.AddScoped<QuanLyBenhVien.Services.IPrescriptionSafetyChecker, QuanLyBenhVien.Services.PrescriptionSafetyChecker>();
builder.Services.AddScoped<QuanLyBenhVien.Services.MedicineStockAllocator>();
builder.Services.AddScoped<QuanLyBenhVien.Services.LeaveRequestService>();

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
        options.Events.OnValidatePrincipal = QuanLyBenhVien.Helpers.SecurityStampCookieValidator.ValidateAsync;
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

            // EnsureCreated() only builds schema when the database doesn't exist
            // yet - it is a no-op against the real, already-existing SQL Server
            // database, so the new password-reset table/columns added below
            // would otherwise never reach it. Idempotent (checked with
            // IF NOT EXISTS/COL_LENGTH), safe to also run against a freshly
            // created database where EnsureCreated already added them.
            if (context.Database.IsSqlServer())
            {
                context.Database.ExecuteSqlRaw(@"
                IF COL_LENGTH('NguoiDung','SecurityStamp') IS NULL
                    ALTER TABLE NguoiDung ADD SecurityStamp NVARCHAR(64) NOT NULL DEFAULT '';
                IF COL_LENGTH('NguoiDung','PhaiDoiMatKhau') IS NULL
                    ALTER TABLE NguoiDung ADD PhaiDoiMatKhau BIT NOT NULL DEFAULT 0;
                IF COL_LENGTH('NguoiDung','MatKhauTamHetHan') IS NULL
                    ALTER TABLE NguoiDung ADD MatKhauTamHetHan DATETIME2 NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'YeuCauKhoiPhucMatKhau')
                BEGIN
                    CREATE TABLE YeuCauKhoiPhucMatKhau (
                        Id INT NOT NULL CONSTRAINT PK_YeuCauKhoiPhucMatKhau PRIMARY KEY IDENTITY,
                        NguoiDungId INT NULL,
                        IpAddress NVARCHAR(50) NOT NULL,
                        KhoiTaoBoi NVARCHAR(20) NOT NULL,
                        Kenh NVARCHAR(10) NULL,
                        MaXacNhanHash NVARCHAR(200) NULL,
                        ThoiHanMa DATETIME2 NULL,
                        SoLanNhapSai INT NOT NULL DEFAULT 0,
                        MaDaDung BIT NOT NULL DEFAULT 0,
                        SoLanGuiLai INT NOT NULL DEFAULT 0,
                        LanGuiGanNhatLuc DATETIME2 NULL,
                        ResetTokenHash NVARCHAR(200) NULL,
                        ThoiHanResetToken DATETIME2 NULL,
                        ResetTokenDaDung BIT NOT NULL DEFAULT 0,
                        NgayTao DATETIME2 NOT NULL DEFAULT GETDATE(),
                        CONSTRAINT FK_YeuCauKhoiPhucMatKhau_NguoiDung_NguoiDungId
                            FOREIGN KEY (NguoiDungId) REFERENCES NguoiDung (Id),
                        CONSTRAINT CK_YeuCauKhoiPhucMatKhau_KhoiTaoBoi CHECK (KhoiTaoBoi IN ('TuPhucVu','Admin')),
                        CONSTRAINT CK_YeuCauKhoiPhucMatKhau_Kenh CHECK (Kenh IS NULL OR Kenh IN ('Email','Sdt'))
                    );
                    CREATE INDEX IX_YeuCauKhoiPhucMatKhau_NguoiDungId_NgayTao ON YeuCauKhoiPhucMatKhau (NguoiDungId, NgayTao);
                    CREATE INDEX IX_YeuCauKhoiPhucMatKhau_IpAddress_NgayTao ON YeuCauKhoiPhucMatKhau (IpAddress, NgayTao);
                    CREATE INDEX IX_YeuCauKhoiPhucMatKhau_ResetTokenHash ON YeuCauKhoiPhucMatKhau (ResetTokenHash);
                END");

                // Kê đơn & Y lệnh (Sprint 1) - cùng lý do như trên: EnsureCreated()
                // không tự thêm bảng/cột mới vào DB SQL Server đã tồn tại.
                //
                // Tách thành nhiều batch ExecuteSqlRaw riêng (không gộp chung 1
                // chuỗi): SQL Server biên dịch toàn bộ 1 batch trước khi chạy, nên
                // 1 câu ALTER TABLE...ADD COLUMN và 1 câu khác THAM CHIẾU đúng cột
                // vừa thêm đó (CHECK constraint, UPDATE, ALTER COLUMN, ADD FK)
                // không được phép nằm chung 1 batch - sẽ báo "Invalid column
                // name" dù cột đã tồn tại, vì tên cột mới chưa "nhìn thấy được"
                // cho tới khi batch trước đó hoàn tất.
                context.Database.ExecuteSqlRaw(@"
                IF COL_LENGTH('Thuoc','CoBaoHiemYTe') IS NULL
                    ALTER TABLE Thuoc ADD CoBaoHiemYTe BIT NOT NULL DEFAULT 0;
                IF COL_LENGTH('Thuoc','LieuToiDaMoiNgay') IS NULL
                    ALTER TABLE Thuoc ADD LieuToiDaMoiNgay INT NULL;
                IF COL_LENGTH('Thuoc','LieuToiDaMoiNgayTheoKg') IS NULL
                    ALTER TABLE Thuoc ADD LieuToiDaMoiNgayTheoKg DECIMAL(10, 4) NULL;
                IF COL_LENGTH('Thuoc','QuyCachSoLuong') IS NULL
                    ALTER TABLE Thuoc ADD QuyCachSoLuong INT NULL;

                IF COL_LENGTH('ChiTietDonThuoc','LieuMoiLan') IS NULL
                    ALTER TABLE ChiTietDonThuoc ADD LieuMoiLan DECIMAL(10, 2) NULL;
                IF COL_LENGTH('ChiTietDonThuoc','SoLanMoiNgay') IS NULL
                    ALTER TABLE ChiTietDonThuoc ADD SoLanMoiNgay INT NULL;
                IF COL_LENGTH('ChiTietDonThuoc','DuongDung') IS NULL
                    ALTER TABLE ChiTietDonThuoc ADD DuongDung NVARCHAR(50) NULL;
                IF COL_LENGTH('ChiTietDonThuoc','ThoiDiemDung') IS NULL
                    ALTER TABLE ChiTietDonThuoc ADD ThoiDiemDung NVARCHAR(50) NULL;
                IF COL_LENGTH('ChiTietDonThuoc','SoNgayDung') IS NULL
                    ALTER TABLE ChiTietDonThuoc ADD SoNgayDung INT NULL;
                IF COL_LENGTH('ChiTietDonThuoc','HuongDanSuDung') IS NULL
                    ALTER TABLE ChiTietDonThuoc ADD HuongDanSuDung NVARCHAR(300) NULL;

                IF COL_LENGTH('PhieuKham','NgayHenTaiKham') IS NULL
                    ALTER TABLE PhieuKham ADD NgayHenTaiKham DATETIME2 NULL;

                IF COL_LENGTH('BenhNhan','MucDoDiUng') IS NULL
                    ALTER TABLE BenhNhan ADD MucDoDiUng NVARCHAR(20) NULL;

                IF COL_LENGTH('DonThuoc','TrangThai') IS NULL
                    ALTER TABLE DonThuoc ADD TrangThai NVARCHAR(20) NOT NULL DEFAULT 'ChoCapPhat';
                IF COL_LENGTH('DonThuoc','LyDoHuy') IS NULL
                    ALTER TABLE DonThuoc ADD LyDoHuy NVARCHAR(500) NULL;
                IF COL_LENGTH('DonThuoc','BacSiKeId') IS NULL
                    ALTER TABLE DonThuoc ADD BacSiKeId INT NULL;");

                // Batch riêng: các câu lệnh tham chiếu tên cột vừa thêm ở trên.
                if (!context.Database.SqlQueryRaw<int>(
                        "SELECT CAST(1 AS INT) AS Value WHERE EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_BenhNhan_MucDoDiUng')")
                    .AsEnumerable().Any())
                {
                    context.Database.ExecuteSqlRaw(@"
                    ALTER TABLE BenhNhan ADD CONSTRAINT CK_BenhNhan_MucDoDiUng
                        CHECK (MucDoDiUng IS NULL OR MucDoDiUng IN ('Nhe','TrungBinh','NghiemTrong'));");
                }

                if (!context.Database.SqlQueryRaw<int>(
                        "SELECT CAST(1 AS INT) AS Value WHERE EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_DonThuoc_TrangThai')")
                    .AsEnumerable().Any())
                {
                    context.Database.ExecuteSqlRaw(@"
                    ALTER TABLE DonThuoc ADD CONSTRAINT CK_DonThuoc_TrangThai
                        CHECK (TrangThai IN ('Nhap','ChoCapPhat','DaCapPhat','DuocPhanHoi','DaHuy'));");
                }

                if (!context.Database.SqlQueryRaw<int>(
                        "SELECT CAST(1 AS INT) AS Value WHERE EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DonThuoc_BacSi_BacSiKeId')")
                    .AsEnumerable().Any())
                {
                    context.Database.ExecuteSqlRaw(@"
                    UPDATE dt SET dt.BacSiKeId = lk.BacSiId
                        FROM DonThuoc dt
                        INNER JOIN PhieuKham pk ON pk.Id = dt.PhieuKhamId
                        INNER JOIN LichKham lk ON lk.Id = pk.LichKhamId
                        WHERE dt.BacSiKeId IS NULL;
                    UPDATE DonThuoc SET BacSiKeId = (SELECT TOP 1 Id FROM BacSi ORDER BY Id) WHERE BacSiKeId IS NULL;
                    ALTER TABLE DonThuoc ALTER COLUMN BacSiKeId INT NOT NULL;
                    ALTER TABLE DonThuoc ADD CONSTRAINT FK_DonThuoc_BacSi_BacSiKeId FOREIGN KEY (BacSiKeId) REFERENCES BacSi (Id);");
                }

                context.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TuongTacThuoc')
                BEGIN
                    CREATE TABLE TuongTacThuoc (
                        Id INT NOT NULL CONSTRAINT PK_TuongTacThuoc PRIMARY KEY IDENTITY,
                        HoatChatA NVARCHAR(150) NOT NULL,
                        HoatChatB NVARCHAR(150) NOT NULL,
                        MucDoTuongTac NVARCHAR(20) NOT NULL,
                        MoTa NVARCHAR(500) NOT NULL,
                        NguonNhap NVARCHAR(30) NOT NULL DEFAULT 'SeedDemo',
                        LaDuLieuMinhHoa BIT NOT NULL DEFAULT 1,
                        NgayTao DATETIME2 NOT NULL DEFAULT GETDATE(),
                        CONSTRAINT CK_TuongTacThuoc_MucDo CHECK (MucDoTuongTac IN ('ChongChiDinh','NghiemTrong','TrungBinh','Nhe'))
                    );
                    CREATE INDEX IX_TuongTacThuoc_HoatChatA_HoatChatB ON TuongTacThuoc (HoatChatA, HoatChatB);
                END

                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'NhomHoatChatCheoPhanUng')
                BEGIN
                    CREATE TABLE NhomHoatChatCheoPhanUng (
                        Id INT NOT NULL CONSTRAINT PK_NhomHoatChatCheoPhanUng PRIMARY KEY IDENTITY,
                        TenNhom NVARCHAR(150) NOT NULL,
                        MoTa NVARCHAR(500) NULL,
                        LaDuLieuMinhHoa BIT NOT NULL DEFAULT 1
                    );
                END

                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ThanhVienNhomHoatChat')
                BEGIN
                    CREATE TABLE ThanhVienNhomHoatChat (
                        Id INT NOT NULL CONSTRAINT PK_ThanhVienNhomHoatChat PRIMARY KEY IDENTITY,
                        NhomId INT NOT NULL,
                        HoatChat NVARCHAR(150) NOT NULL,
                        CONSTRAINT FK_ThanhVienNhomHoatChat_NhomHoatChatCheoPhanUng_NhomId
                            FOREIGN KEY (NhomId) REFERENCES NhomHoatChatCheoPhanUng (Id)
                    );
                    CREATE INDEX IX_ThanhVienNhomHoatChat_HoatChat ON ThanhVienNhomHoatChat (HoatChat);
                END

                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PhanBoLoThuocDonThuoc')
                BEGIN
                    CREATE TABLE PhanBoLoThuocDonThuoc (
                        Id INT NOT NULL CONSTRAINT PK_PhanBoLoThuocDonThuoc PRIMARY KEY IDENTITY,
                        ChiTietDonThuocId INT NOT NULL,
                        LoThuocId INT NOT NULL,
                        SoLuongLay INT NOT NULL,
                        DaHoanTra BIT NOT NULL DEFAULT 0,
                        NgayPhanBo DATETIME2 NOT NULL DEFAULT GETDATE(),
                        CONSTRAINT FK_PhanBoLoThuocDonThuoc_ChiTietDonThuoc_ChiTietDonThuocId
                            FOREIGN KEY (ChiTietDonThuocId) REFERENCES ChiTietDonThuoc (Id),
                        CONSTRAINT FK_PhanBoLoThuocDonThuoc_LoThuoc_LoThuocId
                            FOREIGN KEY (LoThuocId) REFERENCES LoThuoc (Id)
                    );
                    CREATE INDEX IX_PhanBoLoThuocDonThuoc_ChiTietDonThuocId ON PhanBoLoThuocDonThuoc (ChiTietDonThuocId);
                END");

                // Cận lâm sàng (Giai đoạn 1) - cùng lý do như các khối trên.
                // Batch 1: chỉ ALTER TABLE...ADD COLUMN, tự chứa, không câu nào
                // khác trong CÙNG batch tham chiếu tên cột vừa thêm.
                context.Database.ExecuteSqlRaw(@"
                IF COL_LENGTH('NguoiDung','DuocXuLyCLS') IS NULL
                    ALTER TABLE NguoiDung ADD DuocXuLyCLS BIT NOT NULL DEFAULT 0;
                IF COL_LENGTH('PhieuKham','GhiChuCLSCuaBacSi') IS NULL
                    ALTER TABLE PhieuKham ADD GhiChuCLSCuaBacSi NVARCHAR(1000) NULL;");

                // Batch 2: toàn bộ CREATE TABLE mới, theo đúng thứ tự phụ thuộc.
                // Đây là CREATE TABLE (không phải ALTER TABLE ADD COLUMN), nên
                // một bảng tham chiếu bảng khác vừa được tạo trước đó TRONG
                // CÙNG batch là hợp lệ (đã áp dụng y hệt cho
                // TuongTacThuoc/NhomHoatChatCheoPhanUng/ThanhVienNhomHoatChat ở trên).
                context.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'DichVuCLS')
                BEGIN
                    CREATE TABLE DichVuCLS (
                        Id INT NOT NULL CONSTRAINT PK_DichVuCLS PRIMARY KEY IDENTITY,
                        MaDichVu NVARCHAR(30) NOT NULL,
                        TenDichVu NVARCHAR(200) NOT NULL,
                        NhomCLS NVARCHAR(30) NOT NULL,
                        NoiThucHien NVARCHAR(150) NULL,
                        Gia DECIMAL(12, 2) NOT NULL,
                        DangHoatDong BIT NOT NULL DEFAULT 1,
                        GhiChu NVARCHAR(500) NULL,
                        CONSTRAINT CK_DichVuCLS_NhomCLS CHECK (NhomCLS IN ('HuyetHoc','SinhHoa','ViSinh','CDHA','ThamDoChucNang','GiaiPhauBenh')),
                        CONSTRAINT CK_DichVuCLS_Gia CHECK (Gia >= 0)
                    );
                    CREATE UNIQUE INDEX IX_DichVuCLS_MaDichVu ON DichVuCLS (MaDichVu);
                END

                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BoChiDinhCLS')
                BEGIN
                    CREATE TABLE BoChiDinhCLS (
                        Id INT NOT NULL CONSTRAINT PK_BoChiDinhCLS PRIMARY KEY IDENTITY,
                        TenBo NVARCHAR(150) NOT NULL,
                        MoTa NVARCHAR(500) NULL,
                        DangHoatDong BIT NOT NULL DEFAULT 1
                    );
                END

                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ChiTietBoChiDinhCLS')
                BEGIN
                    CREATE TABLE ChiTietBoChiDinhCLS (
                        Id INT NOT NULL CONSTRAINT PK_ChiTietBoChiDinhCLS PRIMARY KEY IDENTITY,
                        BoChiDinhCLSId INT NOT NULL,
                        DichVuCLSId INT NOT NULL,
                        CONSTRAINT FK_ChiTietBoChiDinhCLS_BoChiDinhCLS_BoChiDinhCLSId
                            FOREIGN KEY (BoChiDinhCLSId) REFERENCES BoChiDinhCLS (Id) ON DELETE CASCADE,
                        CONSTRAINT FK_ChiTietBoChiDinhCLS_DichVuCLS_DichVuCLSId
                            FOREIGN KEY (DichVuCLSId) REFERENCES DichVuCLS (Id)
                    );
                    CREATE UNIQUE INDEX IX_ChiTietBoChiDinhCLS_BoChiDinhCLSId_DichVuCLSId ON ChiTietBoChiDinhCLS (BoChiDinhCLSId, DichVuCLSId);
                    CREATE INDEX IX_ChiTietBoChiDinhCLS_DichVuCLSId ON ChiTietBoChiDinhCLS (DichVuCLSId);
                END

                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PhieuChiDinhCLS')
                BEGIN
                    CREATE TABLE PhieuChiDinhCLS (
                        Id INT NOT NULL CONSTRAINT PK_PhieuChiDinhCLS PRIMARY KEY IDENTITY,
                        MaPhieu NVARCHAR(30) NOT NULL,
                        PhieuKhamId INT NOT NULL,
                        BenhNhanId INT NOT NULL,
                        BacSiChiDinhId INT NOT NULL,
                        NgayChiDinh DATETIME2 NOT NULL DEFAULT GETDATE(),
                        BoChiDinhCLSId INT NULL,
                        GhiChuChiDinh NVARCHAR(500) NULL,
                        CONSTRAINT FK_PhieuChiDinhCLS_PhieuKham_PhieuKhamId FOREIGN KEY (PhieuKhamId) REFERENCES PhieuKham (Id),
                        CONSTRAINT FK_PhieuChiDinhCLS_BenhNhan_BenhNhanId FOREIGN KEY (BenhNhanId) REFERENCES BenhNhan (Id),
                        CONSTRAINT FK_PhieuChiDinhCLS_BacSi_BacSiChiDinhId FOREIGN KEY (BacSiChiDinhId) REFERENCES BacSi (Id),
                        CONSTRAINT FK_PhieuChiDinhCLS_BoChiDinhCLS_BoChiDinhCLSId FOREIGN KEY (BoChiDinhCLSId) REFERENCES BoChiDinhCLS (Id)
                    );
                    CREATE UNIQUE INDEX IX_PhieuChiDinhCLS_MaPhieu ON PhieuChiDinhCLS (MaPhieu);
                    CREATE INDEX IX_PhieuChiDinhCLS_PhieuKhamId ON PhieuChiDinhCLS (PhieuKhamId);
                    CREATE INDEX IX_PhieuChiDinhCLS_BenhNhanId ON PhieuChiDinhCLS (BenhNhanId);
                    CREATE INDEX IX_PhieuChiDinhCLS_BacSiChiDinhId ON PhieuChiDinhCLS (BacSiChiDinhId);
                    CREATE INDEX IX_PhieuChiDinhCLS_BoChiDinhCLSId ON PhieuChiDinhCLS (BoChiDinhCLSId);
                END

                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ChiTietPhieuChiDinhCLS')
                BEGIN
                    CREATE TABLE ChiTietPhieuChiDinhCLS (
                        Id INT NOT NULL CONSTRAINT PK_ChiTietPhieuChiDinhCLS PRIMARY KEY IDENTITY,
                        PhieuChiDinhCLSId INT NOT NULL,
                        DichVuCLSId INT NOT NULL,
                        TrangThai NVARCHAR(20) NOT NULL DEFAULT 'ChoThucHien',
                        LyDoHuy NVARCHAR(500) NULL,
                        NgayNhanThucHien DATETIME2 NULL,
                        NguoiNhanId INT NULL,
                        CONSTRAINT CK_ChiTietPhieuChiDinhCLS_TrangThai CHECK (TrangThai IN ('ChoThucHien','DangThucHien','DaCoKetQua','DaHuy')),
                        CONSTRAINT FK_ChiTietPhieuChiDinhCLS_PhieuChiDinhCLS_PhieuChiDinhCLSId
                            FOREIGN KEY (PhieuChiDinhCLSId) REFERENCES PhieuChiDinhCLS (Id) ON DELETE CASCADE,
                        CONSTRAINT FK_ChiTietPhieuChiDinhCLS_DichVuCLS_DichVuCLSId FOREIGN KEY (DichVuCLSId) REFERENCES DichVuCLS (Id),
                        CONSTRAINT FK_ChiTietPhieuChiDinhCLS_NguoiDung_NguoiNhanId FOREIGN KEY (NguoiNhanId) REFERENCES NguoiDung (Id)
                    );
                    CREATE INDEX IX_ChiTietPhieuChiDinhCLS_PhieuChiDinhCLSId ON ChiTietPhieuChiDinhCLS (PhieuChiDinhCLSId);
                    CREATE INDEX IX_ChiTietPhieuChiDinhCLS_DichVuCLSId ON ChiTietPhieuChiDinhCLS (DichVuCLSId);
                    CREATE INDEX IX_ChiTietPhieuChiDinhCLS_NguoiNhanId ON ChiTietPhieuChiDinhCLS (NguoiNhanId);
                END

                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'KetQuaCLS')
                BEGIN
                    CREATE TABLE KetQuaCLS (
                        Id INT NOT NULL CONSTRAINT PK_KetQuaCLS PRIMARY KEY IDENTITY,
                        ChiTietPhieuChiDinhCLSId INT NOT NULL,
                        KetLuan NVARCHAR(1000) NOT NULL,
                        CoBatThuong BIT NOT NULL DEFAULT 0,
                        NguoiThucHienId INT NOT NULL,
                        NguoiDuyetTen NVARCHAR(150) NULL,
                        NgayTra DATETIME2 NOT NULL DEFAULT GETDATE(),
                        DaXem BIT NOT NULL DEFAULT 0,
                        NgayXem DATETIME2 NULL,
                        CONSTRAINT FK_KetQuaCLS_ChiTietPhieuChiDinhCLS_ChiTietPhieuChiDinhCLSId
                            FOREIGN KEY (ChiTietPhieuChiDinhCLSId) REFERENCES ChiTietPhieuChiDinhCLS (Id),
                        CONSTRAINT FK_KetQuaCLS_NguoiDung_NguoiThucHienId FOREIGN KEY (NguoiThucHienId) REFERENCES NguoiDung (Id)
                    );
                    CREATE UNIQUE INDEX IX_KetQuaCLS_ChiTietPhieuChiDinhCLSId ON KetQuaCLS (ChiTietPhieuChiDinhCLSId);
                    CREATE INDEX IX_KetQuaCLS_NguoiThucHienId ON KetQuaCLS (NguoiThucHienId);
                END

                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FileKetQuaCLS')
                BEGIN
                    CREATE TABLE FileKetQuaCLS (
                        Id INT NOT NULL CONSTRAINT PK_FileKetQuaCLS PRIMARY KEY IDENTITY,
                        KetQuaCLSId INT NOT NULL,
                        TenGoc NVARCHAR(260) NOT NULL,
                        TenLuuTru NVARCHAR(260) NOT NULL,
                        ContentType NVARCHAR(100) NOT NULL,
                        KichThuoc BIGINT NOT NULL,
                        ThuTu INT NOT NULL DEFAULT 0,
                        NgayTaiLen DATETIME2 NOT NULL DEFAULT GETDATE(),
                        CONSTRAINT FK_FileKetQuaCLS_KetQuaCLS_KetQuaCLSId
                            FOREIGN KEY (KetQuaCLSId) REFERENCES KetQuaCLS (Id) ON DELETE CASCADE
                    );
                    CREATE INDEX IX_FileKetQuaCLS_KetQuaCLSId ON FileKetQuaCLS (KetQuaCLSId);
                END");

                // Tin nhắn tư vấn (Giai đoạn 1) - cùng lý do như các khối trên.
                context.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HoiThoaiTuVan')
                BEGIN
                    CREATE TABLE HoiThoaiTuVan (
                        Id INT NOT NULL CONSTRAINT PK_HoiThoaiTuVan PRIMARY KEY IDENTITY,
                        BenhNhanId INT NOT NULL,
                        BacSiId INT NOT NULL,
                        TrangThai NVARCHAR(20) NOT NULL DEFAULT 'Moi',
                        NgayTao DATETIME2 NOT NULL DEFAULT GETDATE(),
                        ThoiGianChoTraLoiTu DATETIME2 NULL,
                        ThoiGianTinNhanCuoi DATETIME2 NULL,
                        GhiChuKetLuan NVARCHAR(1000) NULL,
                        NgayDong DATETIME2 NULL,
                        DaGuiAutoReplyNgoaiGio BIT NOT NULL DEFAULT 0,
                        CONSTRAINT CK_HoiThoaiTuVan_TrangThai CHECK (TrangThai IN ('Moi','DangXuLy','DaTraLoi','DaDong')),
                        CONSTRAINT FK_HoiThoaiTuVan_BenhNhan_BenhNhanId FOREIGN KEY (BenhNhanId) REFERENCES BenhNhan (Id),
                        CONSTRAINT FK_HoiThoaiTuVan_BacSi_BacSiId FOREIGN KEY (BacSiId) REFERENCES BacSi (Id)
                    );
                    CREATE UNIQUE INDEX IX_HoiThoaiTuVan_BenhNhanId_BacSiId ON HoiThoaiTuVan (BenhNhanId, BacSiId);
                    CREATE INDEX IX_HoiThoaiTuVan_BacSiId ON HoiThoaiTuVan (BacSiId);
                    CREATE INDEX IX_HoiThoaiTuVan_TrangThai ON HoiThoaiTuVan (TrangThai);
                END

                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TinNhanTuVan')
                BEGIN
                    CREATE TABLE TinNhanTuVan (
                        Id INT NOT NULL CONSTRAINT PK_TinNhanTuVan PRIMARY KEY IDENTITY,
                        HoiThoaiId INT NOT NULL,
                        NguoiGuiId INT NULL,
                        VaiTroNguoiGui NVARCHAR(20) NOT NULL,
                        Loai NVARCHAR(20) NOT NULL DEFAULT 'Text',
                        NoiDung NVARCHAR(2000) NULL,
                        ThoiGianGui DATETIME2 NOT NULL DEFAULT GETDATE(),
                        DaXemBoiNguoiNhan BIT NOT NULL DEFAULT 0,
                        NgayXem DATETIME2 NULL,
                        CONSTRAINT CK_TinNhanTuVan_VaiTroNguoiGui CHECK (VaiTroNguoiGui IN ('Doctor','Patient','HeThong')),
                        CONSTRAINT CK_TinNhanTuVan_Loai CHECK (Loai IN ('Text','MoiDatLich','TuDongPhanHoi')),
                        CONSTRAINT FK_TinNhanTuVan_HoiThoaiTuVan_HoiThoaiId FOREIGN KEY (HoiThoaiId) REFERENCES HoiThoaiTuVan (Id) ON DELETE CASCADE,
                        CONSTRAINT FK_TinNhanTuVan_NguoiDung_NguoiGuiId FOREIGN KEY (NguoiGuiId) REFERENCES NguoiDung (Id)
                    );
                    CREATE INDEX IX_TinNhanTuVan_HoiThoaiId_ThoiGianGui ON TinNhanTuVan (HoiThoaiId, ThoiGianGui);
                    CREATE INDEX IX_TinNhanTuVan_HoiThoaiId_VaiTroNguoiGui_DaXemBoiNguoiNhan ON TinNhanTuVan (HoiThoaiId, VaiTroNguoiGui, DaXemBoiNguoiNhan);
                    CREATE INDEX IX_TinNhanTuVan_NguoiGuiId ON TinNhanTuVan (NguoiGuiId);
                END

                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TepDinhKemTinNhan')
                BEGIN
                    CREATE TABLE TepDinhKemTinNhan (
                        Id INT NOT NULL CONSTRAINT PK_TepDinhKemTinNhan PRIMARY KEY IDENTITY,
                        TinNhanId INT NOT NULL,
                        TenGoc NVARCHAR(260) NOT NULL,
                        TenLuuTru NVARCHAR(260) NOT NULL,
                        ContentType NVARCHAR(100) NOT NULL,
                        KichThuoc BIGINT NOT NULL,
                        ThuTu INT NOT NULL DEFAULT 0,
                        NgayTaiLen DATETIME2 NOT NULL DEFAULT GETDATE(),
                        CONSTRAINT FK_TepDinhKemTinNhan_TinNhanTuVan_TinNhanId
                            FOREIGN KEY (TinNhanId) REFERENCES TinNhanTuVan (Id) ON DELETE CASCADE
                    );
                    CREATE INDEX IX_TepDinhKemTinNhan_TinNhanId ON TepDinhKemTinNhan (TinNhanId);
                END");

                // Hồ sơ & Cài đặt bác sĩ - Tab "Thông tin cá nhân & hành nghề"
                // (Sprint 2) - cùng lý do như các khối trên. Kiểm tra cột
                // DuocDuyetHoSoHanhNghe đã tồn tại chưa TRƯỚC ở phía C# (đúng
                // khuôn kiểm tra CK_BenhNhan_MucDoDiUng ở trên) vì UPDATE bên
                // dưới tham chiếu đúng cột này - đặt UPDATE cùng batch với
                // ALTER TABLE ADD COLUMN của chính nó sẽ lỗi "Invalid column
                // name" do SQL Server phân giải tên ở thời điểm biên dịch CẢ
                // batch, trước khi ALTER TABLE thật sự chạy.
                var hoSoHanhNgheColumnExists = context.Database.SqlQueryRaw<int>(
                        "SELECT CAST(1 AS INT) AS Value WHERE COL_LENGTH('NguoiDung','DuocDuyetHoSoHanhNghe') IS NOT NULL")
                    .AsEnumerable().Any();

                context.Database.ExecuteSqlRaw(@"
                IF COL_LENGTH('NguoiDung','DuocDuyetHoSoHanhNghe') IS NULL
                    ALTER TABLE NguoiDung ADD DuocDuyetHoSoHanhNghe BIT NOT NULL DEFAULT 0;

                IF COL_LENGTH('BacSi','NgaySinh') IS NULL
                    ALTER TABLE BacSi ADD NgaySinh DATETIME2 NULL;
                IF COL_LENGTH('BacSi','SoCCHN') IS NULL
                    ALTER TABLE BacSi ADD SoCCHN NVARCHAR(50) NULL;
                IF COL_LENGTH('BacSi','NgayCapCCHN') IS NULL
                    ALTER TABLE BacSi ADD NgayCapCCHN DATETIME2 NULL;
                IF COL_LENGTH('BacSi','NoiCapCCHN') IS NULL
                    ALTER TABLE BacSi ADD NoiCapCCHN NVARCHAR(200) NULL;
                IF COL_LENGTH('BacSi','PhamViHanhNghe') IS NULL
                    ALTER TABLE BacSi ADD PhamViHanhNghe NVARCHAR(500) NULL;
                IF COL_LENGTH('BacSi','GioiThieuNgan') IS NULL
                    ALTER TABLE BacSi ADD GioiThieuNgan NVARCHAR(500) NULL;
                IF COL_LENGTH('BacSi','QuaTrinhDaoTao') IS NULL
                    ALTER TABLE BacSi ADD QuaTrinhDaoTao NVARCHAR(2000) NULL;");

                // Batch riêng, CHẠY SAU khi ALTER TABLE ở trên đã thật sự áp
                // dụng - cấp sẵn quyền duyệt cho Admin hiện có, chỉ đúng 1 lần
                // (lúc cột chưa tồn tại trước đó). Admin tạo sau này mặc định
                // DuocDuyetHoSoHanhNghe=0, cần được cấp thủ công.
                if (!hoSoHanhNgheColumnExists)
                {
                    context.Database.ExecuteSqlRaw("UPDATE NguoiDung SET DuocDuyetHoSoHanhNghe = 1 WHERE VaiTro = 'Admin';");
                }

                context.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'YeuCauThayDoiHoSo')
                BEGIN
                    CREATE TABLE YeuCauThayDoiHoSo (
                        Id INT NOT NULL CONSTRAINT PK_YeuCauThayDoiHoSo PRIMARY KEY IDENTITY,
                        BacSiId INT NOT NULL,
                        NgayDeXuat DATETIME2 NOT NULL DEFAULT GETDATE(),
                        TrangThai NVARCHAR(20) NOT NULL DEFAULT 'ChoDuyet',
                        DuLieuCuJson NVARCHAR(4000) NOT NULL,
                        DuLieuMoiJson NVARCHAR(4000) NOT NULL,
                        NguoiDuyetId INT NULL,
                        NgayDuyet DATETIME2 NULL,
                        LyDoTuChoi NVARCHAR(500) NULL,
                        CONSTRAINT CK_YeuCauThayDoiHoSo_TrangThai CHECK (TrangThai IN ('ChoDuyet','DaDuyet','TuChoi')),
                        CONSTRAINT FK_YeuCauThayDoiHoSo_BacSi_BacSiId FOREIGN KEY (BacSiId) REFERENCES BacSi (Id) ON DELETE CASCADE,
                        CONSTRAINT FK_YeuCauThayDoiHoSo_NguoiDung_NguoiDuyetId FOREIGN KEY (NguoiDuyetId) REFERENCES NguoiDung (Id)
                    );
                    CREATE INDEX IX_YeuCauThayDoiHoSo_BacSiId ON YeuCauThayDoiHoSo (BacSiId);
                    CREATE INDEX IX_YeuCauThayDoiHoSo_NguoiDuyetId ON YeuCauThayDoiHoSo (NguoiDuyetId);
                END");

                // Hồ sơ & Cài đặt bác sĩ - Tab "Tài khoản & Bảo mật" (Sprint 3):
                // 2FA + phiên đăng nhập + tuỳ chọn giao diện. Cột trước, bảng
                // mới sau - cùng lý do các khối trên.
                context.Database.ExecuteSqlRaw(@"
                IF COL_LENGTH('NguoiDung','TotpBiMat') IS NULL
                    ALTER TABLE NguoiDung ADD TotpBiMat NVARCHAR(MAX) NULL;
                IF COL_LENGTH('NguoiDung','TotpBatDau') IS NULL
                    ALTER TABLE NguoiDung ADD TotpBatDau BIT NOT NULL DEFAULT 0;
                IF COL_LENGTH('NguoiDung','SidebarThuGonMacDinh') IS NULL
                    ALTER TABLE NguoiDung ADD SidebarThuGonMacDinh BIT NOT NULL DEFAULT 0;
                IF COL_LENGTH('NguoiDung','SoDongMoiTrangMacDinh') IS NULL
                    ALTER TABLE NguoiDung ADD SoDongMoiTrangMacDinh INT NULL;");

                context.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MaDuPhongTOTP')
                BEGIN
                    CREATE TABLE MaDuPhongTOTP (
                        Id INT NOT NULL CONSTRAINT PK_MaDuPhongTOTP PRIMARY KEY IDENTITY,
                        NguoiDungId INT NOT NULL,
                        MaHash NVARCHAR(200) NOT NULL,
                        DaDung BIT NOT NULL DEFAULT 0,
                        NgayDung DATETIME2 NULL,
                        NgayTao DATETIME2 NOT NULL DEFAULT GETDATE(),
                        CONSTRAINT FK_MaDuPhongTOTP_NguoiDung_NguoiDungId
                            FOREIGN KEY (NguoiDungId) REFERENCES NguoiDung (Id) ON DELETE CASCADE
                    );
                    CREATE INDEX IX_MaDuPhongTOTP_NguoiDungId ON MaDuPhongTOTP (NguoiDungId);
                END

                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PhienDangNhap')
                BEGIN
                    CREATE TABLE PhienDangNhap (
                        Id INT NOT NULL CONSTRAINT PK_PhienDangNhap PRIMARY KEY IDENTITY,
                        NguoiDungId INT NOT NULL,
                        SessionToken NVARCHAR(64) NOT NULL,
                        ThietBi NVARCHAR(200) NOT NULL,
                        IpAddress NVARCHAR(50) NULL,
                        ThoiGianDangNhap DATETIME2 NOT NULL DEFAULT GETDATE(),
                        ThoiGianHoatDongCuoi DATETIME2 NOT NULL DEFAULT GETDATE(),
                        TrangThai NVARCHAR(20) NOT NULL DEFAULT 'HoatDong',
                        CONSTRAINT FK_PhienDangNhap_NguoiDung_NguoiDungId
                            FOREIGN KEY (NguoiDungId) REFERENCES NguoiDung (Id) ON DELETE CASCADE
                    );
                    CREATE INDEX IX_PhienDangNhap_NguoiDungId ON PhienDangNhap (NguoiDungId);
                    CREATE INDEX IX_PhienDangNhap_SessionToken ON PhienDangNhap (SessionToken);
                END");

                // Thanh toán hóa đơn (Cổng bệnh nhân) - khái niệm "Giao dịch thanh
                // toán" tách khỏi Hóa đơn (1 giao dịch phủ được nhiều hóa đơn,
                // webhook là nguồn chân lý duy nhất cho trạng thái). Batch 1: cột
                // mới trên HoaDon (chưa có FK) + bảng mới GiaoDichThanhToan (chưa
                // bị tham chiếu) - không có gì trong batch này phụ thuộc lẫn nhau.
                context.Database.ExecuteSqlRaw(@"
                IF COL_LENGTH('HoaDon','GiaoDichThanhToanHienTaiId') IS NULL
                    ALTER TABLE HoaDon ADD GiaoDichThanhToanHienTaiId INT NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'GiaoDichThanhToan')
                BEGIN
                    CREATE TABLE GiaoDichThanhToan (
                        Id INT NOT NULL CONSTRAINT PK_GiaoDichThanhToan PRIMARY KEY IDENTITY,
                        NguoiKhoiTaoId INT NOT NULL,
                        IdempotencyKey NVARCHAR(64) NOT NULL,
                        SoTien DECIMAL(18,2) NOT NULL,
                        PhuongThuc NVARCHAR(50) NOT NULL,
                        TrangThai NVARCHAR(20) NOT NULL DEFAULT 'ChoXuLy',
                        MaGiaoDichCong NVARCHAR(100) NULL,
                        NgayTao DATETIME2 NOT NULL DEFAULT GETDATE(),
                        NgayCapNhat DATETIME2 NULL,
                        CONSTRAINT CK_GiaoDichThanhToan_SoTien CHECK (SoTien >= 0),
                        CONSTRAINT CK_GiaoDichThanhToan_TrangThai CHECK (TrangThai IN ('ChoXuLy','DangXuLy','ThanhCong','ThatBai','DaHuy')),
                        CONSTRAINT FK_GiaoDichThanhToan_NguoiDung_NguoiKhoiTaoId
                            FOREIGN KEY (NguoiKhoiTaoId) REFERENCES NguoiDung (Id)
                    );
                    CREATE UNIQUE INDEX IX_GiaoDichThanhToan_IdempotencyKey ON GiaoDichThanhToan (IdempotencyKey);
                    CREATE UNIQUE INDEX IX_GiaoDichThanhToan_MaGiaoDichCong ON GiaoDichThanhToan (MaGiaoDichCong) WHERE MaGiaoDichCong IS NOT NULL;
                    CREATE INDEX IX_GiaoDichThanhToan_NguoiKhoiTaoId ON GiaoDichThanhToan (NguoiKhoiTaoId);
                END");

                // Batch 2: FK từ HoaDon sang GiaoDichThanhToan - CHẠY SAU khi
                // bảng đích đã thật sự tồn tại (bài học lỗi biên dịch cả batch ở
                // các khối trên: statement tham chiếu đối tượng vừa tạo ở batch
                // trước phải nằm ở batch riêng).
                context.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_HoaDon_GiaoDichThanhToan_GiaoDichThanhToanHienTaiId')
                BEGIN
                    ALTER TABLE HoaDon ADD CONSTRAINT FK_HoaDon_GiaoDichThanhToan_GiaoDichThanhToanHienTaiId
                        FOREIGN KEY (GiaoDichThanhToanHienTaiId) REFERENCES GiaoDichThanhToan (Id);
                    CREATE INDEX IX_HoaDon_GiaoDichThanhToanHienTaiId ON HoaDon (GiaoDichThanhToanHienTaiId);
                END");

                // Lịch làm việc (Khu vực bác sĩ) - Nghỉ phép + số dư phép năm.
                // 2 bảng đều chỉ tham chiếu BacSi/NguoiDung đã tồn tại từ trước
                // nên gộp 1 batch là an toàn (không có nguy cơ tham chiếu đối
                // tượng vừa tạo cùng batch như bài học ở khối GiaoDichThanhToan).
                context.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SoDuPhepNam')
                BEGIN
                    CREATE TABLE SoDuPhepNam (
                        Id INT NOT NULL CONSTRAINT PK_SoDuPhepNam PRIMARY KEY IDENTITY,
                        BacSiId INT NOT NULL,
                        Nam INT NOT NULL,
                        TongSoNgay DECIMAL(5,1) NOT NULL,
                        CongDonTuNamTruoc DECIMAL(5,1) NOT NULL,
                        DaDung DECIMAL(5,1) NOT NULL,
                        DaTamGiu DECIMAL(5,1) NOT NULL,
                        NgayCapNhat DATETIME2 NOT NULL DEFAULT GETDATE(),
                        CONSTRAINT FK_SoDuPhepNam_BacSi_BacSiId
                            FOREIGN KEY (BacSiId) REFERENCES BacSi (Id) ON DELETE CASCADE
                    );
                    CREATE UNIQUE INDEX IX_SoDuPhepNam_BacSiId_Nam ON SoDuPhepNam (BacSiId, Nam);
                END

                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'YeuCauNghiPhep')
                BEGIN
                    CREATE TABLE YeuCauNghiPhep (
                        Id INT NOT NULL CONSTRAINT PK_YeuCauNghiPhep PRIMARY KEY IDENTITY,
                        BacSiId INT NOT NULL,
                        TuNgay DATETIME2 NOT NULL,
                        DenNgay DATETIME2 NOT NULL,
                        Buoi NVARCHAR(10) NULL,
                        SoNgayTru DECIMAL(5,1) NOT NULL,
                        LoaiNghi NVARCHAR(20) NOT NULL,
                        LyDo NVARCHAR(1000) NOT NULL,
                        DinhKemUrl NVARCHAR(300) NULL,
                        TrangThai NVARCHAR(20) NOT NULL DEFAULT 'ChoDuyet',
                        NguoiDuyetId INT NULL,
                        NgayDuyet DATETIME2 NULL,
                        LyDoTuChoi NVARCHAR(500) NULL,
                        NgayTao DATETIME2 NOT NULL DEFAULT GETDATE(),
                        CONSTRAINT CK_YeuCauNghiPhep_TrangThai CHECK (TrangThai IN ('ChoDuyet','DaDuyet','TuChoi')),
                        CONSTRAINT CK_YeuCauNghiPhep_LoaiNghi CHECK (LoaiNghi IN ('PhepNam','Om','ViecRieng','Khac')),
                        CONSTRAINT CK_YeuCauNghiPhep_Buoi CHECK (Buoi IS NULL OR Buoi IN ('Sang','Chieu')),
                        CONSTRAINT CK_YeuCauNghiPhep_KhoangNgay CHECK (DenNgay >= TuNgay),
                        CONSTRAINT FK_YeuCauNghiPhep_BacSi_BacSiId
                            FOREIGN KEY (BacSiId) REFERENCES BacSi (Id) ON DELETE CASCADE,
                        CONSTRAINT FK_YeuCauNghiPhep_NguoiDung_NguoiDuyetId
                            FOREIGN KEY (NguoiDuyetId) REFERENCES NguoiDung (Id)
                    );
                    CREATE INDEX IX_YeuCauNghiPhep_BacSiId ON YeuCauNghiPhep (BacSiId);
                    CREATE INDEX IX_YeuCauNghiPhep_NguoiDuyetId ON YeuCauNghiPhep (NguoiDuyetId);
                END");
            }
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

app.MapHub<DoctorDashboardHub>("/hubs/doctor-dashboard");
app.MapHub<ConsultationChatHub>("/hubs/consultation-chat");

// Map Area Routing (must be placed before default route)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
