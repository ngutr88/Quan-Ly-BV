using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyBenhVien.Migrations
{
    /// <inheritdoc />
    public partial class AddArticles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The scaffolder also emitted a CreateTable for "TaiLieuBenhNhan"
            // because the model snapshot had not recorded the AddPatientDocuments
            // migration. That table is already created by that earlier migration,
            // so re-creating it here would fail on every existing database.
            migrationBuilder.CreateTable(
                name: "TinTuc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TieuDe = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TomTat = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    NoiDung = table.Column<string>(type: "TEXT", nullable: false),
                    ChuyenMuc = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AnhBia = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    TacGia = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DaXuatBan = table.Column<bool>(type: "INTEGER", nullable: false),
                    NoiBat = table.Column<bool>(type: "INTEGER", nullable: false),
                    LuotXem = table.Column<int>(type: "INTEGER", nullable: false),
                    NgayDang = table.Column<DateTime>(type: "TEXT", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TinTuc", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TinTuc");
        }
    }
}
