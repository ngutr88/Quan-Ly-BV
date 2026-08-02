using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyBenhVien.Migrations
{
    /// <inheritdoc />
    public partial class ProfileApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DuocDuyetHoSoHanhNghe",
                table: "NguoiDung",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "GioiThieuNgan",
                table: "BacSi",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayCapCCHN",
                table: "BacSi",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySinh",
                table: "BacSi",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoiCapCCHN",
                table: "BacSi",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhamViHanhNghe",
                table: "BacSi",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuaTrinhDaoTao",
                table: "BacSi",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoCCHN",
                table: "BacSi",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "YeuCauThayDoiHoSo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BacSiId = table.Column<int>(type: "INTEGER", nullable: false),
                    NgayDeXuat = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TrangThai = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DuLieuCuJson = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    DuLieuMoiJson = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    NguoiDuyetId = table.Column<int>(type: "INTEGER", nullable: true),
                    NgayDuyet = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LyDoTuChoi = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YeuCauThayDoiHoSo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_YeuCauThayDoiHoSo_BacSi_BacSiId",
                        column: x => x.BacSiId,
                        principalTable: "BacSi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_YeuCauThayDoiHoSo_NguoiDung_NguoiDuyetId",
                        column: x => x.NguoiDuyetId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauThayDoiHoSo_BacSiId",
                table: "YeuCauThayDoiHoSo",
                column: "BacSiId");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauThayDoiHoSo_NguoiDuyetId",
                table: "YeuCauThayDoiHoSo",
                column: "NguoiDuyetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "YeuCauThayDoiHoSo");

            migrationBuilder.DropColumn(
                name: "DuocDuyetHoSoHanhNghe",
                table: "NguoiDung");

            migrationBuilder.DropColumn(
                name: "GioiThieuNgan",
                table: "BacSi");

            migrationBuilder.DropColumn(
                name: "NgayCapCCHN",
                table: "BacSi");

            migrationBuilder.DropColumn(
                name: "NgaySinh",
                table: "BacSi");

            migrationBuilder.DropColumn(
                name: "NoiCapCCHN",
                table: "BacSi");

            migrationBuilder.DropColumn(
                name: "PhamViHanhNghe",
                table: "BacSi");

            migrationBuilder.DropColumn(
                name: "QuaTrinhDaoTao",
                table: "BacSi");

            migrationBuilder.DropColumn(
                name: "SoCCHN",
                table: "BacSi");
        }
    }
}
