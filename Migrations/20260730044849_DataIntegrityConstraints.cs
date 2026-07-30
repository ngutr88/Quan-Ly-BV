using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyBenhVien.Migrations
{
    /// <inheritdoc />
    public partial class DataIntegrityConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ================================================================
            // BƯỚC 0 - LÀM SẠCH DỮ LIỆU (mục 11 của yêu cầu: kiểm tra & dọn dữ liệu
            // trước khi áp UNIQUE/CHECK). Các câu lệnh dưới đây xử lý đúng những vi
            // phạm thực tế đã phát hiện khi khảo sát hms.db hiện tại - xem thêm bản
            // sao độc lập tại scripts/data-cleanup.sql để review/chạy thử trước.
            // Toàn bộ chạy TRƯỚC các thay đổi cấu trúc bên dưới nên dữ liệu đã sạch
            // trước khi UNIQUE INDEX / CHECK CONSTRAINT được tạo.
            // ================================================================

            // 1) NguoiDung.Email: 2 tài khoản dữ liệu thử nghiệm có Email rỗng
            // ("" không phải NULL nên vẫn đụng UNIQUE). Gán placeholder duy nhất
            // theo Id thay vì xóa dữ liệu, để không làm mất tài khoản hiện có.
            migrationBuilder.Sql(
                migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer"
                    ? "UPDATE NguoiDung SET Email = 'no-email-user-' + CAST(Id AS NVARCHAR(20)) + '@placeholder.invalid' WHERE Email IS NULL OR LTRIM(RTRIM(Email)) = '';"
                    : "UPDATE NguoiDung SET Email = 'no-email-user-' || Id || '@placeholder.invalid' WHERE Email IS NULL OR trim(Email) = '';");

            // 2) BenhNhan.SoCCCD / SoBHYT: chuỗi rỗng nghĩa là "chưa có" nhưng "" khác
            // NULL nên vẫn đụng UNIQUE. Chuẩn hóa "chưa có" thành NULL.
            migrationBuilder.Sql(
                migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer"
                    ? "UPDATE BenhNhan SET SoCCCD = NULL WHERE SoCCCD IS NOT NULL AND LTRIM(RTRIM(SoCCCD)) = '';"
                    : "UPDATE BenhNhan SET SoCCCD = NULL WHERE SoCCCD IS NOT NULL AND trim(SoCCCD) = '';");
            migrationBuilder.Sql(
                migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer"
                    ? "UPDATE BenhNhan SET SoBHYT = NULL WHERE SoBHYT IS NOT NULL AND LTRIM(RTRIM(SoBHYT)) = '';"
                    : "UPDATE BenhNhan SET SoBHYT = NULL WHERE SoBHYT IS NOT NULL AND trim(SoBHYT) = '';");

            // 3) BenhNhan.SoCCCD / SoBHYT trùng giá trị thật (vd. dữ liệu kiểm thử
            // đăng ký trùng BHYT "DN4123456789012"): giữ lại bản ghi có Id nhỏ nhất
            // (tạo trước), các bản ghi trùng sau đó chuyển về NULL thay vì xóa hồ sơ.
            // Chuẩn ANSI SQL, chạy giống nhau trên mọi provider.
            migrationBuilder.Sql(@"
                UPDATE BenhNhan SET SoBHYT = NULL
                WHERE SoBHYT IS NOT NULL AND Id NOT IN (
                    SELECT MIN(Id) FROM BenhNhan WHERE SoBHYT IS NOT NULL GROUP BY SoBHYT
                );");
            migrationBuilder.Sql(@"
                UPDATE BenhNhan SET SoCCCD = NULL
                WHERE SoCCCD IS NOT NULL AND Id NOT IN (
                    SELECT MIN(Id) FROM BenhNhan WHERE SoCCCD IS NOT NULL GROUP BY SoCCCD
                );");

            // 4) BacSi.ChucVu: dữ liệu thực tế có giá trị gõ thiếu dấu/sai chính tả
            // ("Bac si", "Pho truong khoa") và một biến thể mô tả thêm ("Bác sĩ điều
            // trị") lẽ ra cùng nghĩa với "Bác sĩ". Chuẩn hóa về đúng 3 giá trị hợp lệ
            // trước khi thêm CK_BacSi_ChucVu.
            migrationBuilder.Sql("UPDATE BacSi SET ChucVu = 'Bác sĩ' WHERE ChucVu IN ('Bac si', 'Bác sĩ điều trị', '');");
            migrationBuilder.Sql("UPDATE BacSi SET ChucVu = 'Phó trưởng khoa' WHERE ChucVu = 'Pho truong khoa';");

            // 5) HoaDon.PhuongThuc: một số hóa đơn chưa thanh toán bị gán nhầm
            // PhuongThuc = "ChuaThanhToan" (đó là TrangThaiThanhToan, không phải
            // phương thức). Chuẩn hóa về 'TienMat' - cùng giá trị mặc định mà
            // ExamController.CompleteSession đã dùng cho MỌI hóa đơn mới tạo trước
            // khi thanh toán thật sự diễn ra, nên đây là lựa chọn nhất quán với quy
            // ước sẵn có của ứng dụng (không dùng NULL ở bước này: cột PhuongThuc
            // vẫn đang NOT NULL tại thời điểm này trong migration - việc đổi sang
            // nullable chỉ có hiệu lực sau khi rebuild bảng HoaDon bên dưới).
            migrationBuilder.Sql("UPDATE HoaDon SET PhuongThuc = 'TienMat' WHERE PhuongThuc = 'ChuaThanhToan';");

            migrationBuilder.DropForeignKey(
                name: "FK_BacSi_NguoiDung_NguoiDungId",
                table: "BacSi");

            migrationBuilder.DropForeignKey(
                name: "FK_BenhNhan_NguoiDung_NguoiDungId",
                table: "BenhNhan");

            migrationBuilder.DropForeignKey(
                name: "FK_ChiSoSucKhoeTuDo_BenhNhan_BenhNhanId",
                table: "ChiSoSucKhoeTuDo");

            migrationBuilder.DropForeignKey(
                name: "FK_DanhGia_BacSi_BacSiId",
                table: "DanhGia");

            migrationBuilder.DropForeignKey(
                name: "FK_DonThuoc_PhieuKham_PhieuKhamId",
                table: "DonThuoc");

            migrationBuilder.DropForeignKey(
                name: "FK_HoaDon_PhieuKham_PhieuKhamId",
                table: "HoaDon");

            migrationBuilder.DropForeignKey(
                name: "FK_NguoiThan_BenhNhan_BenhNhanId",
                table: "NguoiThan");

            migrationBuilder.DropForeignKey(
                name: "FK_PhieuKham_LichKham_LichKhamId",
                table: "PhieuKham");

            migrationBuilder.DropForeignKey(
                name: "FK_TaiLieuBenhNhan_BenhNhan_BenhNhanId",
                table: "TaiLieuBenhNhan");

            migrationBuilder.DropForeignKey(
                name: "FK_TiemChung_BenhNhan_BenhNhanId",
                table: "TiemChung");

            migrationBuilder.DropForeignKey(
                name: "FK_TienSuGiaDinh_BenhNhan_BenhNhanId",
                table: "TienSuGiaDinh");

            migrationBuilder.DropIndex(
                name: "IX_PhieuKham_LichKhamId",
                table: "PhieuKham");

            migrationBuilder.DropIndex(
                name: "IX_HoaDon_PhieuKhamId",
                table: "HoaDon");

            migrationBuilder.DropIndex(
                name: "IX_DonThuoc_PhieuKhamId",
                table: "DonThuoc");

            migrationBuilder.AlterColumn<DateTime>(
                name: "NgayDang",
                table: "TinTuc",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTime>(
                name: "NgayGhiNhan",
                table: "TienSuGiaDinh",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTime>(
                name: "NgayGui",
                table: "ThongBao",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTime>(
                name: "NgayTaiLen",
                table: "TaiLieuBenhNhan",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTime>(
                name: "NgayKham",
                table: "PhieuKham",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CapNhatLuc",
                table: "PhanQuyenVaiTro",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ThoiGian",
                table: "NhatKyHeThong",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTime>(
                name: "NgayTao",
                table: "NguoiDung",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AddColumn<bool>(
                name: "DaXoa",
                table: "NguoiDung",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayXoa",
                table: "NguoiDung",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "XoaBoiId",
                table: "NguoiDung",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayNhap",
                table: "LoThuoc",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "'2000-01-01 00:00:00'");

            migrationBuilder.AlterColumn<DateTime>(
                name: "NgayTao",
                table: "LichKham",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "PhuongThuc",
                table: "HoaDon",
                type: "TEXT",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<DateTime>(
                name: "NgayTao",
                table: "HoaDon",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTime>(
                name: "NgayKe",
                table: "DonThuoc",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTime>(
                name: "NgayTao",
                table: "DanhGia",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AddColumn<int>(
                name: "LichKhamId",
                table: "DanhGia",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "NgayTao",
                table: "ChiSoSucKhoeTuDo",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTime>(
                name: "NgayDo",
                table: "ChiSoSucKhoeTuDo",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AddColumn<bool>(
                name: "DaXoa",
                table: "BenhNhan",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayXoa",
                table: "BenhNhan",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "XoaBoiId",
                table: "BenhNhan",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DaXoa",
                table: "BacSi",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayXoa",
                table: "BacSi",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "XoaBoiId",
                table: "BacSi",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Thuoc_Gia",
                table: "Thuoc",
                sql: "Gia >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Thuoc_NguongToiThieu",
                table: "Thuoc",
                sql: "NguongToiThieu >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Thuoc_TonKho",
                table: "Thuoc",
                sql: "TonKho >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuKham_LichKhamId",
                table: "PhieuKham",
                column: "LichKhamId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NguoiDung_Email",
                table: "NguoiDung",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NguoiDung_Sdt",
                table: "NguoiDung",
                column: "Sdt",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NguoiDung_XoaBoiId",
                table: "NguoiDung",
                column: "XoaBoiId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_NguoiDung_TrangThai",
                table: "NguoiDung",
                sql: "TrangThai IN ('Active','Blocked')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_NguoiDung_VaiTro",
                table: "NguoiDung",
                sql: "VaiTro IN ('Admin','Doctor','Patient')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LoThuoc_HanSuDung",
                table: "LoThuoc",
                sql: "HanSuDung >= NgayNhap");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LoThuoc_SoLuongNhap",
                table: "LoThuoc",
                sql: "SoLuongNhap > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LoThuoc_SoLuongTon",
                table: "LoThuoc",
                sql: "SoLuongTon >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LoThuoc_TonKhongVuotQuaNhap",
                table: "LoThuoc",
                sql: "SoLuongTon <= SoLuongNhap");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LichLamViecBacSi_GioKetThuc",
                table: "LichLamViecBacSi",
                sql: "GioKetThuc > GioBatDau");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LichLamViecBacSi_SoBenhNhanToiDa",
                table: "LichLamViecBacSi",
                sql: "SoBenhNhanToiDa > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LichLamViecBacSi_ThoiLuongKhamPhut",
                table: "LichLamViecBacSi",
                sql: "ThoiLuongKhamPhut BETWEEN 5 AND 240");

            migrationBuilder.CreateIndex(
                name: "IX_LichKham_ThoiGian",
                table: "LichKham",
                column: "ThoiGian");

            migrationBuilder.CreateIndex(
                name: "IX_LichKham_TrangThai",
                table: "LichKham",
                column: "TrangThai");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LichKham_TrangThai",
                table: "LichKham",
                sql: "TrangThai IN ('ChoXacNhan','DaXacNhan','DangKham','HoanThanh','DaHuy','VangMat')");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDon_PhieuKhamId",
                table: "HoaDon",
                column: "PhieuKhamId",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_HoaDon_PhuongThuc",
                table: "HoaDon",
                sql: "PhuongThuc IS NULL OR PhuongThuc IN ('TienMat','ChuyenKhoan','Online (MoMo)','Online (VNPay)','Online (ZaloPay)','Online')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HoaDon_TongTien",
                table: "HoaDon",
                sql: "TongTien >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HoaDon_TrangThaiThanhToan",
                table: "HoaDon",
                sql: "TrangThaiThanhToan IN ('ChuaThanhToan','DaThanhToan','DangXuLy','QuaHan','ThanhToanThatBai','DaHuy')");

            migrationBuilder.CreateIndex(
                name: "IX_DonThuoc_PhieuKhamId",
                table: "DonThuoc",
                column: "PhieuKhamId",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_DichVu_Gia",
                table: "DichVu",
                sql: "Gia >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_DanhGia_LichKhamId",
                table: "DanhGia",
                column: "LichKhamId",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_DanhGia_SoSao",
                table: "DanhGia",
                sql: "SoSao BETWEEN 1 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChiTietHoaDon_SoTien",
                table: "ChiTietHoaDon",
                sql: "SoTien >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChiTietDonThuoc_SoLuong",
                table: "ChiTietDonThuoc",
                sql: "SoLuong > 0");

            migrationBuilder.CreateIndex(
                name: "IX_BenhNhan_SoBHYT",
                table: "BenhNhan",
                column: "SoBHYT",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BenhNhan_SoCCCD",
                table: "BenhNhan",
                column: "SoCCCD",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BenhNhan_XoaBoiId",
                table: "BenhNhan",
                column: "XoaBoiId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BenhNhan_NgaySinh",
                table: "BenhNhan",
                sql: "NgaySinh <= CURRENT_TIMESTAMP");

            migrationBuilder.CreateIndex(
                name: "IX_BacSi_XoaBoiId",
                table: "BacSi",
                column: "XoaBoiId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BacSi_ChucVu",
                table: "BacSi",
                sql: "ChucVu IN ('Bác sĩ','Phó trưởng khoa','Trưởng khoa')");

            migrationBuilder.AddForeignKey(
                name: "FK_BacSi_NguoiDung_NguoiDungId",
                table: "BacSi",
                column: "NguoiDungId",
                principalTable: "NguoiDung",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BacSi_NguoiDung_XoaBoiId",
                table: "BacSi",
                column: "XoaBoiId",
                principalTable: "NguoiDung",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BenhNhan_NguoiDung_NguoiDungId",
                table: "BenhNhan",
                column: "NguoiDungId",
                principalTable: "NguoiDung",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BenhNhan_NguoiDung_XoaBoiId",
                table: "BenhNhan",
                column: "XoaBoiId",
                principalTable: "NguoiDung",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChiSoSucKhoeTuDo_BenhNhan_BenhNhanId",
                table: "ChiSoSucKhoeTuDo",
                column: "BenhNhanId",
                principalTable: "BenhNhan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DanhGia_BacSi_BacSiId",
                table: "DanhGia",
                column: "BacSiId",
                principalTable: "BacSi",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DanhGia_LichKham_LichKhamId",
                table: "DanhGia",
                column: "LichKhamId",
                principalTable: "LichKham",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_DonThuoc_PhieuKham_PhieuKhamId",
                table: "DonThuoc",
                column: "PhieuKhamId",
                principalTable: "PhieuKham",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HoaDon_PhieuKham_PhieuKhamId",
                table: "HoaDon",
                column: "PhieuKhamId",
                principalTable: "PhieuKham",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NguoiDung_NguoiDung_XoaBoiId",
                table: "NguoiDung",
                column: "XoaBoiId",
                principalTable: "NguoiDung",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NguoiThan_BenhNhan_BenhNhanId",
                table: "NguoiThan",
                column: "BenhNhanId",
                principalTable: "BenhNhan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PhieuKham_LichKham_LichKhamId",
                table: "PhieuKham",
                column: "LichKhamId",
                principalTable: "LichKham",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TaiLieuBenhNhan_BenhNhan_BenhNhanId",
                table: "TaiLieuBenhNhan",
                column: "BenhNhanId",
                principalTable: "BenhNhan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TiemChung_BenhNhan_BenhNhanId",
                table: "TiemChung",
                column: "BenhNhanId",
                principalTable: "BenhNhan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TienSuGiaDinh_BenhNhan_BenhNhanId",
                table: "TienSuGiaDinh",
                column: "BenhNhanId",
                principalTable: "BenhNhan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BacSi_NguoiDung_NguoiDungId",
                table: "BacSi");

            migrationBuilder.DropForeignKey(
                name: "FK_BacSi_NguoiDung_XoaBoiId",
                table: "BacSi");

            migrationBuilder.DropForeignKey(
                name: "FK_BenhNhan_NguoiDung_NguoiDungId",
                table: "BenhNhan");

            migrationBuilder.DropForeignKey(
                name: "FK_BenhNhan_NguoiDung_XoaBoiId",
                table: "BenhNhan");

            migrationBuilder.DropForeignKey(
                name: "FK_ChiSoSucKhoeTuDo_BenhNhan_BenhNhanId",
                table: "ChiSoSucKhoeTuDo");

            migrationBuilder.DropForeignKey(
                name: "FK_DanhGia_BacSi_BacSiId",
                table: "DanhGia");

            migrationBuilder.DropForeignKey(
                name: "FK_DanhGia_LichKham_LichKhamId",
                table: "DanhGia");

            migrationBuilder.DropForeignKey(
                name: "FK_DonThuoc_PhieuKham_PhieuKhamId",
                table: "DonThuoc");

            migrationBuilder.DropForeignKey(
                name: "FK_HoaDon_PhieuKham_PhieuKhamId",
                table: "HoaDon");

            migrationBuilder.DropForeignKey(
                name: "FK_NguoiDung_NguoiDung_XoaBoiId",
                table: "NguoiDung");

            migrationBuilder.DropForeignKey(
                name: "FK_NguoiThan_BenhNhan_BenhNhanId",
                table: "NguoiThan");

            migrationBuilder.DropForeignKey(
                name: "FK_PhieuKham_LichKham_LichKhamId",
                table: "PhieuKham");

            migrationBuilder.DropForeignKey(
                name: "FK_TaiLieuBenhNhan_BenhNhan_BenhNhanId",
                table: "TaiLieuBenhNhan");

            migrationBuilder.DropForeignKey(
                name: "FK_TiemChung_BenhNhan_BenhNhanId",
                table: "TiemChung");

            migrationBuilder.DropForeignKey(
                name: "FK_TienSuGiaDinh_BenhNhan_BenhNhanId",
                table: "TienSuGiaDinh");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Thuoc_Gia",
                table: "Thuoc");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Thuoc_NguongToiThieu",
                table: "Thuoc");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Thuoc_TonKho",
                table: "Thuoc");

            migrationBuilder.DropIndex(
                name: "IX_PhieuKham_LichKhamId",
                table: "PhieuKham");

            migrationBuilder.DropIndex(
                name: "IX_NguoiDung_Email",
                table: "NguoiDung");

            migrationBuilder.DropIndex(
                name: "IX_NguoiDung_Sdt",
                table: "NguoiDung");

            migrationBuilder.DropIndex(
                name: "IX_NguoiDung_XoaBoiId",
                table: "NguoiDung");

            migrationBuilder.DropCheckConstraint(
                name: "CK_NguoiDung_TrangThai",
                table: "NguoiDung");

            migrationBuilder.DropCheckConstraint(
                name: "CK_NguoiDung_VaiTro",
                table: "NguoiDung");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LoThuoc_HanSuDung",
                table: "LoThuoc");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LoThuoc_SoLuongNhap",
                table: "LoThuoc");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LoThuoc_SoLuongTon",
                table: "LoThuoc");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LoThuoc_TonKhongVuotQuaNhap",
                table: "LoThuoc");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LichLamViecBacSi_GioKetThuc",
                table: "LichLamViecBacSi");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LichLamViecBacSi_SoBenhNhanToiDa",
                table: "LichLamViecBacSi");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LichLamViecBacSi_ThoiLuongKhamPhut",
                table: "LichLamViecBacSi");

            migrationBuilder.DropIndex(
                name: "IX_LichKham_ThoiGian",
                table: "LichKham");

            migrationBuilder.DropIndex(
                name: "IX_LichKham_TrangThai",
                table: "LichKham");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LichKham_TrangThai",
                table: "LichKham");

            migrationBuilder.DropIndex(
                name: "IX_HoaDon_PhieuKhamId",
                table: "HoaDon");

            migrationBuilder.DropCheckConstraint(
                name: "CK_HoaDon_PhuongThuc",
                table: "HoaDon");

            migrationBuilder.DropCheckConstraint(
                name: "CK_HoaDon_TongTien",
                table: "HoaDon");

            migrationBuilder.DropCheckConstraint(
                name: "CK_HoaDon_TrangThaiThanhToan",
                table: "HoaDon");

            migrationBuilder.DropIndex(
                name: "IX_DonThuoc_PhieuKhamId",
                table: "DonThuoc");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DichVu_Gia",
                table: "DichVu");

            migrationBuilder.DropIndex(
                name: "IX_DanhGia_LichKhamId",
                table: "DanhGia");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DanhGia_SoSao",
                table: "DanhGia");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ChiTietHoaDon_SoTien",
                table: "ChiTietHoaDon");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ChiTietDonThuoc_SoLuong",
                table: "ChiTietDonThuoc");

            migrationBuilder.DropIndex(
                name: "IX_BenhNhan_SoBHYT",
                table: "BenhNhan");

            migrationBuilder.DropIndex(
                name: "IX_BenhNhan_SoCCCD",
                table: "BenhNhan");

            migrationBuilder.DropIndex(
                name: "IX_BenhNhan_XoaBoiId",
                table: "BenhNhan");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BenhNhan_NgaySinh",
                table: "BenhNhan");

            migrationBuilder.DropIndex(
                name: "IX_BacSi_XoaBoiId",
                table: "BacSi");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BacSi_ChucVu",
                table: "BacSi");

            migrationBuilder.DropColumn(
                name: "DaXoa",
                table: "NguoiDung");

            migrationBuilder.DropColumn(
                name: "NgayXoa",
                table: "NguoiDung");

            migrationBuilder.DropColumn(
                name: "XoaBoiId",
                table: "NguoiDung");

            migrationBuilder.DropColumn(
                name: "NgayNhap",
                table: "LoThuoc");

            migrationBuilder.DropColumn(
                name: "LichKhamId",
                table: "DanhGia");

            migrationBuilder.DropColumn(
                name: "DaXoa",
                table: "BenhNhan");

            migrationBuilder.DropColumn(
                name: "NgayXoa",
                table: "BenhNhan");

            migrationBuilder.DropColumn(
                name: "XoaBoiId",
                table: "BenhNhan");

            migrationBuilder.DropColumn(
                name: "DaXoa",
                table: "BacSi");

            migrationBuilder.DropColumn(
                name: "NgayXoa",
                table: "BacSi");

            migrationBuilder.DropColumn(
                name: "XoaBoiId",
                table: "BacSi");

            migrationBuilder.AlterColumn<DateTime>(
                name: "NgayDang",
                table: "TinTuc",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "NgayGhiNhan",
                table: "TienSuGiaDinh",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "NgayGui",
                table: "ThongBao",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "NgayTaiLen",
                table: "TaiLieuBenhNhan",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "NgayKham",
                table: "PhieuKham",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CapNhatLuc",
                table: "PhanQuyenVaiTro",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ThoiGian",
                table: "NhatKyHeThong",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "NgayTao",
                table: "NguoiDung",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "NgayTao",
                table: "LichKham",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<string>(
                name: "PhuongThuc",
                table: "HoaDon",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "NgayTao",
                table: "HoaDon",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "NgayKe",
                table: "DonThuoc",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "NgayTao",
                table: "DanhGia",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "NgayTao",
                table: "ChiSoSucKhoeTuDo",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "NgayDo",
                table: "ChiSoSucKhoeTuDo",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuKham_LichKhamId",
                table: "PhieuKham",
                column: "LichKhamId");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDon_PhieuKhamId",
                table: "HoaDon",
                column: "PhieuKhamId");

            migrationBuilder.CreateIndex(
                name: "IX_DonThuoc_PhieuKhamId",
                table: "DonThuoc",
                column: "PhieuKhamId");

            migrationBuilder.AddForeignKey(
                name: "FK_BacSi_NguoiDung_NguoiDungId",
                table: "BacSi",
                column: "NguoiDungId",
                principalTable: "NguoiDung",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BenhNhan_NguoiDung_NguoiDungId",
                table: "BenhNhan",
                column: "NguoiDungId",
                principalTable: "NguoiDung",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChiSoSucKhoeTuDo_BenhNhan_BenhNhanId",
                table: "ChiSoSucKhoeTuDo",
                column: "BenhNhanId",
                principalTable: "BenhNhan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DanhGia_BacSi_BacSiId",
                table: "DanhGia",
                column: "BacSiId",
                principalTable: "BacSi",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DonThuoc_PhieuKham_PhieuKhamId",
                table: "DonThuoc",
                column: "PhieuKhamId",
                principalTable: "PhieuKham",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HoaDon_PhieuKham_PhieuKhamId",
                table: "HoaDon",
                column: "PhieuKhamId",
                principalTable: "PhieuKham",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NguoiThan_BenhNhan_BenhNhanId",
                table: "NguoiThan",
                column: "BenhNhanId",
                principalTable: "BenhNhan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PhieuKham_LichKham_LichKhamId",
                table: "PhieuKham",
                column: "LichKhamId",
                principalTable: "LichKham",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaiLieuBenhNhan_BenhNhan_BenhNhanId",
                table: "TaiLieuBenhNhan",
                column: "BenhNhanId",
                principalTable: "BenhNhan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TiemChung_BenhNhan_BenhNhanId",
                table: "TiemChung",
                column: "BenhNhanId",
                principalTable: "BenhNhan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TienSuGiaDinh_BenhNhan_BenhNhanId",
                table: "TienSuGiaDinh",
                column: "BenhNhanId",
                principalTable: "BenhNhan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
