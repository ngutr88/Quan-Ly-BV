using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyBenhVien.Migrations
{
    /// <inheritdoc />
    public partial class PaymentTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GiaoDichThanhToanHienTaiId",
                table: "HoaDon",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GiaoDichThanhToan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NguoiKhoiTaoId = table.Column<int>(type: "INTEGER", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PhuongThuc = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TrangThai = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    MaGiaoDichCong = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "TEXT", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiaoDichThanhToan", x => x.Id);
                    table.CheckConstraint("CK_GiaoDichThanhToan_SoTien", "SoTien >= 0");
                    table.CheckConstraint("CK_GiaoDichThanhToan_TrangThai", "TrangThai IN ('ChoXuLy','DangXuLy','ThanhCong','ThatBai','DaHuy')");
                    table.ForeignKey(
                        name: "FK_GiaoDichThanhToan_NguoiDung_NguoiKhoiTaoId",
                        column: x => x.NguoiKhoiTaoId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HoaDon_GiaoDichThanhToanHienTaiId",
                table: "HoaDon",
                column: "GiaoDichThanhToanHienTaiId");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichThanhToan_IdempotencyKey",
                table: "GiaoDichThanhToan",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichThanhToan_MaGiaoDichCong",
                table: "GiaoDichThanhToan",
                column: "MaGiaoDichCong",
                unique: true,
                filter: "[MaGiaoDichCong] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichThanhToan_NguoiKhoiTaoId",
                table: "GiaoDichThanhToan",
                column: "NguoiKhoiTaoId");

            migrationBuilder.AddForeignKey(
                name: "FK_HoaDon_GiaoDichThanhToan_GiaoDichThanhToanHienTaiId",
                table: "HoaDon",
                column: "GiaoDichThanhToanHienTaiId",
                principalTable: "GiaoDichThanhToan",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HoaDon_GiaoDichThanhToan_GiaoDichThanhToanHienTaiId",
                table: "HoaDon");

            migrationBuilder.DropTable(
                name: "GiaoDichThanhToan");

            migrationBuilder.DropIndex(
                name: "IX_HoaDon_GiaoDichThanhToanHienTaiId",
                table: "HoaDon");

            migrationBuilder.DropColumn(
                name: "GiaoDichThanhToanHienTaiId",
                table: "HoaDon");
        }
    }
}
