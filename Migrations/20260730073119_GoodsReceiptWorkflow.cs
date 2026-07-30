using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyBenhVien.Migrations
{
    /// <inheritdoc />
    public partial class GoodsReceiptWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HamLuong",
                table: "Thuoc",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuyCachDongGoi",
                table: "Thuoc",
                type: "TEXT",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DuocDuyetPhieuNhapKho",
                table: "NguoiDung",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "GiaNhap",
                table: "LoThuoc",
                type: "decimal(18, 2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NhaCungCapId",
                table: "LoThuoc",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NhaCungCap",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenNhaCungCap = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    MaSoThue = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    DiaChi = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    SoDienThoai = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    NguoiLienHe = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DangHoatDong = table.Column<bool>(type: "INTEGER", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhaCungCap", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhieuNhapKho",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MaPhieu = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    NgayNhap = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LoaiNhap = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    NhaCungCapId = table.Column<int>(type: "INTEGER", nullable: true),
                    SoHoaDonNCC = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    NgayHoaDon = table.Column<DateTime>(type: "TEXT", nullable: true),
                    KhoNhap = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    NguoiGiaoHang = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    GhiChu = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    TrangThai = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    NguoiTaoId = table.Column<int>(type: "INTEGER", nullable: false),
                    NguoiDuyetId = table.Column<int>(type: "INTEGER", nullable: true),
                    NgayDuyet = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LyDoTuChoi = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PhieuGocId = table.Column<int>(type: "INTEGER", nullable: true),
                    TongSoMatHang = table.Column<int>(type: "INTEGER", nullable: false),
                    TongTienTruocVAT = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    TienVAT = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    TongThanhToan = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    NgayCapNhat = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuNhapKho", x => x.Id);
                    table.CheckConstraint("CK_PhieuNhapKho_KhoNhap", "KhoNhap IN ('KhoChan','KhoLe','NhaThuoc')");
                    table.CheckConstraint("CK_PhieuNhapKho_LoaiNhap", "LoaiNhap IN ('MuaNCC','ChuyenKho','HangTraVe','VienTro')");
                    table.CheckConstraint("CK_PhieuNhapKho_TienVAT", "TienVAT >= 0");
                    table.CheckConstraint("CK_PhieuNhapKho_TongSoMatHang", "TongSoMatHang >= 0");
                    table.CheckConstraint("CK_PhieuNhapKho_TongThanhToan", "TongThanhToan >= 0");
                    table.CheckConstraint("CK_PhieuNhapKho_TongTienTruocVAT", "TongTienTruocVAT >= 0");
                    table.CheckConstraint("CK_PhieuNhapKho_TrangThai", "TrangThai IN ('Nhap','ChoDuyet','DaDuyet','TuChoi','DaHuy')");
                    table.ForeignKey(
                        name: "FK_PhieuNhapKho_NguoiDung_NguoiDuyetId",
                        column: x => x.NguoiDuyetId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuNhapKho_NguoiDung_NguoiTaoId",
                        column: x => x.NguoiTaoId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuNhapKho_NhaCungCap_NhaCungCapId",
                        column: x => x.NhaCungCapId,
                        principalTable: "NhaCungCap",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuNhapKho_PhieuNhapKho_PhieuGocId",
                        column: x => x.PhieuGocId,
                        principalTable: "PhieuNhapKho",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PhieuNhapKhoChiTiet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PhieuNhapKhoId = table.Column<int>(type: "INTEGER", nullable: false),
                    ThuocId = table.Column<int>(type: "INTEGER", nullable: false),
                    SoLo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    HanSuDung = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SoLuong = table.Column<int>(type: "INTEGER", nullable: false),
                    DonGia = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    PhanTramVAT = table.Column<decimal>(type: "decimal(5, 2)", nullable: false),
                    ThanhTien = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    XacNhanCanDate = table.Column<bool>(type: "INTEGER", nullable: false),
                    CongDonVaoLoHienCo = table.Column<bool>(type: "INTEGER", nullable: false),
                    LoThuocId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuNhapKhoChiTiet", x => x.Id);
                    table.CheckConstraint("CK_PhieuNhapKhoChiTiet_DonGia", "DonGia >= 0");
                    table.CheckConstraint("CK_PhieuNhapKhoChiTiet_PhanTramVAT", "PhanTramVAT BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_PhieuNhapKhoChiTiet_SoLuong", "SoLuong > 0");
                    table.CheckConstraint("CK_PhieuNhapKhoChiTiet_ThanhTien", "ThanhTien >= 0");
                    table.ForeignKey(
                        name: "FK_PhieuNhapKhoChiTiet_LoThuoc_LoThuocId",
                        column: x => x.LoThuocId,
                        principalTable: "LoThuoc",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PhieuNhapKhoChiTiet_PhieuNhapKho_PhieuNhapKhoId",
                        column: x => x.PhieuNhapKhoId,
                        principalTable: "PhieuNhapKho",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhieuNhapKhoChiTiet_Thuoc_ThuocId",
                        column: x => x.ThuocId,
                        principalTable: "Thuoc",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoThuoc_NhaCungCapId",
                table: "LoThuoc",
                column: "NhaCungCapId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LoThuoc_GiaNhap",
                table: "LoThuoc",
                sql: "GiaNhap IS NULL OR GiaNhap >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_NhaCungCap_TenNhaCungCap",
                table: "NhaCungCap",
                column: "TenNhaCungCap");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuNhapKho_MaPhieu",
                table: "PhieuNhapKho",
                column: "MaPhieu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhieuNhapKho_NguoiDuyetId",
                table: "PhieuNhapKho",
                column: "NguoiDuyetId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuNhapKho_NguoiTaoId",
                table: "PhieuNhapKho",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuNhapKho_NhaCungCapId",
                table: "PhieuNhapKho",
                column: "NhaCungCapId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuNhapKho_PhieuGocId",
                table: "PhieuNhapKho",
                column: "PhieuGocId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuNhapKho_TrangThai",
                table: "PhieuNhapKho",
                column: "TrangThai");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuNhapKhoChiTiet_LoThuocId",
                table: "PhieuNhapKhoChiTiet",
                column: "LoThuocId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuNhapKhoChiTiet_PhieuNhapKhoId",
                table: "PhieuNhapKhoChiTiet",
                column: "PhieuNhapKhoId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuNhapKhoChiTiet_ThuocId",
                table: "PhieuNhapKhoChiTiet",
                column: "ThuocId");

            migrationBuilder.AddForeignKey(
                name: "FK_LoThuoc_NhaCungCap_NhaCungCapId",
                table: "LoThuoc",
                column: "NhaCungCapId",
                principalTable: "NhaCungCap",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LoThuoc_NhaCungCap_NhaCungCapId",
                table: "LoThuoc");

            migrationBuilder.DropTable(
                name: "PhieuNhapKhoChiTiet");

            migrationBuilder.DropTable(
                name: "PhieuNhapKho");

            migrationBuilder.DropTable(
                name: "NhaCungCap");

            migrationBuilder.DropIndex(
                name: "IX_LoThuoc_NhaCungCapId",
                table: "LoThuoc");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LoThuoc_GiaNhap",
                table: "LoThuoc");

            migrationBuilder.DropColumn(
                name: "HamLuong",
                table: "Thuoc");

            migrationBuilder.DropColumn(
                name: "QuyCachDongGoi",
                table: "Thuoc");

            migrationBuilder.DropColumn(
                name: "DuocDuyetPhieuNhapKho",
                table: "NguoiDung");

            migrationBuilder.DropColumn(
                name: "GiaNhap",
                table: "LoThuoc");

            migrationBuilder.DropColumn(
                name: "NhaCungCapId",
                table: "LoThuoc");
        }
    }
}
