using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyBenhVien.Migrations
{
    /// <inheritdoc />
    public partial class AddRolePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "GhiChu",
                table: "ChiSoSucKhoeTuDo",
                type: "TEXT",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 300);

            migrationBuilder.CreateTable(
                name: "PhanQuyenVaiTro",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VaiTro = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ModuleKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DuocPhep = table.Column<bool>(type: "INTEGER", nullable: false),
                    CapNhatLuc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CapNhatBoiId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhanQuyenVaiTro", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhanQuyenVaiTro_NguoiDung_CapNhatBoiId",
                        column: x => x.CapNhatBoiId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhanQuyenVaiTro_CapNhatBoiId",
                table: "PhanQuyenVaiTro",
                column: "CapNhatBoiId");

            migrationBuilder.CreateIndex(
                name: "IX_PhanQuyenVaiTro_VaiTro_ModuleKey",
                table: "PhanQuyenVaiTro",
                columns: new[] { "VaiTro", "ModuleKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhanQuyenVaiTro");

            migrationBuilder.AlterColumn<string>(
                name: "GhiChu",
                table: "ChiSoSucKhoeTuDo",
                type: "TEXT",
                maxLength: 300,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 300,
                oldNullable: true);
        }
    }
}
