using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyBenhVien.Migrations
{
    /// <inheritdoc />
    public partial class LabDiagnosticsCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GhiChuCLSCuaBacSi",
                table: "PhieuKham",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DuocXuLyCLS",
                table: "NguoiDung",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "BoChiDinhCLS",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenBo = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    MoTa = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DangHoatDong = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoChiDinhCLS", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DichVuCLS",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MaDichVu = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    TenDichVu = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    NhomCLS = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    NoiThucHien = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    Gia = table.Column<decimal>(type: "decimal(12, 2)", nullable: false),
                    DangHoatDong = table.Column<bool>(type: "INTEGER", nullable: false),
                    GhiChu = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DichVuCLS", x => x.Id);
                    table.CheckConstraint("CK_DichVuCLS_Gia", "Gia >= 0");
                    table.CheckConstraint("CK_DichVuCLS_NhomCLS", "NhomCLS IN ('HuyetHoc','SinhHoa','ViSinh','CDHA','ThamDoChucNang','GiaiPhauBenh')");
                });

            migrationBuilder.CreateTable(
                name: "PhieuChiDinhCLS",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MaPhieu = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    PhieuKhamId = table.Column<int>(type: "INTEGER", nullable: false),
                    BenhNhanId = table.Column<int>(type: "INTEGER", nullable: false),
                    BacSiChiDinhId = table.Column<int>(type: "INTEGER", nullable: false),
                    NgayChiDinh = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    BoChiDinhCLSId = table.Column<int>(type: "INTEGER", nullable: true),
                    GhiChuChiDinh = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuChiDinhCLS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhieuChiDinhCLS_BacSi_BacSiChiDinhId",
                        column: x => x.BacSiChiDinhId,
                        principalTable: "BacSi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuChiDinhCLS_BenhNhan_BenhNhanId",
                        column: x => x.BenhNhanId,
                        principalTable: "BenhNhan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuChiDinhCLS_BoChiDinhCLS_BoChiDinhCLSId",
                        column: x => x.BoChiDinhCLSId,
                        principalTable: "BoChiDinhCLS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuChiDinhCLS_PhieuKham_PhieuKhamId",
                        column: x => x.PhieuKhamId,
                        principalTable: "PhieuKham",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietBoChiDinhCLS",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BoChiDinhCLSId = table.Column<int>(type: "INTEGER", nullable: false),
                    DichVuCLSId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietBoChiDinhCLS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChiTietBoChiDinhCLS_BoChiDinhCLS_BoChiDinhCLSId",
                        column: x => x.BoChiDinhCLSId,
                        principalTable: "BoChiDinhCLS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiTietBoChiDinhCLS_DichVuCLS_DichVuCLSId",
                        column: x => x.DichVuCLSId,
                        principalTable: "DichVuCLS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietPhieuChiDinhCLS",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PhieuChiDinhCLSId = table.Column<int>(type: "INTEGER", nullable: false),
                    DichVuCLSId = table.Column<int>(type: "INTEGER", nullable: false),
                    TrangThai = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    LyDoHuy = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    NgayNhanThucHien = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NguoiNhanId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietPhieuChiDinhCLS", x => x.Id);
                    table.CheckConstraint("CK_ChiTietPhieuChiDinhCLS_TrangThai", "TrangThai IN ('ChoThucHien','DangThucHien','DaCoKetQua','DaHuy')");
                    table.ForeignKey(
                        name: "FK_ChiTietPhieuChiDinhCLS_DichVuCLS_DichVuCLSId",
                        column: x => x.DichVuCLSId,
                        principalTable: "DichVuCLS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietPhieuChiDinhCLS_NguoiDung_NguoiNhanId",
                        column: x => x.NguoiNhanId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietPhieuChiDinhCLS_PhieuChiDinhCLS_PhieuChiDinhCLSId",
                        column: x => x.PhieuChiDinhCLSId,
                        principalTable: "PhieuChiDinhCLS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KetQuaCLS",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChiTietPhieuChiDinhCLSId = table.Column<int>(type: "INTEGER", nullable: false),
                    KetLuan = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CoBatThuong = table.Column<bool>(type: "INTEGER", nullable: false),
                    NguoiThucHienId = table.Column<int>(type: "INTEGER", nullable: false),
                    NguoiDuyetTen = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    NgayTra = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DaXem = table.Column<bool>(type: "INTEGER", nullable: false),
                    NgayXem = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KetQuaCLS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KetQuaCLS_ChiTietPhieuChiDinhCLS_ChiTietPhieuChiDinhCLSId",
                        column: x => x.ChiTietPhieuChiDinhCLSId,
                        principalTable: "ChiTietPhieuChiDinhCLS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KetQuaCLS_NguoiDung_NguoiThucHienId",
                        column: x => x.NguoiThucHienId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FileKetQuaCLS",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KetQuaCLSId = table.Column<int>(type: "INTEGER", nullable: false),
                    TenGoc = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    TenLuuTru = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    KichThuoc = table.Column<long>(type: "INTEGER", nullable: false),
                    ThuTu = table.Column<int>(type: "INTEGER", nullable: false),
                    NgayTaiLen = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileKetQuaCLS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileKetQuaCLS_KetQuaCLS_KetQuaCLSId",
                        column: x => x.KetQuaCLSId,
                        principalTable: "KetQuaCLS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietBoChiDinhCLS_BoChiDinhCLSId_DichVuCLSId",
                table: "ChiTietBoChiDinhCLS",
                columns: new[] { "BoChiDinhCLSId", "DichVuCLSId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietBoChiDinhCLS_DichVuCLSId",
                table: "ChiTietBoChiDinhCLS",
                column: "DichVuCLSId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuChiDinhCLS_DichVuCLSId",
                table: "ChiTietPhieuChiDinhCLS",
                column: "DichVuCLSId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuChiDinhCLS_NguoiNhanId",
                table: "ChiTietPhieuChiDinhCLS",
                column: "NguoiNhanId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuChiDinhCLS_PhieuChiDinhCLSId",
                table: "ChiTietPhieuChiDinhCLS",
                column: "PhieuChiDinhCLSId");

            migrationBuilder.CreateIndex(
                name: "IX_DichVuCLS_MaDichVu",
                table: "DichVuCLS",
                column: "MaDichVu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileKetQuaCLS_KetQuaCLSId",
                table: "FileKetQuaCLS",
                column: "KetQuaCLSId");

            migrationBuilder.CreateIndex(
                name: "IX_KetQuaCLS_ChiTietPhieuChiDinhCLSId",
                table: "KetQuaCLS",
                column: "ChiTietPhieuChiDinhCLSId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KetQuaCLS_NguoiThucHienId",
                table: "KetQuaCLS",
                column: "NguoiThucHienId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuChiDinhCLS_BacSiChiDinhId",
                table: "PhieuChiDinhCLS",
                column: "BacSiChiDinhId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuChiDinhCLS_BenhNhanId",
                table: "PhieuChiDinhCLS",
                column: "BenhNhanId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuChiDinhCLS_BoChiDinhCLSId",
                table: "PhieuChiDinhCLS",
                column: "BoChiDinhCLSId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuChiDinhCLS_MaPhieu",
                table: "PhieuChiDinhCLS",
                column: "MaPhieu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhieuChiDinhCLS_PhieuKhamId",
                table: "PhieuChiDinhCLS",
                column: "PhieuKhamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChiTietBoChiDinhCLS");

            migrationBuilder.DropTable(
                name: "FileKetQuaCLS");

            migrationBuilder.DropTable(
                name: "KetQuaCLS");

            migrationBuilder.DropTable(
                name: "ChiTietPhieuChiDinhCLS");

            migrationBuilder.DropTable(
                name: "DichVuCLS");

            migrationBuilder.DropTable(
                name: "PhieuChiDinhCLS");

            migrationBuilder.DropTable(
                name: "BoChiDinhCLS");

            migrationBuilder.DropColumn(
                name: "GhiChuCLSCuaBacSi",
                table: "PhieuKham");

            migrationBuilder.DropColumn(
                name: "DuocXuLyCLS",
                table: "NguoiDung");
        }
    }
}
