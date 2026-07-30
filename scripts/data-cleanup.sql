-- ============================================================================
-- Script lam sach du lieu TRUOC KHI ap dung migration DataIntegrityConstraints.
-- ============================================================================
-- Muc dich: cho phep kiem tra/chay THU cac buoc lam sach mot cach doc lap,
-- TRUOC khi chay "dotnet ef database update" that su. Migration
-- (Migrations/20260730044849_DataIntegrityConstraints.cs) da nhung san CHINH
-- XAC cac cau lenh nay o dau ham Up(), nen ban KHONG BAT BUOC phai chay file
-- nay thu cong - no chi giup review/doi chieu truoc khi migrate that.
--
-- Cach dung (khuyen nghi: chay tren MOT BAN SAO cua hms.db, khong chay truc
-- tiep len CSDL dang phuc vu ung dung):
--   1) Sao luu:      .\scripts\backup-sqlite.ps1
--   2) Copy thu:     Copy-Item hms.db hms_test.db
--   3) Kiem tra:     chay cac cau SELECT "truoc khi sua" ben duoi de xem du
--                     lieu nao se bi anh huong
--   4) Lam sach:      chay cac cau UPDATE ben duoi tren hms_test.db
--   5) Doi chieu:     chay lai cac cau SELECT de xac nhan da sach
--   6) Chi khi hai long, moi chay that: dotnet ef database update
--
-- Cu phap SQLite ben duoi (trim(), ||). Neu chuyen sang SQL Server, thay:
--   trim(x)   -> LTRIM(RTRIM(x))
--   a || b    -> a + b
-- (migration .cs da tu dong chon dung cu phap theo provider dang chay).
-- ============================================================================


-- ----------------------------------------------------------------------------
-- 1) NguoiDung.Email rong ("" khong phai NULL nen van dung UNIQUE)
-- ----------------------------------------------------------------------------
-- Truoc khi sua - xem cac tai khoan bi anh huong:
SELECT Id, HoTen, Email, Sdt, VaiTro FROM NguoiDung WHERE Email IS NULL OR trim(Email) = '';

-- Lam sach: gan placeholder duy nhat theo Id (KHONG xoa tai khoan).
UPDATE NguoiDung
SET Email = 'no-email-user-' || Id || '@placeholder.invalid'
WHERE Email IS NULL OR trim(Email) = '';

-- Sau khi sua - phai tra ve 0 dong:
SELECT count(*) AS con_email_rong FROM NguoiDung WHERE Email IS NULL OR trim(Email) = '';


-- ----------------------------------------------------------------------------
-- 2) BenhNhan.SoCCCD / SoBHYT rong ("" -> NULL, vi NULL moi dung nghia "chua co")
-- ----------------------------------------------------------------------------
SELECT Id, NguoiDungId, SoCCCD, SoBHYT FROM BenhNhan
WHERE (SoCCCD IS NOT NULL AND trim(SoCCCD) = '') OR (SoBHYT IS NOT NULL AND trim(SoBHYT) = '');

UPDATE BenhNhan SET SoCCCD = NULL WHERE SoCCCD IS NOT NULL AND trim(SoCCCD) = '';
UPDATE BenhNhan SET SoBHYT = NULL WHERE SoBHYT IS NOT NULL AND trim(SoBHYT) = '';


-- ----------------------------------------------------------------------------
-- 3) BenhNhan.SoCCCD / SoBHYT trung gia tri that (vd. du lieu kiem thu dang ky
--    trung so BHYT). Giu ban ghi co Id nho nhat (tao truoc), cac ban ghi trung
--    sau do chuyen ve NULL thay vi xoa ho so benh nhan.
-- ----------------------------------------------------------------------------
SELECT SoBHYT, count(*) AS so_lan_trung FROM BenhNhan WHERE SoBHYT IS NOT NULL GROUP BY SoBHYT HAVING count(*) > 1;
SELECT SoCCCD, count(*) AS so_lan_trung FROM BenhNhan WHERE SoCCCD IS NOT NULL GROUP BY SoCCCD HAVING count(*) > 1;

UPDATE BenhNhan SET SoBHYT = NULL
WHERE SoBHYT IS NOT NULL AND Id NOT IN (
    SELECT MIN(Id) FROM BenhNhan WHERE SoBHYT IS NOT NULL GROUP BY SoBHYT
);
UPDATE BenhNhan SET SoCCCD = NULL
WHERE SoCCCD IS NOT NULL AND Id NOT IN (
    SELECT MIN(Id) FROM BenhNhan WHERE SoCCCD IS NOT NULL GROUP BY SoCCCD
);


-- ----------------------------------------------------------------------------
-- 4) BacSi.ChucVu bi go sai/thieu dau ("Bac si", "Pho truong khoa") hoac them
--    mo ta khong can thiet ("Bac si dieu tri") lam trung nghia voi "Bac si".
-- ----------------------------------------------------------------------------
SELECT ChucVu, count(*) FROM BacSi GROUP BY ChucVu;

