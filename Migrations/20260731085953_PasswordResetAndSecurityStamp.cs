using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyBenhVien.Migrations
{
    /// <inheritdoc />
    public partial class PasswordResetAndSecurityStamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "MatKhauTamHetHan",
                table: "NguoiDung",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PhaiDoiMatKhau",
                table: "NguoiDung",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SecurityStamp",
                table: "NguoiDung",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<decimal>(
                name: "DuongHuyet",
                table: "ChiSoSucKhoeTuDo",
                type: "decimal(5, 2)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ChieuCao",
                table: "ChiSoSucKhoeTuDo",
                type: "decimal(5, 2)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CanNang",
                table: "ChiSoSucKhoeTuDo",
                type: "decimal(5, 2)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "YeuCauKhoiPhucMatKhau",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NguoiDungId = table.Column<int>(type: "INTEGER", nullable: true),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    KhoiTaoBoi = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Kenh = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    MaXacNhanHash = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ThoiHanMa = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SoLanNhapSai = table.Column<int>(type: "INTEGER", nullable: false),
                    MaDaDung = table.Column<bool>(type: "INTEGER", nullable: false),
                    SoLanGuiLai = table.Column<int>(type: "INTEGER", nullable: false),
                    LanGuiGanNhatLuc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResetTokenHash = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ThoiHanResetToken = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResetTokenDaDung = table.Column<bool>(type: "INTEGER", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YeuCauKhoiPhucMatKhau", x => x.Id);
                    table.CheckConstraint("CK_YeuCauKhoiPhucMatKhau_Kenh", "Kenh IS NULL OR Kenh IN ('Email','Sdt')");
                    table.CheckConstraint("CK_YeuCauKhoiPhucMatKhau_KhoiTaoBoi", "KhoiTaoBoi IN ('TuPhucVu','Admin')");
                    table.ForeignKey(
                        name: "FK_YeuCauKhoiPhucMatKhau_NguoiDung_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauKhoiPhucMatKhau_IpAddress_NgayTao",
                table: "YeuCauKhoiPhucMatKhau",
                columns: new[] { "IpAddress", "NgayTao" });

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauKhoiPhucMatKhau_NguoiDungId_NgayTao",
                table: "YeuCauKhoiPhucMatKhau",
                columns: new[] { "NguoiDungId", "NgayTao" });

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauKhoiPhucMatKhau_ResetTokenHash",
                table: "YeuCauKhoiPhucMatKhau",
                column: "ResetTokenHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "YeuCauKhoiPhucMatKhau");

            migrationBuilder.DropColumn(
                name: "MatKhauTamHetHan",
                table: "NguoiDung");

            migrationBuilder.DropColumn(
                name: "PhaiDoiMatKhau",
                table: "NguoiDung");

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "NguoiDung");

            migrationBuilder.AlterColumn<string>(
                name: "DuongHuyet",
                table: "ChiSoSucKhoeTuDo",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5, 2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ChieuCao",
                table: "ChiSoSucKhoeTuDo",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5, 2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CanNang",
                table: "ChiSoSucKhoeTuDo",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5, 2)",
                oldNullable: true);
        }
    }
}
