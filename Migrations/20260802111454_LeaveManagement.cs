using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyBenhVien.Migrations
{
    /// <inheritdoc />
    public partial class LeaveManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SoDuPhepNam",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BacSiId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nam = table.Column<int>(type: "INTEGER", nullable: false),
                    TongSoNgay = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    CongDonTuNamTruoc = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    DaDung = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    DaTamGiu = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoDuPhepNam", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SoDuPhepNam_BacSi_BacSiId",
                        column: x => x.BacSiId,
                        principalTable: "BacSi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "YeuCauNghiPhep",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BacSiId = table.Column<int>(type: "INTEGER", nullable: false),
                    TuNgay = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DenNgay = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Buoi = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    SoNgayTru = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    LoaiNghi = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    LyDo = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    DinhKemUrl = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    TrangThai = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    NguoiDuyetId = table.Column<int>(type: "INTEGER", nullable: true),
                    NgayDuyet = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LyDoTuChoi = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YeuCauNghiPhep", x => x.Id);
                    table.CheckConstraint("CK_YeuCauNghiPhep_Buoi", "Buoi IS NULL OR Buoi IN ('Sang','Chieu')");
                    table.CheckConstraint("CK_YeuCauNghiPhep_KhoangNgay", "DenNgay >= TuNgay");
                    table.CheckConstraint("CK_YeuCauNghiPhep_LoaiNghi", "LoaiNghi IN ('PhepNam','Om','ViecRieng','Khac')");
                    table.CheckConstraint("CK_YeuCauNghiPhep_TrangThai", "TrangThai IN ('ChoDuyet','DaDuyet','TuChoi')");
                    table.ForeignKey(
                        name: "FK_YeuCauNghiPhep_BacSi_BacSiId",
                        column: x => x.BacSiId,
                        principalTable: "BacSi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_YeuCauNghiPhep_NguoiDung_NguoiDuyetId",
                        column: x => x.NguoiDuyetId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SoDuPhepNam_BacSiId_Nam",
                table: "SoDuPhepNam",
                columns: new[] { "BacSiId", "Nam" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauNghiPhep_BacSiId",
                table: "YeuCauNghiPhep",
                column: "BacSiId");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauNghiPhep_NguoiDuyetId",
                table: "YeuCauNghiPhep",
                column: "NguoiDuyetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SoDuPhepNam");

            migrationBuilder.DropTable(
                name: "YeuCauNghiPhep");
        }
    }
}
