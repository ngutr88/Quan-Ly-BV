using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Models;

namespace QuanLyBenhVien.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Doctor> Doctors { get; set; } = null!;
        public DbSet<Patient> Patients { get; set; } = null!;
        public DbSet<Department> Departments { get; set; } = null!;
        public DbSet<Service> Services { get; set; } = null!;
        public DbSet<Appointment> Appointments { get; set; } = null!;
        public DbSet<ExaminationRecord> ExaminationRecords { get; set; } = null!;
        public DbSet<Prescription> Prescriptions { get; set; } = null!;
        public DbSet<PrescriptionDetail> PrescriptionDetails { get; set; } = null!;
        public DbSet<Medicine> Medicines { get; set; } = null!;
        public DbSet<MedicineBatch> MedicineBatches { get; set; } = null!;
        public DbSet<Invoice> Invoices { get; set; } = null!;
        public DbSet<InvoiceDetail> InvoiceDetails { get; set; } = null!;
        public DbSet<Review> Reviews { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<Dependent> Dependents { get; set; } = null!;
        public DbSet<DoctorWorkSchedule> DoctorWorkSchedules { get; set; } = null!;
        public DbSet<PatientDocument> PatientDocuments { get; set; } = null!;
        public DbSet<Article> Articles { get; set; } = null!;
        public DbSet<FamilyHistory> FamilyHistories { get; set; } = null!;
        public DbSet<Immunization> Immunizations { get; set; } = null!;
        public DbSet<PatientHealthMetric> PatientHealthMetrics { get; set; } = null!;
        public DbSet<RolePermission> RolePermissions { get; set; } = null!;
        public DbSet<Supplier> Suppliers { get; set; } = null!;
        public DbSet<GoodsReceipt> GoodsReceipts { get; set; } = null!;
        public DbSet<GoodsReceiptDetail> GoodsReceiptDetails { get; set; } = null!;
        public DbSet<PasswordResetRequest> PasswordResetRequests { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Provider currently in use (Sqlite today; SqlServer/MySql are wired in
            // Program.cs for a future migration). A handful of constraints below need
            // provider-specific raw SQL (date/time functions, NULL-handling in unique
            // indexes), so branch on it once here instead of scattering checks around.
            var isSqlServer = Database.ProviderName == "Microsoft.EntityFrameworkCore.SqlServer";
            var isMySql = Database.ProviderName != null &&
                          Database.ProviderName.Contains("MySql", StringComparison.OrdinalIgnoreCase);
            // SQL Server's unique INDEX/CONSTRAINT allows at most one NULL, unlike
            // Sqlite/MySQL which treat every NULL as distinct. A filtered index is the
            // portable way to get "unique only when not null" on SQL Server; the other
            // two providers already behave that way with a plain unique index.
            string NowSql() => isSqlServer ? "GETDATE()" : isMySql ? "NOW()" : "CURRENT_TIMESTAMP";

            // One on/off switch per role+module.
            modelBuilder.Entity<RolePermission>()
                .HasIndex(p => new { p.VaiTro, p.ModuleKey })
                .IsUnique();

            // ================================================================
            // Quan hệ & hành vi xóa (mục 4 và 5 của yêu cầu)
            // ================================================================

            // User - Doctor (One-to-One). Trước đây Cascade: xóa một NguoiDung sẽ xóa
            // luôn hồ sơ BacSi và kéo theo toàn bộ lịch sử (DanhGia, LichLamViecBacSi...).
            // Đổi sang Restrict: không cho xóa cứng tài khoản còn hồ sơ bác sĩ; việc
            // "xóa" một bác sĩ phải đi qua cơ chế xóa mềm (Doctor.DaXoa).
            modelBuilder.Entity<User>()
                .HasOne(u => u.DoctorProfile)
                .WithOne(d => d.User)
                .HasForeignKey<Doctor>(d => d.NguoiDungId)
                .OnDelete(DeleteBehavior.Restrict);

            // User - Patient (One-to-One). Cùng lý do như trên: không để việc xóa tài
            // khoản xóa luôn hồ sơ bệnh nhân và toàn bộ lịch sử khám chữa bệnh.
            modelBuilder.Entity<User>()
                .HasOne(u => u.PatientProfile)
                .WithOne(p => p.User)
                .HasForeignKey<Patient>(p => p.NguoiDungId)
                .OnDelete(DeleteBehavior.Restrict);

            // User - XoaBoi (self-reference, ai đã xóa mềm tài khoản này). NoAction vì
            // SQL Server không cho phép Cascade trên self-reference nhiều đường.
            modelBuilder.Entity<User>()
                .HasOne(u => u.XoaBoi)
                .WithMany()
                .HasForeignKey(u => u.XoaBoiId)
                .OnDelete(DeleteBehavior.NoAction);

            // Doctor - XoaBoi (ai đã xóa mềm hồ sơ bác sĩ)
            modelBuilder.Entity<Doctor>()
                .HasOne(d => d.XoaBoi)
                .WithMany()
                .HasForeignKey(d => d.XoaBoiId)
                .OnDelete(DeleteBehavior.Restrict);

            // Patient - XoaBoi (ai đã xóa mềm hồ sơ bệnh nhân)
            modelBuilder.Entity<Patient>()
                .HasOne(p => p.XoaBoi)
                .WithMany()
                .HasForeignKey(p => p.XoaBoiId)
                .OnDelete(DeleteBehavior.Restrict);

            // Doctor - Department (Many-to-One). Không cho xóa Khoa khi vẫn còn bác sĩ.
            modelBuilder.Entity<Doctor>()
                .HasOne(d => d.Department)
                .WithMany()
                .HasForeignKey(d => d.KhoaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Doctor - WorkSchedule (One-to-Many). Lịch làm việc là dữ liệu chi tiết
            // phụ thuộc hoàn toàn vào bác sĩ => Cascade hợp lý.
            modelBuilder.Entity<DoctorWorkSchedule>()
                .HasOne(s => s.Doctor)
                .WithMany(d => d.WorkSchedules)
                .HasForeignKey(s => s.BacSiId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DoctorWorkSchedule>()
                .HasIndex(s => new { s.BacSiId, s.ThuTrongTuan, s.GioBatDau, s.GioKetThuc })
                .IsUnique();

            // Service - Department (Many-to-One)
            modelBuilder.Entity<Service>()
                .HasOne(s => s.Department)
                .WithMany()
                .HasForeignKey(s => s.KhoaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Appointment - Patient (Many-to-One). Restrict: không cho xóa cứng bệnh
            // nhân còn lịch khám (đằng nào cũng đã Restrict từ trước, giữ nguyên).
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany()
                .HasForeignKey(a => a.BenhNhanId)
                .OnDelete(DeleteBehavior.Restrict);

            // Appointment - Doctor (Many-to-One). SetNull: bác sĩ ngừng hoạt động vẫn
            // giữ lại lịch khám lịch sử, chỉ gỡ liên kết BacSiId (giữ nguyên theo yêu cầu).
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany()
                .HasForeignKey(a => a.BacSiId)
                .OnDelete(DeleteBehavior.SetNull);

            // ExaminationRecord - Appointment (thực sự là One-to-One: một lịch khám chỉ
            // tạo một phiếu khám, ExamController đã chặn tạo phiếu khám thứ hai ở tầng
            // ứng dụng). Đổi WithMany -> WithOne để EF tự tạo UNIQUE INDEX trên
            // LichKhamId, chính thức hóa ràng buộc này ở tầng CSDL (mục 2).
            // Restrict thay vì Cascade: xóa lịch khám không được kéo theo mất phiếu khám.
            modelBuilder.Entity<ExaminationRecord>()
                .HasOne(e => e.Appointment)
                .WithOne()
                .HasForeignKey<ExaminationRecord>(e => e.LichKhamId)
                .OnDelete(DeleteBehavior.Restrict);

            // Prescription - ExaminationRecord (One-to-One thực sự: một phiếu khám chỉ
            // sinh ra tối đa một đơn thuốc trong luồng ExamController.CompleteSession).
            modelBuilder.Entity<Prescription>()
                .HasOne(p => p.ExaminationRecord)
                .WithOne()
                .HasForeignKey<Prescription>(p => p.PhieuKhamId)
                .OnDelete(DeleteBehavior.Restrict);

            // PrescriptionDetail - Prescription (Many-to-One). Chi tiết đơn thuốc phụ
            // thuộc hoàn toàn vào đơn thuốc => Cascade hợp lý (mục 5, được phép CASCADE).
            modelBuilder.Entity<PrescriptionDetail>()
                .HasOne(pd => pd.Prescription)
                .WithMany(p => p.PrescriptionDetails)
                .HasForeignKey(pd => pd.DonThuocId)
                .OnDelete(DeleteBehavior.Cascade);

            // PrescriptionDetail - Medicine (Many-to-One). Restrict: không cho xóa
            // thuốc đã từng được kê để giữ lịch sử đơn thuốc.
            modelBuilder.Entity<PrescriptionDetail>()
                .HasOne(pd => pd.Medicine)
                .WithMany()
                .HasForeignKey(pd => pd.ThuocId)
                .OnDelete(DeleteBehavior.Restrict);

            // MedicineBatch - Medicine (Many-to-One). Lô thuốc phụ thuộc hoàn toàn vào
            // thuốc gốc => Cascade được phép theo mục 5 ("nếu nghiệp vụ cho phép").
            modelBuilder.Entity<MedicineBatch>()
                .HasOne(mb => mb.Medicine)
                .WithMany(m => m.LoThuocs)
                .HasForeignKey(mb => mb.ThuocId)
                .OnDelete(DeleteBehavior.Cascade);

            // Invoice - ExaminationRecord (One-to-One thực sự: CompleteSession chỉ tạo
            // một hóa đơn cho mỗi phiếu khám).
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.ExaminationRecord)
                .WithOne()
                .HasForeignKey<Invoice>(i => i.PhieuKhamId)
                .OnDelete(DeleteBehavior.Restrict);

            // InvoiceDetail - Invoice (Many-to-One). Chi tiết hóa đơn phụ thuộc hoàn
            // toàn vào hóa đơn => Cascade hợp lý (mục 5, được phép CASCADE).
            modelBuilder.Entity<InvoiceDetail>()
                .HasOne(id => id.Invoice)
                .WithMany(i => i.InvoiceDetails)
                .HasForeignKey(id => id.HoaDonId)
                .OnDelete(DeleteBehavior.Cascade);

            // Review - Patient (Many-to-One), giữ Restrict như trước.
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Patient)
                .WithMany()
                .HasForeignKey(r => r.BenhNhanId)
                .OnDelete(DeleteBehavior.Restrict);

            // Review - Doctor (Many-to-One). Trước đây Cascade (xóa bác sĩ mất hết đánh
            // giá); đổi sang Restrict vì đánh giá là dữ liệu lịch sử cần giữ lại, và từ
            // nay việc "xóa" bác sĩ phải dùng xóa mềm (Doctor.DaXoa) nên đường Cascade
            // này thực chất không còn cần thiết.
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Doctor)
                .WithMany()
                .HasForeignKey(r => r.BacSiId)
                .OnDelete(DeleteBehavior.Restrict);

            // Review - Appointment (mới, tùy chọn): gắn đánh giá với đúng lần khám để
            // ràng buộc UNIQUE (bên dưới) ngăn đánh giá trùng cho cùng một lần khám.
            // SetNull: hủy liên kết lịch khám không được xóa mất đánh giá đã có.
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Appointment)
                .WithMany()
                .HasForeignKey(r => r.LichKhamId)
                .OnDelete(DeleteBehavior.SetNull);

            // Dependent (NguoiThan) - Patient. Đây là dữ liệu y tế của bệnh nhân, không
            // phải bảng chi tiết của một giao dịch đơn lẻ => Restrict (mục 5: hạn chế
            // CASCADE với dữ liệu y tế). Muốn gỡ bệnh nhân phải xóa mềm (Patient.DaXoa).
            modelBuilder.Entity<Dependent>()
                .HasOne(d => d.Patient)
                .WithMany(p => p.Dependents)
                .HasForeignKey(d => d.BenhNhanId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PatientDocument>()
                .HasOne(d => d.Patient)
                .WithMany()
                .HasForeignKey(d => d.BenhNhanId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FamilyHistory>()
                .HasOne(f => f.Patient)
                .WithMany()
                .HasForeignKey(f => f.BenhNhanId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Immunization>()
                .HasOne(i => i.Patient)
                .WithMany()
                .HasForeignKey(i => i.BenhNhanId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PatientHealthMetric>()
                .HasOne(m => m.Patient)
                .WithMany()
                .HasForeignKey(m => m.BenhNhanId)
                .OnDelete(DeleteBehavior.Restrict);

            // ================================================================
            // Ràng buộc UNIQUE (mục 2 của yêu cầu)
            // ================================================================
            // Ghi chú: BacSi.NguoiDungId và BenhNhan.NguoiDungId đã tự động là UNIQUE
            // nhờ cấu hình quan hệ One-to-One với NguoiDung ở trên (EF Core luôn tạo
            // UNIQUE INDEX cho khóa ngoại phụ thuộc trong quan hệ 1-1), nên không cần
            // khai báo lại. PhieuKham.LichKhamId, DonThuoc.PhieuKhamId, HoaDon.PhieuKhamId
            // cũng vừa được chính thức hóa thành UNIQUE ở trên qua WithOne().

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Sdt)
                .IsUnique();

            // SoCCCD/SoBHYT là cột nullable (NULL = "chưa có"). Trên SQL Server, UNIQUE
            // INDEX/CONSTRAINT mặc định chỉ cho phép một giá trị NULL, khác với
            // Sqlite/MySQL vốn coi mỗi NULL là khác biệt; dùng filtered index để đồng
            // nhất hành vi "duy nhất nếu khác NULL" trên cả ba nhà cung cấp.
            var soCccdIndex = modelBuilder.Entity<Patient>()
                .HasIndex(p => p.SoCCCD)
                .IsUnique();
            if (isSqlServer) soCccdIndex.HasFilter("[SoCCCD] IS NOT NULL");

            var soBhytIndex = modelBuilder.Entity<Patient>()
                .HasIndex(p => p.SoBHYT)
                .IsUnique();
            if (isSqlServer) soBhytIndex.HasFilter("[SoBHYT] IS NOT NULL");

            // DanhGia.LichKhamId: nullable, tối đa một đánh giá cho mỗi lần khám cụ thể.
            var reviewLichKhamIndex = modelBuilder.Entity<Review>()
                .HasIndex(r => r.LichKhamId)
                .IsUnique();
            if (isSqlServer) reviewLichKhamIndex.HasFilter("[LichKhamId] IS NOT NULL");

            // ================================================================
            // Chỉ mục tra cứu thường xuyên (mục 8 của yêu cầu)
            // ================================================================
            // Không khai báo lại các FK đã được EF Core tự động đánh index theo quy ước
            // (BacSi.KhoaId, LichKham.BenhNhanId, LichKham.BacSiId, PhieuKham.LichKhamId,
            // HoaDon.PhieuKhamId, DonThuoc.PhieuKhamId, LoThuoc.ThuocId, ThongBao.NguoiDungId).
            // Hai cột dưới đây không phải khóa ngoại nên cần khai báo index tường minh.
            modelBuilder.Entity<Appointment>()
                .HasIndex(a => a.ThoiGian);

            modelBuilder.Entity<Appointment>()
                .HasIndex(a => a.TrangThai);

            // ================================================================
            // Ràng buộc CHECK (mục 3 của yêu cầu)
            // ================================================================
            modelBuilder.Entity<Review>()
                .ToTable(t => t.HasCheckConstraint("CK_DanhGia_SoSao", "SoSao BETWEEN 1 AND 5"));

            modelBuilder.Entity<Medicine>(e =>
            {
                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_Thuoc_TonKho", "TonKho >= 0");
                    t.HasCheckConstraint("CK_Thuoc_NguongToiThieu", "NguongToiThieu >= 0");
                    t.HasCheckConstraint("CK_Thuoc_Gia", "Gia >= 0");
                });
            });

            modelBuilder.Entity<MedicineBatch>(e =>
            {
                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_LoThuoc_SoLuongNhap", "SoLuongNhap > 0");
                    t.HasCheckConstraint("CK_LoThuoc_SoLuongTon", "SoLuongTon >= 0");
                    t.HasCheckConstraint("CK_LoThuoc_TonKhongVuotQuaNhap", "SoLuongTon <= SoLuongNhap");
                    t.HasCheckConstraint("CK_LoThuoc_HanSuDung", "HanSuDung >= NgayNhap");
                });
            });

            modelBuilder.Entity<PrescriptionDetail>()
                .ToTable(t => t.HasCheckConstraint("CK_ChiTietDonThuoc_SoLuong", "SoLuong > 0"));

            modelBuilder.Entity<Service>()
                .ToTable(t => t.HasCheckConstraint("CK_DichVu_Gia", "Gia >= 0"));

            modelBuilder.Entity<Invoice>(e =>
            {
                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_HoaDon_TongTien", "TongTien >= 0");
                    t.HasCheckConstraint("CK_HoaDon_TrangThaiThanhToan",
                        "TrangThaiThanhToan IN ('ChuaThanhToan','DaThanhToan','DangXuLy','QuaHan','ThanhToanThatBai','DaHuy')");
                    // Danh sách phương thức hiện có trong ứng dụng (PaymentController,
                    // InvoicesController). NULL nghĩa là hóa đơn chưa chọn phương thức.
                    t.HasCheckConstraint("CK_HoaDon_PhuongThuc",
                        "PhuongThuc IS NULL OR PhuongThuc IN ('TienMat','ChuyenKhoan','Online (MoMo)','Online (VNPay)','Online (ZaloPay)','Online')");
                });
            });

            modelBuilder.Entity<InvoiceDetail>()
                .ToTable(t => t.HasCheckConstraint("CK_ChiTietHoaDon_SoTien", "SoTien >= 0"));

            modelBuilder.Entity<DoctorWorkSchedule>(e =>
            {
                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_LichLamViecBacSi_SoBenhNhanToiDa", "SoBenhNhanToiDa > 0");
                    t.HasCheckConstraint("CK_LichLamViecBacSi_ThoiLuongKhamPhut", "ThoiLuongKhamPhut BETWEEN 5 AND 240");
                    t.HasCheckConstraint("CK_LichLamViecBacSi_GioKetThuc", "GioKetThuc > GioBatDau");
                });
            });

            // Ngày sinh không được lớn hơn thời điểm ghi nhận (đánh giá tại thời điểm
            // INSERT/UPDATE, không hồi tố cho dữ liệu cũ - hành vi chuẩn của CHECK).
            modelBuilder.Entity<Patient>()
                .ToTable(t => t.HasCheckConstraint("CK_BenhNhan_NgaySinh", $"NgaySinh <= {NowSql()}"));

            modelBuilder.Entity<User>(e =>
            {
                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_NguoiDung_VaiTro", "VaiTro IN ('Admin','Doctor','Patient')");
                    t.HasCheckConstraint("CK_NguoiDung_TrangThai", "TrangThai IN ('Active','Blocked')");
                });
            });

            modelBuilder.Entity<Appointment>()
                .ToTable(t => t.HasCheckConstraint("CK_LichKham_TrangThai",
                    "TrangThai IN ('ChoXacNhan','DaXacNhan','DangKham','HoanThanh','DaHuy','VangMat')"));

            // ChucVu có dữ liệu thực tế bị gõ sai/thiếu dấu (đã làm sạch trong migration
            // trước khi thêm CHECK này - xem script làm sạch dữ liệu).
            // Chuỗi có dấu tiếng Việt: SQL Server diễn giải literal '...' (không tiền tố
            // N) theo bảng mã không-Unicode mặc định của server, làm hỏng dấu thành '?'
            // (đã phát hiện thực tế khi EnsureCreated trên SQL Server) - bắt buộc dùng
            // N'...' trên SQL Server; SQLite/MySQL thì dùng nguyên văn không tiền tố N.
            var chucVuList = isSqlServer
                ? "N'Bác sĩ',N'Phó trưởng khoa',N'Trưởng khoa'"
                : "'Bác sĩ','Phó trưởng khoa','Trưởng khoa'";
            modelBuilder.Entity<Doctor>()
                .ToTable(t => t.HasCheckConstraint("CK_BacSi_ChucVu", $"ChucVu IN ({chucVuList})"));

            // ================================================================
            // Giá trị mặc định cho cột ngày giờ (mục 9 của yêu cầu)
            // ================================================================
            // EF Core luôn gửi giá trị tường minh khi ghi qua DbContext (Ngay* = DateTime.Now
            // ở tầng C#), nên các DEFAULT dưới đây chủ yếu là lưới an toàn cho các câu
            // INSERT trực tiếp bằng SQL, không làm thay đổi hành vi hiện tại của ứng dụng.
            modelBuilder.Entity<User>().Property(u => u.NgayTao).HasDefaultValueSql(NowSql());
            modelBuilder.Entity<Appointment>().Property(a => a.NgayTao).HasDefaultValueSql(NowSql());
            modelBuilder.Entity<Review>().Property(r => r.NgayTao).HasDefaultValueSql(NowSql());
            modelBuilder.Entity<Invoice>().Property(i => i.NgayTao).HasDefaultValueSql(NowSql());
            modelBuilder.Entity<Prescription>().Property(p => p.NgayKe).HasDefaultValueSql(NowSql());
            modelBuilder.Entity<ExaminationRecord>().Property(e => e.NgayKham).HasDefaultValueSql(NowSql());
            modelBuilder.Entity<Notification>().Property(n => n.NgayGui).HasDefaultValueSql(NowSql());
            modelBuilder.Entity<AuditLog>().Property(a => a.ThoiGian).HasDefaultValueSql(NowSql());
            modelBuilder.Entity<FamilyHistory>().Property(f => f.NgayGhiNhan).HasDefaultValueSql(NowSql());
            modelBuilder.Entity<PatientDocument>().Property(d => d.NgayTaiLen).HasDefaultValueSql(NowSql());
            modelBuilder.Entity<PatientHealthMetric>().Property(m => m.NgayDo).HasDefaultValueSql(NowSql());
            modelBuilder.Entity<PatientHealthMetric>().Property(m => m.NgayTao).HasDefaultValueSql(NowSql());
            modelBuilder.Entity<Article>().Property(a => a.NgayDang).HasDefaultValueSql(NowSql());
            modelBuilder.Entity<RolePermission>().Property(p => p.CapNhatLuc).HasDefaultValueSql(NowSql());

            // NgayNhap dùng mốc lịch sử cố định (thay vì "hôm nay") làm giá trị mặc định:
            // các lô thuốc đã tồn tại trước khi có cột này sẽ được backfill về mốc này khi
            // chạy migration, đảm bảo luôn thỏa CK_LoThuoc_HanSuDung (HanSuDung >= NgayNhap)
            // bất kể hạn dùng đã nhập là bao giờ. Ứng dụng luôn set NgayNhap tường minh
            // khi tạo lô mới (xem MedicineBatch.NgayNhap và MedicinesController.ReceiveBatch)
            // nên giá trị mặc định này chỉ có tác dụng cho dữ liệu cũ / INSERT SQL trực tiếp.
            modelBuilder.Entity<MedicineBatch>().Property(b => b.NgayNhap).HasDefaultValueSql("'2000-01-01 00:00:00'");

            // ================================================================
            // Phiếu nhập kho thuốc (NhaCungCap / PhieuNhapKho / PhieuNhapKhoChiTiet)
            // ================================================================

            modelBuilder.Entity<MedicineBatch>()
                .HasOne(b => b.NhaCungCap)
                .WithMany()
                .HasForeignKey(b => b.NhaCungCapId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GoodsReceipt>()
                .HasOne(r => r.NhaCungCap)
                .WithMany(s => s.PhieuNhaps)
                .HasForeignKey(r => r.NhaCungCapId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GoodsReceipt>()
                .HasOne(r => r.NguoiTao)
                .WithMany()
                .HasForeignKey(r => r.NguoiTaoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GoodsReceipt>()
                .HasOne(r => r.NguoiDuyet)
                .WithMany()
                .HasForeignKey(r => r.NguoiDuyetId)
                .OnDelete(DeleteBehavior.Restrict);

            // Phiếu điều chỉnh tự tham chiếu phiếu gốc - NoAction để tránh lỗi
            // "multiple cascade paths" của SQL Server trên self-reference.
            modelBuilder.Entity<GoodsReceipt>()
                .HasOne(r => r.PhieuGoc)
                .WithMany()
                .HasForeignKey(r => r.PhieuGocId)
                .OnDelete(DeleteBehavior.NoAction);

            // Chi tiết phụ thuộc hoàn toàn vào phiếu nhập => Cascade hợp lý
            // (giống ChiTietDonThuoc/ChiTietHoaDon, mục 5 của yêu cầu CSDL trước).
            modelBuilder.Entity<GoodsReceiptDetail>()
                .HasOne(d => d.PhieuNhapKho)
                .WithMany(r => r.ChiTiet)
                .HasForeignKey(d => d.PhieuNhapKhoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GoodsReceiptDetail>()
                .HasOne(d => d.Medicine)
                .WithMany()
                .HasForeignKey(d => d.ThuocId)
                .OnDelete(DeleteBehavior.Restrict);

            // SetNull: nếu lô thuốc kết quả sau này biến mất (vd Thuoc gốc bị
            // xóa, Cascade xuống LoThuoc), dòng chi tiết phiếu vẫn giữ lại để
            // không mất lịch sử nhập kho, chỉ mất liên kết tới lô cụ thể.
            modelBuilder.Entity<GoodsReceiptDetail>()
                .HasOne(d => d.LoThuoc)
                .WithMany()
                .HasForeignKey(d => d.LoThuocId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<GoodsReceipt>()
                .HasIndex(r => r.MaPhieu)
                .IsUnique();

            modelBuilder.Entity<GoodsReceipt>()
                .HasIndex(r => r.TrangThai);

            modelBuilder.Entity<GoodsReceipt>(e =>
            {
                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_PhieuNhapKho_TrangThai",
                        "TrangThai IN ('Nhap','ChoDuyet','DaDuyet','TuChoi','DaHuy')");
                    t.HasCheckConstraint("CK_PhieuNhapKho_LoaiNhap",
                        "LoaiNhap IN ('MuaNCC','ChuyenKho','HangTraVe','VienTro')");
                    t.HasCheckConstraint("CK_PhieuNhapKho_KhoNhap",
                        "KhoNhap IN ('KhoChan','KhoLe','NhaThuoc')");
                    t.HasCheckConstraint("CK_PhieuNhapKho_TongSoMatHang", "TongSoMatHang >= 0");
                    t.HasCheckConstraint("CK_PhieuNhapKho_TongTienTruocVAT", "TongTienTruocVAT >= 0");
                    t.HasCheckConstraint("CK_PhieuNhapKho_TienVAT", "TienVAT >= 0");
                    t.HasCheckConstraint("CK_PhieuNhapKho_TongThanhToan", "TongThanhToan >= 0");
                });
            });

            modelBuilder.Entity<GoodsReceiptDetail>(e =>
            {
                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_PhieuNhapKhoChiTiet_SoLuong", "SoLuong > 0");
                    t.HasCheckConstraint("CK_PhieuNhapKhoChiTiet_DonGia", "DonGia >= 0");
                    t.HasCheckConstraint("CK_PhieuNhapKhoChiTiet_PhanTramVAT", "PhanTramVAT BETWEEN 0 AND 100");
                    t.HasCheckConstraint("CK_PhieuNhapKhoChiTiet_ThanhTien", "ThanhTien >= 0");
                });
            });

            modelBuilder.Entity<MedicineBatch>()
                .ToTable(t => t.HasCheckConstraint("CK_LoThuoc_GiaNhap", "GiaNhap IS NULL OR GiaNhap >= 0"));

            modelBuilder.Entity<Supplier>()
                .HasIndex(s => s.TenNhaCungCap);

            modelBuilder.Entity<GoodsReceipt>().Property(r => r.NgayNhap).HasDefaultValueSql(NowSql());
            modelBuilder.Entity<GoodsReceipt>().Property(r => r.NgayTao).HasDefaultValueSql(NowSql());
            modelBuilder.Entity<Supplier>().Property(s => s.NgayTao).HasDefaultValueSql(NowSql());

            // Đếm theo IP và theo tài khoản trong cửa sổ 1 giờ (giới hạn tần suất
            // + CAPTCHA), tra ResetTokenHash khi bấm link đặt lại mật khẩu.
            modelBuilder.Entity<PasswordResetRequest>(e =>
            {
                e.HasIndex(r => new { r.NguoiDungId, r.NgayTao });
                e.HasIndex(r => new { r.IpAddress, r.NgayTao });
                e.HasIndex(r => r.ResetTokenHash);
                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_YeuCauKhoiPhucMatKhau_KhoiTaoBoi", "KhoiTaoBoi IN ('TuPhucVu','Admin')");
                    t.HasCheckConstraint("CK_YeuCauKhoiPhucMatKhau_Kenh", "Kenh IS NULL OR Kenh IN ('Email','Sdt')");
                });
            });
            modelBuilder.Entity<PasswordResetRequest>().Property(r => r.NgayTao).HasDefaultValueSql(NowSql());
        }
    }
}