UPDATE BacSi SET ChucVu = 'Bác sĩ' WHERE ChucVu IN ('Bac si', 'Bác sĩ điều trị');
UPDATE BacSi SET ChucVu = 'Phó trưởng khoa' WHERE ChucVu = 'Pho truong khoa';

-- Sau khi sua - chi con dung 3 gia tri:
SELECT ChucVu, count(*) FROM BacSi GROUP BY ChucVu;


-- ----------------------------------------------------------------------------
-- 5) HoaDon.PhuongThuc bi gan nham gia tri "ChuaThanhToan" (do la
--    TrangThaiThanhToan, khong phai phuong thuc thanh toan). Chuan hoa ve
--    'TienMat' - cung gia tri mac dinh ma ExamController.CompleteSession dung
--    cho MOI hoa don moi tao truoc khi thanh toan that su dien ra.
-- ----------------------------------------------------------------------------
SELECT Id, TrangThaiThanhToan, PhuongThuc FROM HoaDon WHERE PhuongThuc = 'ChuaThanhToan';

UPDATE HoaDon SET PhuongThuc = 'TienMat' WHERE PhuongThuc = 'ChuaThanhToan';


-- ----------------------------------------------------------------------------
-- Kiem tra tong quat con lai truoc khi ap CHECK constraint (tat ca phai tra ve 0):
-- ----------------------------------------------------------------------------
SELECT count(*) AS vi_pham_danhgia_sosao FROM DanhGia WHERE SoSao < 1 OR SoSao > 5;
SELECT count(*) AS vi_pham_thuoc_tonkho FROM Thuoc WHERE TonKho < 0;
SELECT count(*) AS vi_pham_thuoc_nguong FROM Thuoc WHERE NguongToiThieu < 0;
SELECT count(*) AS vi_pham_thuoc_gia FROM Thuoc WHERE Gia < 0;
SELECT count(*) AS vi_pham_lothuoc_nhap FROM LoThuoc WHERE SoLuongNhap <= 0;
SELECT count(*) AS vi_pham_lothuoc_ton FROM LoThuoc WHERE SoLuongTon < 0 OR SoLuongTon > SoLuongNhap;
SELECT count(*) AS vi_pham_ctdt_soluong FROM ChiTietDonThuoc WHERE SoLuong <= 0;
SELECT count(*) AS vi_pham_dichvu_gia FROM DichVu WHERE Gia < 0;
SELECT count(*) AS vi_pham_hoadon_tongtien FROM HoaDon WHERE TongTien < 0;
SELECT count(*) AS vi_pham_cthd_sotien FROM ChiTietHoaDon WHERE SoTien < 0;
SELECT count(*) AS vi_pham_llv_sobn FROM LichLamViecBacSi WHERE SoBenhNhanToiDa <= 0;
SELECT count(*) AS vi_pham_llv_thoiluong FROM LichLamViecBacSi WHERE ThoiLuongKhamPhut < 5 OR ThoiLuongKhamPhut > 240;
SELECT count(*) AS vi_pham_llv_gio FROM LichLamViecBacSi WHERE GioKetThuc <= GioBatDau;
SELECT count(*) AS vi_pham_ngaysinh_tuonglai FROM BenhNhan WHERE date(NgaySinh) > date('now');
SELECT count(*) AS vi_pham_vaitro FROM NguoiDung WHERE VaiTro NOT IN ('Admin','Doctor','Patient');
SELECT count(*) AS vi_pham_trangthai_nd FROM NguoiDung WHERE TrangThai NOT IN ('Active','Blocked');
SELECT count(*) AS vi_pham_trangthai_lk FROM LichKham WHERE TrangThai NOT IN ('ChoXacNhan','DaXacNhan','DangKham','HoanThanh','DaHuy','VangMat');
SELECT count(*) AS vi_pham_trangthai_hd FROM HoaDon WHERE TrangThaiThanhToan NOT IN ('ChuaThanhToan','DaThanhToan','DangXuLy','QuaHan','ThanhToanThatBai','DaHuy');
SELECT count(*) AS vi_pham_phuongthuc FROM HoaDon WHERE PhuongThuc IS NOT NULL AND PhuongThuc NOT IN ('TienMat','ChuyenKhoan','Online (MoMo)','Online (VNPay)','Online (ZaloPay)','Online');
SELECT count(*) AS vi_pham_chucvu FROM BacSi WHERE ChucVu NOT IN ('Bác sĩ','Phó trưởng khoa','Trưởng khoa');
SELECT Email, count(*) AS c FROM NguoiDung WHERE Email IS NOT NULL AND Email <> '' GROUP BY Email HAVING c > 1;
SELECT Sdt, count(*) AS c FROM NguoiDung WHERE Sdt IS NOT NULL AND Sdt <> '' GROUP BY Sdt HAVING c > 1;
SELECT SoCCCD, count(*) AS c FROM BenhNhan WHERE SoCCCD IS NOT NULL GROUP BY SoCCCD HAVING c > 1;
SELECT SoBHYT, count(*) AS c FROM BenhNhan WHERE SoBHYT IS NOT NULL GROUP BY SoBHYT HAVING c > 1;
