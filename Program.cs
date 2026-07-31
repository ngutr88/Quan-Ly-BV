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
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.AddService<QuanLyBenhVien.Helpers.ModulePermissionFilter>();
    options.Filters.AddService<QuanLyBenhVien.Helpers.ForcePasswordChangeFilter>();
});
builder.Services.AddScoped<QuanLyBenhVien.Services.ExcelExportService>();
builder.Services.AddScoped<DoctorDashboardNotifier>();
builder.Services.AddSignalR();
builder.Services.AddSingleton<QuanLyBenhVien.Services.HospitalSettingsProvider>();
builder.Services.AddScoped<QuanLyBenhVien.Services.AppointmentSlotService>();
builder.Services.AddScoped<QuanLyBenhVien.Services.IEmailSender, QuanLyBenhVien.Services.MockEmailSender>();
builder.Services.AddScoped<QuanLyBenhVien.Services.ISmsSender, QuanLyBenhVien.Services.MockSmsSender>();
builder.Services.AddScoped<QuanLyBenhVien.Services.IPrescriptionSafetyChecker, QuanLyBenhVien.Services.PrescriptionSafetyChecker>();
builder.Services.AddScoped<QuanLyBenhVien.Services.MedicineStockAllocator>();

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

// Map Area Routing (must be placed before default route)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
