using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyBenhVien.Migrations
{
    /// <inheritdoc />
    public partial class PatientEmrPhase1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DoiTuongId",
                table: "NhatKyHeThong",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DoiTuongLoai",
                table: "NhatKyHeThong",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnhDaiDien",
                table: "NguoiDung",
                type: "TEXT",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayHetHanBHYT",
                table: "BenhNhan",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TiemChung",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BenhNhanId = table.Column<int>(type: "INTEGER", nullable: false),
                    TenVaccine = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    NgayTiem = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MuiSo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    GhiChu = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiemChung", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TiemChung_BenhNhan_BenhNhanId",
                        column: x => x.BenhNhanId,
                        principalTable: "BenhNhan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TienSuGiaDinh",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BenhNhanId = table.Column<int>(type: "INTEGER", nullable: false),
                    QuanHe = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TenBenh = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    GhiChu = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    NgayGhiNhan = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TienSuGiaDinh", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TienSuGiaDinh_BenhNhan_BenhNhanId",
                        column: x => x.BenhNhanId,
                        principalTable: "BenhNhan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TiemChung_BenhNhanId",
                table: "TiemChung",
                column: "BenhNhanId");

            migrationBuilder.CreateIndex(
                name: "IX_TienSuGiaDinh_BenhNhanId",
                table: "TienSuGiaDinh",
                column: "BenhNhanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TiemChung");

            migrationBuilder.DropTable(
                name: "TienSuGiaDinh");

            migrationBuilder.DropColumn(
                name: "DoiTuongId",
                table: "NhatKyHeThong");

            migrationBuilder.DropColumn(
                name: "DoiTuongLoai",
                table: "NhatKyHeThong");

            migrationBuilder.DropColumn(
                name: "AnhDaiDien",
                table: "NguoiDung");

            migrationBuilder.DropColumn(
                name: "NgayHetHanBHYT",
                table: "BenhNhan");
        }
    }
}
