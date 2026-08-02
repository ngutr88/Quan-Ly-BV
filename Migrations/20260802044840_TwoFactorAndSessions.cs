using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyBenhVien.Migrations
{
    /// <inheritdoc />
    public partial class TwoFactorAndSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SidebarThuGonMacDinh",
                table: "NguoiDung",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SoDongMoiTrangMacDinh",
                table: "NguoiDung",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TotpBatDau",
                table: "NguoiDung",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TotpBiMat",
                table: "NguoiDung",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MaDuPhongTOTP",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NguoiDungId = table.Column<int>(type: "INTEGER", nullable: false),
                    MaHash = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DaDung = table.Column<bool>(type: "INTEGER", nullable: false),
                    NgayDung = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaDuPhongTOTP", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaDuPhongTOTP_NguoiDung_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhienDangNhap",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NguoiDungId = table.Column<int>(type: "INTEGER", nullable: false),
                    SessionToken = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ThietBi = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ThoiGianDangNhap = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ThoiGianHoatDongCuoi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TrangThai = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhienDangNhap", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhienDangNhap_NguoiDung_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaDuPhongTOTP_NguoiDungId",
                table: "MaDuPhongTOTP",
                column: "NguoiDungId");

            migrationBuilder.CreateIndex(
                name: "IX_PhienDangNhap_NguoiDungId",
                table: "PhienDangNhap",
                column: "NguoiDungId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaDuPhongTOTP");

            migrationBuilder.DropTable(
                name: "PhienDangNhap");

            migrationBuilder.DropColumn(
                name: "SidebarThuGonMacDinh",
                table: "NguoiDung");

            migrationBuilder.DropColumn(
                name: "SoDongMoiTrangMacDinh",
                table: "NguoiDung");

            migrationBuilder.DropColumn(
                name: "TotpBatDau",
                table: "NguoiDung");

            migrationBuilder.DropColumn(
                name: "TotpBiMat",
                table: "NguoiDung");
        }
    }
}
