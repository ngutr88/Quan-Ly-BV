using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyBenhVien.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientHealthMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChiSoSucKhoeTuDo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BenhNhanId = table.Column<int>(type: "INTEGER", nullable: false),
                    NgayDo = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CanNang = table.Column<decimal>(type: "TEXT", nullable: true),
                    ChieuCao = table.Column<decimal>(type: "TEXT", nullable: true),
                    HuyetApTamThu = table.Column<int>(type: "INTEGER", nullable: true),
                    HuyetApTamTruong = table.Column<int>(type: "INTEGER", nullable: true),
                    NhipTim = table.Column<int>(type: "INTEGER", nullable: true),
                    DuongHuyet = table.Column<decimal>(type: "TEXT", nullable: true),
                    GhiChu = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiSoSucKhoeTuDo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChiSoSucKhoeTuDo_BenhNhan_BenhNhanId",
                        column: x => x.BenhNhanId,
                        principalTable: "BenhNhan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
            migrationBuilder.CreateIndex(
                name: "IX_ChiSoSucKhoeTuDo_BenhNhanId",
                table: "ChiSoSucKhoeTuDo",
                column: "BenhNhanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ChiSoSucKhoeTuDo");
        }
    }
}
