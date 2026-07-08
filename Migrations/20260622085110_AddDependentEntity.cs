using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyBenhVien.Migrations
{
    /// <inheritdoc />
    public partial class AddDependentEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Khoa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenKhoa = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    MoTa = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ViTri = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Khoa", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NguoiDung",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HoTen = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Sdt = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    MatKhauHash = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    VaiTro = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TrangThai = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NguoiDung", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Thuoc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenThuoc = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    HoatChat = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    DonViTinh = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Gia = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    TonKho = table.Column<int>(type: "INTEGER", nullable: false),
                    NguongToiThieu = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Thuoc", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DichVu",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KhoaId = table.Column<int>(type: "INTEGER", nullable: false),
                    TenDichVu = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Gia = table.Column<decimal>(type: "decimal(18, 2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DichVu", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DichVu_Khoa_KhoaId",
                        column: x => x.KhoaId,
                        principalTable: "Khoa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BacSi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NguoiDungId = table.Column<int>(type: "INTEGER", nullable: false),
                    KhoaId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChuyenKhoa = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    HocVi = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SoNamKinhNghiem = table.Column<int>(type: "INTEGER", nullable: false),
                    LichLamViec = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BacSi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BacSi_Khoa_KhoaId",
                        column: x => x.KhoaId,
                        principalTable: "Khoa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BacSi_NguoiDung_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BenhNhan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NguoiDungId = table.Column<int>(type: "INTEGER", nullable: false),
                    NgaySinh = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GioiTinh = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    NhomMau = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    SoBHYT = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TienSuBenh = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DiUng = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenhNhan", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BenhNhan_NguoiDung_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NhatKyHeThong",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NguoiDungId = table.Column<int>(type: "INTEGER", nullable: true),
                    HanhDong = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ChiTiet = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhatKyHeThong", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NhatKyHeThong_NguoiDung_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ThongBao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NguoiDungId = table.Column<int>(type: "INTEGER", nullable: false),
                    NoiDung = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    NgayGui = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DaDoc = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThongBao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThongBao_NguoiDung_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoThuoc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ThuocId = table.Column<int>(type: "INTEGER", nullable: false),
                    SoLo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    HanSuDung = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SoLuongNhap = table.Column<int>(type: "INTEGER", nullable: false),
                    SoLuongTon = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoThuoc", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoThuoc_Thuoc_ThuocId",
                        column: x => x.ThuocId,
                        principalTable: "Thuoc",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DanhGia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BenhNhanId = table.Column<int>(type: "INTEGER", nullable: false),
                    BacSiId = table.Column<int>(type: "INTEGER", nullable: false),
                    SoSao = table.Column<int>(type: "INTEGER", nullable: false),
                    NhanXet = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhGia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DanhGia_BacSi_BacSiId",
                        column: x => x.BacSiId,
                        principalTable: "BacSi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DanhGia_BenhNhan_BenhNhanId",
                        column: x => x.BenhNhanId,
                        principalTable: "BenhNhan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LichKham",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BenhNhanId = table.Column<int>(type: "INTEGER", nullable: false),
                    BacSiId = table.Column<int>(type: "INTEGER", nullable: true),
                    ThoiGian = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TrangThai = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LyDoKham = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichKham", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LichKham_BacSi_BacSiId",
                        column: x => x.BacSiId,
                        principalTable: "BacSi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LichKham_BenhNhan_BenhNhanId",
                        column: x => x.BenhNhanId,
                        principalTable: "BenhNhan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NguoiThan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BenhNhanId = table.Column<int>(type: "INTEGER", nullable: false),
                    HoTen = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    QuanHe = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    GioiTinh = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    NamSinh = table.Column<int>(type: "INTEGER", nullable: false),
                    NhomMau = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    SoBHYT = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TienSuBenhLy = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NguoiThan", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NguoiThan_BenhNhan_BenhNhanId",
                        column: x => x.BenhNhanId,
                        principalTable: "BenhNhan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhieuKham",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LichKhamId = table.Column<int>(type: "INTEGER", nullable: false),
                    TrieuChung = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    HuyetAp = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    NhipTim = table.Column<int>(type: "INTEGER", nullable: true),
                    NhietDo = table.Column<decimal>(type: "decimal(4, 1)", nullable: true),
                    CanNang = table.Column<decimal>(type: "decimal(5, 2)", nullable: true),
                    ChieuCao = table.Column<decimal>(type: "decimal(5, 2)", nullable: true),
                    BMI = table.Column<decimal>(type: "decimal(4, 2)", nullable: true),
                    ChanDoan = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    LoiDan = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ChiDinhCLS = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    KetQuaCLS = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    NgayKham = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuKham", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhieuKham_LichKham_LichKhamId",
                        column: x => x.LichKhamId,
                        principalTable: "LichKham",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DonThuoc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PhieuKhamId = table.Column<int>(type: "INTEGER", nullable: false),
                    NgayKe = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonThuoc", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DonThuoc_PhieuKham_PhieuKhamId",
                        column: x => x.PhieuKhamId,
                        principalTable: "PhieuKham",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HoaDon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PhieuKhamId = table.Column<int>(type: "INTEGER", nullable: false),
                    TongTien = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    TrangThaiThanhToan = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PhuongThuc = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "TEXT", nullable: false),
                    NgayThanhToan = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoaDon", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HoaDon_PhieuKham_PhieuKhamId",
                        column: x => x.PhieuKhamId,
                        principalTable: "PhieuKham",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietDonThuoc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DonThuocId = table.Column<int>(type: "INTEGER", nullable: false),
                    ThuocId = table.Column<int>(type: "INTEGER", nullable: false),
                    LieuDung = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SoLuong = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietDonThuoc", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChiTietDonThuoc_DonThuoc_DonThuocId",
                        column: x => x.DonThuocId,
                        principalTable: "DonThuoc",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiTietDonThuoc_Thuoc_ThuocId",
                        column: x => x.ThuocId,
                        principalTable: "Thuoc",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietHoaDon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HoaDonId = table.Column<int>(type: "INTEGER", nullable: false),
                    LoaiPhi = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(18, 2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietHoaDon", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChiTietHoaDon_HoaDon_HoaDonId",
                        column: x => x.HoaDonId,
                        principalTable: "HoaDon",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BacSi_KhoaId",
                table: "BacSi",
                column: "KhoaId");

            migrationBuilder.CreateIndex(
                name: "IX_BacSi_NguoiDungId",
                table: "BacSi",
                column: "NguoiDungId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BenhNhan_NguoiDungId",
                table: "BenhNhan",
                column: "NguoiDungId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDonThuoc_DonThuocId",
                table: "ChiTietDonThuoc",
                column: "DonThuocId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDonThuoc_ThuocId",
                table: "ChiTietDonThuoc",
                column: "ThuocId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietHoaDon_HoaDonId",
                table: "ChiTietHoaDon",
                column: "HoaDonId");

            migrationBuilder.CreateIndex(
                name: "IX_DanhGia_BacSiId",
                table: "DanhGia",
                column: "BacSiId");

            migrationBuilder.CreateIndex(
                name: "IX_DanhGia_BenhNhanId",
                table: "DanhGia",
                column: "BenhNhanId");

            migrationBuilder.CreateIndex(
                name: "IX_DichVu_KhoaId",
                table: "DichVu",
                column: "KhoaId");

            migrationBuilder.CreateIndex(
                name: "IX_DonThuoc_PhieuKhamId",
                table: "DonThuoc",
                column: "PhieuKhamId");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDon_PhieuKhamId",
                table: "HoaDon",
                column: "PhieuKhamId");

            migrationBuilder.CreateIndex(
                name: "IX_LichKham_BacSiId",
                table: "LichKham",
                column: "BacSiId");

            migrationBuilder.CreateIndex(
                name: "IX_LichKham_BenhNhanId",
                table: "LichKham",
                column: "BenhNhanId");

            migrationBuilder.CreateIndex(
                name: "IX_LoThuoc_ThuocId",
                table: "LoThuoc",
                column: "ThuocId");

            migrationBuilder.CreateIndex(
                name: "IX_NguoiThan_BenhNhanId",
                table: "NguoiThan",
                column: "BenhNhanId");

            migrationBuilder.CreateIndex(
                name: "IX_NhatKyHeThong_NguoiDungId",
                table: "NhatKyHeThong",
                column: "NguoiDungId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuKham_LichKhamId",
                table: "PhieuKham",
                column: "LichKhamId");

            migrationBuilder.CreateIndex(
                name: "IX_ThongBao_NguoiDungId",
                table: "ThongBao",
                column: "NguoiDungId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChiTietDonThuoc");

            migrationBuilder.DropTable(
                name: "ChiTietHoaDon");

            migrationBuilder.DropTable(
                name: "DanhGia");

            migrationBuilder.DropTable(
                name: "DichVu");

            migrationBuilder.DropTable(
                name: "LoThuoc");

            migrationBuilder.DropTable(
                name: "NguoiThan");

            migrationBuilder.DropTable(
                name: "NhatKyHeThong");

            migrationBuilder.DropTable(
                name: "ThongBao");

            migrationBuilder.DropTable(
                name: "DonThuoc");

            migrationBuilder.DropTable(
                name: "HoaDon");

            migrationBuilder.DropTable(
                name: "Thuoc");

            migrationBuilder.DropTable(
                name: "PhieuKham");

            migrationBuilder.DropTable(
                name: "LichKham");

            migrationBuilder.DropTable(
                name: "BacSi");

            migrationBuilder.DropTable(
                name: "BenhNhan");

            migrationBuilder.DropTable(
                name: "Khoa");

            migrationBuilder.DropTable(
                name: "NguoiDung");
        }
    }
}
