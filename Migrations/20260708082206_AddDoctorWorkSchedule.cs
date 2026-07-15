using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyBenhVien.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorWorkSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LichLamViecBacSi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BacSiId = table.Column<int>(type: "INTEGER", nullable: false),
                    ThuTrongTuan = table.Column<int>(type: "INTEGER", nullable: false),
                    GioBatDau = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    GioKetThuc = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    ThoiLuongKhamPhut = table.Column<int>(type: "INTEGER", nullable: false),
                    SoBenhNhanToiDa = table.Column<int>(type: "INTEGER", nullable: false),
                    PhongKham = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    HieuLucTu = table.Column<DateTime>(type: "TEXT", nullable: true),
                    HieuLucDen = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DangHoatDong = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichLamViecBacSi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LichLamViecBacSi_BacSi_BacSiId",
                        column: x => x.BacSiId,
                        principalTable: "BacSi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LichLamViecBacSi_BacSiId_ThuTrongTuan_GioBatDau_GioKetThuc",
                table: "LichLamViecBacSi",
                columns: new[] { "BacSiId", "ThuTrongTuan", "GioBatDau", "GioKetThuc" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LichLamViecBacSi");
        }
    }
}
