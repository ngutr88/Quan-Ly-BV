using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyBenhVien.Migrations
{
    /// <inheritdoc />
    public partial class PrescriptionSafetyCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CoBaoHiemYTe",
                table: "Thuoc",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LieuToiDaMoiNgay",
                table: "Thuoc",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LieuToiDaMoiNgayTheoKg",
                table: "Thuoc",
                type: "decimal(10, 4)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuyCachSoLuong",
                table: "Thuoc",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayHenTaiKham",
                table: "PhieuKham",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BacSiKeId",
                table: "DonThuoc",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Backfill dòng DonThuoc đã tồn tại (BacSiKeId là cột mới, NOT NULL)
            // bằng đúng bác sĩ của lượt khám gắn với đơn đó, thay vì để nguyên
            // giá trị mặc định 0 (không trỏ tới bác sĩ thật nào).
            migrationBuilder.Sql(@"
                UPDATE DonThuoc
                SET BacSiKeId = (
                    SELECT lk.BacSiId
                    FROM PhieuKham pk
                    INNER JOIN LichKham lk ON lk.Id = pk.LichKhamId
                    WHERE pk.Id = DonThuoc.PhieuKhamId
                )
                WHERE BacSiKeId = 0;

                UPDATE DonThuoc SET BacSiKeId = (SELECT Id FROM BacSi ORDER BY Id LIMIT 1) WHERE BacSiKeId = 0;
            ");

            migrationBuilder.AddColumn<string>(
                name: "LyDoHuy",
                table: "DonThuoc",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrangThai",
                table: "DonThuoc",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "ChoCapPhat");

            migrationBuilder.AddColumn<string>(
                name: "DuongDung",
                table: "ChiTietDonThuoc",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HuongDanSuDung",
                table: "ChiTietDonThuoc",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LieuMoiLan",
                table: "ChiTietDonThuoc",
                type: "decimal(10, 2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SoLanMoiNgay",
                table: "ChiTietDonThuoc",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SoNgayDung",
                table: "ChiTietDonThuoc",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThoiDiemDung",
                table: "ChiTietDonThuoc",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MucDoDiUng",
                table: "BenhNhan",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NhomHoatChatCheoPhanUng",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenNhom = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    MoTa = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    LaDuLieuMinhHoa = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhomHoatChatCheoPhanUng", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhanBoLoThuocDonThuoc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChiTietDonThuocId = table.Column<int>(type: "INTEGER", nullable: false),
                    LoThuocId = table.Column<int>(type: "INTEGER", nullable: false),
                    SoLuongLay = table.Column<int>(type: "INTEGER", nullable: false),
                    DaHoanTra = table.Column<bool>(type: "INTEGER", nullable: false),
                    NgayPhanBo = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhanBoLoThuocDonThuoc", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhanBoLoThuocDonThuoc_ChiTietDonThuoc_ChiTietDonThuocId",
                        column: x => x.ChiTietDonThuocId,
                        principalTable: "ChiTietDonThuoc",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhanBoLoThuocDonThuoc_LoThuoc_LoThuocId",
                        column: x => x.LoThuocId,
                        principalTable: "LoThuoc",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TuongTacThuoc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HoatChatA = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    HoatChatB = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    MucDoTuongTac = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    MoTa = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    NguonNhap = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    LaDuLieuMinhHoa = table.Column<bool>(type: "INTEGER", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TuongTacThuoc", x => x.Id);
                    table.CheckConstraint("CK_TuongTacThuoc_MucDo", "MucDoTuongTac IN ('ChongChiDinh','NghiemTrong','TrungBinh','Nhe')");
                });

            migrationBuilder.CreateTable(
                name: "ThanhVienNhomHoatChat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NhomId = table.Column<int>(type: "INTEGER", nullable: false),
                    HoatChat = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThanhVienNhomHoatChat", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThanhVienNhomHoatChat_NhomHoatChatCheoPhanUng_NhomId",
                        column: x => x.NhomId,
                        principalTable: "NhomHoatChatCheoPhanUng",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DonThuoc_BacSiKeId",
                table: "DonThuoc",
                column: "BacSiKeId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DonThuoc_TrangThai",
                table: "DonThuoc",
                sql: "TrangThai IN ('Nhap','ChoCapPhat','DaCapPhat','DuocPhanHoi','DaHuy')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BenhNhan_MucDoDiUng",
                table: "BenhNhan",
                sql: "MucDoDiUng IS NULL OR MucDoDiUng IN ('Nhe','TrungBinh','NghiemTrong')");

            migrationBuilder.CreateIndex(
                name: "IX_PhanBoLoThuocDonThuoc_ChiTietDonThuocId",
                table: "PhanBoLoThuocDonThuoc",
                column: "ChiTietDonThuocId");

            migrationBuilder.CreateIndex(
                name: "IX_PhanBoLoThuocDonThuoc_LoThuocId",
                table: "PhanBoLoThuocDonThuoc",
                column: "LoThuocId");

            migrationBuilder.CreateIndex(
                name: "IX_ThanhVienNhomHoatChat_HoatChat",
                table: "ThanhVienNhomHoatChat",
                column: "HoatChat");

            migrationBuilder.CreateIndex(
                name: "IX_ThanhVienNhomHoatChat_NhomId",
                table: "ThanhVienNhomHoatChat",
                column: "NhomId");

            migrationBuilder.CreateIndex(
                name: "IX_TuongTacThuoc_HoatChatA_HoatChatB",
                table: "TuongTacThuoc",
                columns: new[] { "HoatChatA", "HoatChatB" });

            migrationBuilder.AddForeignKey(
                name: "FK_DonThuoc_BacSi_BacSiKeId",
                table: "DonThuoc",
                column: "BacSiKeId",
                principalTable: "BacSi",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DonThuoc_BacSi_BacSiKeId",
                table: "DonThuoc");

            migrationBuilder.DropTable(
                name: "PhanBoLoThuocDonThuoc");

            migrationBuilder.DropTable(
                name: "ThanhVienNhomHoatChat");

            migrationBuilder.DropTable(
                name: "TuongTacThuoc");

            migrationBuilder.DropTable(
                name: "NhomHoatChatCheoPhanUng");

            migrationBuilder.DropIndex(
                name: "IX_DonThuoc_BacSiKeId",
                table: "DonThuoc");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DonThuoc_TrangThai",
                table: "DonThuoc");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BenhNhan_MucDoDiUng",
                table: "BenhNhan");

            migrationBuilder.DropColumn(
                name: "CoBaoHiemYTe",
                table: "Thuoc");

            migrationBuilder.DropColumn(
                name: "LieuToiDaMoiNgay",
                table: "Thuoc");

            migrationBuilder.DropColumn(
                name: "LieuToiDaMoiNgayTheoKg",
                table: "Thuoc");

            migrationBuilder.DropColumn(
                name: "QuyCachSoLuong",
                table: "Thuoc");

            migrationBuilder.DropColumn(
                name: "NgayHenTaiKham",
                table: "PhieuKham");

            migrationBuilder.DropColumn(
                name: "BacSiKeId",
                table: "DonThuoc");

            migrationBuilder.DropColumn(
                name: "LyDoHuy",
                table: "DonThuoc");

            migrationBuilder.DropColumn(
                name: "TrangThai",
                table: "DonThuoc");

            migrationBuilder.DropColumn(
                name: "DuongDung",
                table: "ChiTietDonThuoc");

            migrationBuilder.DropColumn(
                name: "HuongDanSuDung",
                table: "ChiTietDonThuoc");

            migrationBuilder.DropColumn(
                name: "LieuMoiLan",
                table: "ChiTietDonThuoc");

            migrationBuilder.DropColumn(
                name: "SoLanMoiNgay",
                table: "ChiTietDonThuoc");

            migrationBuilder.DropColumn(
                name: "SoNgayDung",
                table: "ChiTietDonThuoc");

            migrationBuilder.DropColumn(
                name: "ThoiDiemDung",
                table: "ChiTietDonThuoc");

            migrationBuilder.DropColumn(
                name: "MucDoDiUng",
                table: "BenhNhan");
        }
    }
}
