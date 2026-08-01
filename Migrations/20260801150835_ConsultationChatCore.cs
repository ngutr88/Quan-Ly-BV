using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyBenhVien.Migrations
{
    /// <inheritdoc />
    public partial class ConsultationChatCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HoiThoaiTuVan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BenhNhanId = table.Column<int>(type: "INTEGER", nullable: false),
                    BacSiId = table.Column<int>(type: "INTEGER", nullable: false),
                    TrangThai = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ThoiGianChoTraLoiTu = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ThoiGianTinNhanCuoi = table.Column<DateTime>(type: "TEXT", nullable: true),
                    GhiChuKetLuan = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    NgayDong = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DaGuiAutoReplyNgoaiGio = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoiThoaiTuVan", x => x.Id);
                    table.CheckConstraint("CK_HoiThoaiTuVan_TrangThai", "TrangThai IN ('Moi','DangXuLy','DaTraLoi','DaDong')");
                    table.ForeignKey(
                        name: "FK_HoiThoaiTuVan_BacSi_BacSiId",
                        column: x => x.BacSiId,
                        principalTable: "BacSi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HoiThoaiTuVan_BenhNhan_BenhNhanId",
                        column: x => x.BenhNhanId,
                        principalTable: "BenhNhan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TinNhanTuVan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HoiThoaiId = table.Column<int>(type: "INTEGER", nullable: false),
                    NguoiGuiId = table.Column<int>(type: "INTEGER", nullable: true),
                    VaiTroNguoiGui = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Loai = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    NoiDung = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ThoiGianGui = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DaXemBoiNguoiNhan = table.Column<bool>(type: "INTEGER", nullable: false),
                    NgayXem = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TinNhanTuVan", x => x.Id);
                    table.CheckConstraint("CK_TinNhanTuVan_Loai", "Loai IN ('Text','MoiDatLich','TuDongPhanHoi')");
                    table.CheckConstraint("CK_TinNhanTuVan_VaiTroNguoiGui", "VaiTroNguoiGui IN ('Doctor','Patient','HeThong')");
                    table.ForeignKey(
                        name: "FK_TinNhanTuVan_HoiThoaiTuVan_HoiThoaiId",
                        column: x => x.HoiThoaiId,
                        principalTable: "HoiThoaiTuVan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TinNhanTuVan_NguoiDung_NguoiGuiId",
                        column: x => x.NguoiGuiId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TepDinhKemTinNhan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TinNhanId = table.Column<int>(type: "INTEGER", nullable: false),
                    TenGoc = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    TenLuuTru = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    KichThuoc = table.Column<long>(type: "INTEGER", nullable: false),
                    ThuTu = table.Column<int>(type: "INTEGER", nullable: false),
                    NgayTaiLen = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TepDinhKemTinNhan", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TepDinhKemTinNhan_TinNhanTuVan_TinNhanId",
                        column: x => x.TinNhanId,
                        principalTable: "TinNhanTuVan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HoiThoaiTuVan_BacSiId",
                table: "HoiThoaiTuVan",
                column: "BacSiId");

            migrationBuilder.CreateIndex(
                name: "IX_HoiThoaiTuVan_BenhNhanId_BacSiId",
                table: "HoiThoaiTuVan",
                columns: new[] { "BenhNhanId", "BacSiId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HoiThoaiTuVan_TrangThai",
                table: "HoiThoaiTuVan",
                column: "TrangThai");

            migrationBuilder.CreateIndex(
                name: "IX_TepDinhKemTinNhan_TinNhanId",
                table: "TepDinhKemTinNhan",
                column: "TinNhanId");

            migrationBuilder.CreateIndex(
                name: "IX_TinNhanTuVan_HoiThoaiId_ThoiGianGui",
                table: "TinNhanTuVan",
                columns: new[] { "HoiThoaiId", "ThoiGianGui" });

            migrationBuilder.CreateIndex(
                name: "IX_TinNhanTuVan_HoiThoaiId_VaiTroNguoiGui_DaXemBoiNguoiNhan",
                table: "TinNhanTuVan",
                columns: new[] { "HoiThoaiId", "VaiTroNguoiGui", "DaXemBoiNguoiNhan" });

            migrationBuilder.CreateIndex(
                name: "IX_TinNhanTuVan_NguoiGuiId",
                table: "TinNhanTuVan",
                column: "NguoiGuiId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TepDinhKemTinNhan");

            migrationBuilder.DropTable(
                name: "TinNhanTuVan");

            migrationBuilder.DropTable(
                name: "HoiThoaiTuVan");
        }
    }
}
