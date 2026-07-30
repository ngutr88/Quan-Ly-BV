# Rà soát & chuẩn hóa cơ sở dữ liệu HMS (QuanLyBenhVien)

Tài liệu này tổng hợp toàn bộ thay đổi cơ sở dữ liệu thực hiện trong migration
`Migrations/20260730044849_DataIntegrityConstraints.cs`, dựa trên khảo sát trực
tiếp schema và dữ liệu thật trong `hms.db` (312 tài khoản, 279 bác sĩ, 28 bệnh
nhân, 29 lịch khám, 16 hóa đơn, 3343 ca làm việc...). Không có bảng/cột nào bị
xóa hoặc đổi tên; mọi thay đổi đều là bổ sung (thêm cột, thêm ràng buộc, sửa
hành vi xóa) hoặc chuẩn hóa dữ liệu bẩn đã phát hiện.

## 1. Lỗi / điểm chưa hợp lý phát hiện trong CSDL hiện tại

| # | Vấn đề | Bằng chứng thực tế | Mức độ |
|---|---|---|---|
| 1 | Xóa `NguoiDung` (User) sẽ **CASCADE** xóa `BacSi`/`BenhNhan` và toàn bộ dữ liệu y tế phụ thuộc (đánh giá, người thân, tài liệu, tiêm chủng, tiền sử gia đình, chỉ số sức khỏe) | Cấu hình `OnDelete(DeleteBehavior.Cascade)` trên cả 2 quan hệ 1-1 User↔Doctor, User↔Patient | **Cao** - đúng rủi ro nêu trong yêu cầu |
| 2 | `StaffController.Edit` xóa cứng `BacSi` khi đổi vai trò Doctor→Admin (`_context.Doctors.Remove(...)`) | `Areas/Admin/Controllers/StaffController.cs` (trước sửa) | **Cao** - mất `DanhGia` (Cascade), có thể vướng `LichKham`/`PhieuKham` |
| 3 | `PhieuKham.LichKhamId`, `DonThuoc.PhieuKhamId`, `HoaDon.PhieuKhamId` không có ràng buộc UNIQUE dù nghiệp vụ (đã xác nhận trong `ExamController.CompleteSession`) chỉ tạo tối đa 1 bản ghi mỗi loại cho mỗi lịch khám/phiếu khám | Quan hệ khai báo `WithMany()` thay vì `WithOne()` trong `ApplicationDbContext` | Trung bình |
| 4 | `BenhNhan.SoCCCD = '079000000017'`... không trùng, nhưng `SoBHYT = 'DN4123456789012'` bị trùng ở 2 bệnh nhân (Id 27, 28 - dữ liệu kiểm thử đăng ký) | Khảo sát trực tiếp `hms.db` | Trung bình |
| 5 | `NguoiDung.Email` rỗng (`''`, không phải NULL) ở 2 tài khoản test (Id 309, 310) | Khảo sát trực tiếp | Trung bình |
| 6 | `BacSi.ChucVu` có giá trị gõ sai chính tả/thiếu dấu tạo ra 5 nhóm lẽ ra chỉ nên có 3: `'Bac si'` (5), `'Bác sĩ'` (227), `'Bác sĩ điều trị'` (1), `'Pho truong khoa'` (1), `'Phó trưởng khoa'` (30), `'Trưởng khoa'` (15) | Khảo sát trực tiếp | Trung bình - lỗi kinh điển "chuỗi tự do dễ sai chính tả" |
| 7 | `HoaDon.PhuongThuc` bị gán giá trị `'ChuaThanhToan'` ở 2 hóa đơn - đó là một `TrangThaiThanhToan`, không phải phương thức thanh toán | Khảo sát trực tiếp + `DbSeeder.cs` (đã sửa) | Thấp/Trung bình - lẫn lộn 2 khái niệm |
| 8 | `LichLamViecBacSi` không kiểm tra chồng ca (2 ca khác giờ nhưng đè lên nhau vẫn được lưu) | `Helpers/DoctorScheduleHelper.cs` chỉ lọc `GioBatDau < GioKetThuc`, không so 2 ca với nhau | Trung bình |
| 9 | Đặt lịch khám: "đầy slot" được tính theo kiểu có/không (1 lịch = hết chỗ), bỏ qua `SoBenhNhanToiDa` - cột này **chưa từng được đọc** ở bất kỳ đâu trong code | `BookController.GetAvailableSlotsAsync` (trước sửa); `SoBenhNhanToiDa` luôn = 1 trong dữ liệu hiện có nên bug chưa lộ | Trung bình |
| 10 | `DanhGia` không có liên kết tới lần khám cụ thể - không thể ràng buộc "không đánh giá 2 lần cho cùng 1 lần khám"; đồng thời không có kiểm tra "đã hoàn thành khám mới được đánh giá" | `RecordController.SubmitReview` (trước sửa) chỉ kiểm tra bác sĩ tồn tại | Trung bình |
| 11 | `LoThuoc` không có cột ngày nhập → không thể kiểm tra "hạn dùng ≥ ngày nhập" | Schema `LoThuoc` (trước sửa) | Thấp - thiếu cột cần cho ràng buộc yêu cầu |
| 12 | Không có CHECK nào cho các cột số lượng/tiền/ngày tháng - toàn bộ ràng buộc nghiệp vụ (tồn kho ≥ 0, ngày hết hạn ≥ ngày nhập...) chỉ tồn tại dưới dạng `[Range]`/`[Required]` phía C#, không có ở tầng CSDL | Toàn bộ entity trước sửa | Cao - dữ liệu ghi trực tiếp bằng SQL hoặc từ nguồn khác sẽ không được bảo vệ |
| 13 | Các cột trạng thái (`TrangThai`, `VaiTro`, `TrangThaiThanhToan`, `PhuongThuc`) là `string` tự do, không có CHECK/enum ràng buộc tại CSDL | Toàn bộ entity trước sửa | Trung bình |
| 14 | `NgayTao`/`NgayGui`/`NgayKham`... không có `DEFAULT` ở tầng CSDL, chỉ dựa vào giá trị C# `DateTime.Now` gán trước khi `SaveChanges` | Toàn bộ entity trước sửa | Thấp - chỉ là lưới an toàn, ứng dụng vẫn hoạt động đúng |

## 2. Danh sách ràng buộc đã thêm (đầy đủ, theo migration `DataIntegrityConstraints`)

### 2.1. UNIQUE
- `NguoiDung.Email` - UNIQUE (đã làm sạch 2 email rỗng trước khi thêm).
- `NguoiDung.Sdt` - UNIQUE (dữ liệu vốn đã sạch).
- `BenhNhan.SoCCCD` - UNIQUE, cho phép nhiều `NULL` (filtered index trên SQL Server).
- `BenhNhan.SoBHYT` - UNIQUE, cho phép nhiều `NULL` (đã dồn giá trị trùng `'DN4123456789012'` về `NULL` cho bản ghi tạo sau).
- `BacSi.NguoiDungId`, `BenhNhan.NguoiDungId` - **đã tự động UNIQUE từ trước** nhờ quan hệ 1-1 với `NguoiDung` (không cần thêm).
- `PhieuKham.LichKhamId`, `DonThuoc.PhieuKhamId`, `HoaDon.PhieuKhamId` - UNIQUE, chính thức hóa bằng cách đổi quan hệ từ `WithMany()` sang `WithOne()` (1-1 thật sự, khớp với hành vi `ExamController.CompleteSession`).
- `DanhGia.LichKhamId` (cột mới, nullable) - UNIQUE cho phép nhiều `NULL`, đảm bảo tối đa 1 đánh giá cho mỗi lần khám cụ thể khi cột này được điền.

### 2.2. CHECK
| Bảng | Ràng buộc |
|---|---|
| `DanhGia` | `SoSao BETWEEN 1 AND 5` |
| `Thuoc` | `TonKho >= 0`, `NguongToiThieu >= 0`, `Gia >= 0` |
| `LoThuoc` | `SoLuongNhap > 0`, `SoLuongTon >= 0`, `SoLuongTon <= SoLuongNhap`, `HanSuDung >= NgayNhap` |
| `ChiTietDonThuoc` | `SoLuong > 0` |
| `DichVu` | `Gia >= 0` |
| `HoaDon` | `TongTien >= 0`, `TrangThaiThanhToan IN (...)`, `PhuongThuc IS NULL OR PhuongThuc IN (...)` |
| `ChiTietHoaDon` | `SoTien >= 0` |
| `LichLamViecBacSi` | `SoBenhNhanToiDa > 0`, `ThoiLuongKhamPhut BETWEEN 5 AND 240`, `GioKetThuc > GioBatDau` |
| `BenhNhan` | `NgaySinh <= (thời điểm ghi nhận)` |
| `NguoiDung` | `VaiTro IN ('Admin','Doctor','Patient')`, `TrangThai IN ('Active','Blocked')` |
| `LichKham` | `TrangThai IN ('ChoXacNhan','DaXacNhan','DangKham','HoanThanh','DaHuy','VangMat')` |
| `BacSi` | `ChucVu IN ('Bác sĩ','Phó trưởng khoa','Trưởng khoa')` (đã làm sạch lỗi chính tả trước khi thêm) |

**Không thêm** CHECK cho `BacSi.HocVi`: đây là học vị/chức danh y khoa ghép
(`ThS.BS`, `TS.BS`, `PGS.TS.BS`...), một tập giá trị mở rộng hợp lệ trong thực
tế, không phải lỗi chính tả - ép vào danh sách cố định sẽ chặn các tổ hợp học
vị hợp lệ trong tương lai (`GS.TS.BS`...). Bình luận code cũ (`// BS, ThS, TS,
PGS, GS`) đã lỗi thời so với dữ liệu thật, không phản ánh đúng lỗi cần sửa.

**Không thêm** cột "trạng thái phiếu khám" hay "trạng thái thuốc" mới:
- Trạng thái phiếu khám vốn được suy ra từ `LichKham.TrangThai` (không có cột
  riêng nào từng tồn tại) - thêm cột trùng lặp sẽ tạo nguy cơ 2 nguồn sự thật
  lệch nhau.
- Trạng thái thuốc (còn hàng/sắp hết/hết hàng) nên tiếp tục được **tính** từ
  `TonKho` so với `NguongToiThieu` tại tầng hiển thị, không lưu thành cột -
  tránh lỗi kinh điển "cột trạng thái phái sinh bị lệch với dữ liệu gốc".
- "Trạng thái ca làm việc" đã là kiểu `bool DangHoatDong` (không phải chuỗi tự
  do) từ trước, đã đạt yêu cầu chuẩn hóa.

### 2.3. Khóa ngoại & hành vi xóa (đầy đủ các thay đổi)
| Quan hệ | Trước | Sau | Lý do |
|---|---|---|---|
| `BacSi.NguoiDungId → NguoiDung` | Cascade | **Restrict** | Không cho xóa cứng tài khoản còn hồ sơ bác sĩ |
| `BenhNhan.NguoiDungId → NguoiDung` | Cascade | **Restrict** | Không cho xóa cứng tài khoản còn hồ sơ bệnh nhân |
| `DanhGia.BacSiId → BacSi` | Cascade | **Restrict** | Giữ lịch sử đánh giá khi bác sĩ ngừng hoạt động |
| `PhieuKham.LichKhamId → LichKham` | Cascade | **Restrict** | Không mất phiếu khám nếu lịch khám bị xóa |
| `DonThuoc.PhieuKhamId → PhieuKham` | Cascade | **Restrict** | Không mất đơn thuốc nếu phiếu khám bị xóa |
| `HoaDon.PhieuKhamId → PhieuKham` | Cascade | **Restrict** | Không mất hóa đơn nếu phiếu khám bị xóa |
| `NguoiThan/TaiLieuBenhNhan/TienSuGiaDinh/TiemChung/ChiSoSucKhoeTuDo.BenhNhanId → BenhNhan` | Cascade | **Restrict** | Đây là "dữ liệu y tế" theo đúng nghĩa yêu cầu mục 5, không phải bảng chi tiết của một giao dịch đơn lẻ |
| `BacSi.KhoaId → Khoa` | Restrict | Restrict (giữ nguyên) | Không cho xóa khoa còn bác sĩ - đã đúng từ trước |
| `LichKham.BacSiId → BacSi` | SetNull | SetNull (giữ nguyên) | Giữ lịch khám khi bác sĩ ngừng hoạt động, theo đúng yêu cầu |
| `LichKham.BenhNhanId → BenhNhan` | Restrict | Restrict (giữ nguyên) | Đã đúng từ trước |
| `ChiTietDonThuoc.DonThuocId → DonThuoc` | Cascade | Cascade (giữ nguyên) | Bảng chi tiết phụ thuộc hoàn toàn - được phép theo mục 5 |
| `ChiTietDonThuoc.ThuocId → Thuoc` | Restrict | Restrict (giữ nguyên) | Giữ lịch sử đơn thuốc dù thuốc gốc có bị xóa |
| `ChiTietHoaDon.HoaDonId → HoaDon` | Cascade | Cascade (giữ nguyên) | Bảng chi tiết phụ thuộc hoàn toàn - được phép theo mục 5 |
| `LoThuoc.ThuocId → Thuoc` | Cascade | Cascade (giữ nguyên) | Lô thuốc phụ thuộc hoàn toàn vào thuốc gốc - nghiệp vụ cho phép |
| `DanhGia.LichKhamId → LichKham` (mới) | - | **SetNull** | Hủy liên kết không được xóa mất đánh giá đã có |
| `NguoiDung/BacSi/BenhNhan.XoaBoiId → NguoiDung` (mới) | - | Restrict / NoAction | Tự tham chiếu (self-reference) ai đã xóa mềm bản ghi này |

### 2.4. Index bổ sung (mục 8)
Phần lớn cột trong yêu cầu **đã tự động có index** nhờ quy ước EF Core (mọi
khóa ngoại được EF Core tự đánh index): `BacSi.KhoaId`, `LichKham.BenhNhanId`,
`LichKham.BacSiId`, `PhieuKham.LichKhamId`, `HoaDon.PhieuKhamId`,
`DonThuoc.PhieuKhamId`, `LoThuoc.ThuocId`, `ThongBao.NguoiDungId` - các UNIQUE
INDEX ở mục 2.1 cũng đồng thời phục vụ tra cứu cho `Email`, `Sdt`, `SoCCCD`,
`SoBHYT`. Chỉ 2 cột không phải khóa ngoại nên cần khai báo tường minh:
- `LichKham.ThoiGian` (ghi chú: yêu cầu ghi "`LichKham.NgayKham`" nhưng cột
  thực tế tên là `ThoiGian` - không đổi tên cột theo đúng nguyên tắc "không tự
  ý đổi tên", chỉ đánh index đúng cột chứa ngày giờ khám).
- `LichKham.TrangThai`.

### 2.5. Kiểu dữ liệu (mục 10)
Đã đạt yêu cầu từ trước, không cần đổi: tiền tệ dùng `decimal(18,2)`
(`Gia`, `TongTien`, `SoTien`...), số điện thoại/CCCD/BHYT dùng `string`,
boolean dùng `bool` (ánh xạ `INTEGER 0/1` trên SQLite, `bit` trên SQL Server).
Chỉ 2 điều chỉnh nhỏ: `BenhNhan.SoCCCD`/`SoBHYT` và `HoaDon.PhuongThuc`
chuyển từ "chuỗi rỗng mặc định" sang **nullable thật sự** (`NULL` = chưa có),
để ràng buộc UNIQUE/CHECK "duy nhất/hợp lệ nếu khác NULL" có ý nghĩa đúng.

### 2.6. Ngày giờ mặc định (mục 9)
Thêm `DEFAULT CURRENT_TIMESTAMP` (SQLite) / `GETDATE()` (SQL Server) /
`NOW()` (MySQL) cho các cột `NgayTao`, `NgayKham`, `NgayKe`, `NgayGui`,
`ThoiGian` (audit log), `NgayGhiNhan`, `NgayTaiLen`, `NgayDo`, `NgayDang`,
`CapNhatLuc`, `NgayNhap`. Đây thuần túy là **lưới an toàn** cho các câu INSERT
trực tiếp bằng SQL - ứng dụng vẫn luôn gán `DateTime.Now` tường minh ở tầng
C# như trước, không có gì thay đổi hành vi hiện tại.

`NgayCapNhat`: chỉ tồn tại sẵn ở `TinTuc` (Article) và đã được
`NewsController` tự cập nhật thủ công khi sửa bài - không cần cơ chế tự động
thêm. **Không** thêm cột `NgayCapNhat` mới cho các bảng khác (NguoiDung,
BacSi, LichKham, PhieuKham...): đây sẽ là 8-10 cột mới không gắn với lỗi cụ
thể nào đã phát hiện, vượt phạm vi "chỉ sửa những gì thực sự cần". Có thể bổ
sung sau nếu có nhu cầu theo dõi lịch sử sửa cho các bảng đó.

## 3. Xóa mềm (Soft delete) - mục 5

Thêm 3 cột `DaXoa (bool)`, `NgayXoa (datetime?)`, `XoaBoiId (int?, tự tham
chiếu NguoiDung)` cho đúng 3 bảng có nhu cầu "gỡ bỏ" thực sự trong ứng dụng
hôm nay: **`NguoiDung`, `BacSi`, `BenhNhan`**. Không thêm cho `PhieuKham`,
`DonThuoc`, `HoaDon` vì hiện **không có bất kỳ hành động xóa nào** cho 3 bảng
này trong toàn bộ code (đã rà soát Admin/Doctor/Patient controllers) - thêm
cột xóa mềm không gắn với luồng nghiệp vụ nào sẽ chỉ là cột chết. Ghi chú lại
ở đây để bổ sung tương tự (đúng 1 pattern) khi tính năng xóa cho các bảng đó
được xây dựng.

Thay đổi code đi kèm để cơ chế này thực sự có tác dụng:
- `StaffController.Edit` (đổi vai trò Doctor → Admin): trước đây
  `_context.Doctors.Remove(...)` xóa cứng hồ sơ bác sĩ (mất `DanhGia` do
  Cascade). Nay set `DaXoa=true, NgayXoa=now, XoaBoiId=<admin thực hiện>`.
  Khi đổi ngược lại Admin → Doctor, code tái sử dụng đúng hồ sơ `BacSi` cũ
  (đã có sẵn theo `NguoiDungId` là UNIQUE) và **kích hoạt lại**
  (`DaXoa=false`, xóa `NgayXoa`/`XoaBoiId`) thay vì tạo hồ sơ trùng.
- `BookController` (đặt lịch) và `HomeController` (trang công khai: bác sĩ
  nổi bật, đếm bác sĩ theo khoa, danh bạ bác sĩ, thống kê trang chủ): thêm
  điều kiện `!DaXoa` bên cạnh điều kiện `TrangThai == "Active"` sẵn có, để bác
  sĩ đã bị gỡ không xuất hiện trong các luồng đặt lịch/duyệt web công khai.
- **Cố ý không** dùng EF Core Global Query Filter (`HasQueryFilter`) cho
  `Doctor`/`Patient`: nếu áp dụng toàn cục, các trang **lịch sử** (chi tiết
  lịch khám cũ, đánh giá cũ) sẽ mất luôn tên bác sĩ đã bị xóa mềm khi
  `Include(a => a.Doctor)` - đi ngược đúng mục tiêu "giữ lịch sử". Vì vậy chỉ
  lọc `!DaXoa` tại đúng các điểm "chọn bác sĩ để đặt lịch mới" như liệt kê ở
  trên, còn điều hướng theo lịch sử (navigation) không bị lọc.

## 4. Kiểm tra nghiệp vụ (mục 7) - đối chiếu từng ý

| Yêu cầu | Trạng thái | Ghi chú |
|---|---|---|
| Không đặt trùng giờ cùng bác sĩ | ✅ Đã có sẵn (`BookController`, kiểm tra `ThoiGian` trùng) | Không đổi |
| Không cho 2 ca làm việc chồng giờ | ✅ **Mới thêm** `DoctorScheduleHelper.HasOverlap` + gọi ở `StaffController`/`DoctorsController` trước khi lưu | |
| Không vượt quá `SoBenhNhanToiDa` mỗi ca | ✅ **Mới thêm** - `BookController` trước đây bỏ qua cột này hoàn toàn (1 lịch = hết chỗ); nay đếm số lịch đã đặt theo từng khung giờ và so với `SoBenhNhanToiDa` | Sửa cả `GetAvailableSlotsAsync` (hiển thị slot) và `ConfirmBooking` (kiểm tra lại trong transaction) |
| Không kê thuốc vượt tồn kho | ✅ Đã có sẵn (`ExamController.CompleteSession`) | Không đổi |
| Không dùng thuốc hết hạn | ✅ Đã có sẵn (lọc `HanSuDung > DateTime.Today`) | Không đổi |
| Không thanh toán âm/vượt số tiền còn lại | ⚠️ Không áp dụng được ở thiết kế hiện tại | Luồng thanh toán hiện chỉ có 2 dạng: đánh dấu **toàn bộ** hóa đơn đã trả (`InvoicesController.PayCounter`) hoặc webhook trả về trạng thái thành công/thất bại (`PaymentController`) - **không có** ô nhập số tiền nào để có thể âm/vượt. Khi tính năng thanh toán từng phần được xây dựng, cần bổ sung kiểm tra `0 < SoTien <= (TongTien - đã trả)` tại đó. |
| Không đánh giá bác sĩ nhiều lần cho cùng 1 lần khám | ✅ **Mới thêm** cột `DanhGia.LichKhamId` (nullable) + UNIQUE INDEX filtered | Xem mục 5 bên dưới |
| Chỉ bệnh nhân đã hoàn thành khám mới được đánh giá | ✅ **Mới thêm** kiểm tra `LichKham.TrangThai == "HoanThanh"` trong `RecordController.SubmitReview` | |
| Không tạo phiếu khám nếu lịch khám đã hủy | ✅ Đã có sẵn (`ExamController.CompleteSession` chặn `DaHuy`/`VangMat`) | Không đổi |
| Không tạo đơn thuốc/hóa đơn khi chưa có phiếu khám hợp lệ | ✅ Đã đảm bảo qua khóa ngoại `NOT NULL` (`PhieuKhamId` bắt buộc) | Không đổi |

## 5. Vì sao `DanhGia` không dùng UNIQUE(BenhNhanId, BacSiId)

Dữ liệu thật cho thấy cặp (BenhNhanId=17, BacSiId=272) có **4 đánh giá** ở 4
thời điểm khác nhau (các lần khám khác nhau với cùng 1 bác sĩ theo thời gian)
- đây là dữ liệu hợp lệ, không phải trùng lặp lỗi. Đồng thời `RecordController`
hiện tại **upsert theo (BenhNhanId, BacSiId)** (sửa đè đánh giá cũ), tức mâu
thuẫn với đúng ngữ nghĩa "một lần khám một đánh giá" mà yêu cầu mô tả.

Giải pháp đã chọn: thêm cột `DanhGia.LichKhamId` (nullable, mới) tham chiếu
đúng lần khám cụ thể, cùng UNIQUE INDEX filtered (duy nhất khi khác NULL).
Điều này:
- Cho phép giữ nguyên 4 đánh giá lịch sử nói trên (không phải xóa/dồn dữ liệu).
- Đã sẵn sàng ở tầng CSDL cho khi tính năng "đánh giá gắn với đúng lần khám"
  được xây dựng đầy đủ ở tầng giao diện (hiện `SubmitReview` mới nhận
  `doctorId`, chưa nhận `lichKhamId` - đây là việc UI/controller cho tương
  lai, ngoài phạm vi thuần CSDL của yêu cầu này).
- Trong lúc chờ đó, chỉ bổ sung kiểm tra tối thiểu, an toàn: bệnh nhân phải có
  ít nhất 1 lần khám `HoanThanh` với bác sĩ đó mới được gửi đánh giá.

## 6. Sơ đồ quan hệ (ER)

```mermaid
erDiagram
    NguoiDung ||--o| BacSi : "1-1"
    NguoiDung ||--o| BenhNhan : "1-1"
    NguoiDung ||--o{ ThongBao : ""
    NguoiDung ||--o{ NhatKyHeThong : ""
    NguoiDung ||--o| NguoiDung : "XoaBoiId (tu tham chieu)"

    Khoa ||--o{ BacSi : "KhoaId (Restrict)"
    Khoa ||--o{ DichVu : "KhoaId (Cascade)"

    BacSi ||--o{ LichLamViecBacSi : "Cascade"
    BacSi ||--o{ LichKham : "SetNull"
    BacSi ||--o{ DanhGia : "Restrict"

    BenhNhan ||--o{ LichKham : "Restrict"
    BenhNhan ||--o{ DanhGia : "Restrict"
    BenhNhan ||--o{ NguoiThan : "Restrict"
    BenhNhan ||--o{ TaiLieuBenhNhan : "Restrict"
    BenhNhan ||--o{ TienSuGiaDinh : "Restrict"
    BenhNhan ||--o{ TiemChung : "Restrict"
    BenhNhan ||--o{ ChiSoSucKhoeTuDo : "Restrict"

    LichKham ||--o| PhieuKham : "1-1, Restrict"
    LichKham ||--o{ DanhGia : "SetNull (LichKhamId, moi)"

    PhieuKham ||--o| DonThuoc : "1-1, Restrict"
    PhieuKham ||--o| HoaDon : "1-1, Restrict"

    DonThuoc ||--o{ ChiTietDonThuoc : "Cascade"
    Thuoc ||--o{ ChiTietDonThuoc : "Restrict"
    Thuoc ||--o{ LoThuoc : "Cascade"

    HoaDon ||--o{ ChiTietHoaDon : "Cascade"

    NguoiDung {
        int Id PK
        string Email UK
        string Sdt UK
        string VaiTro "CHECK"
        string TrangThai "CHECK"
        bool DaXoa "moi"
        datetime NgayXoa "moi"
        int XoaBoiId FK "moi"
    }
    BacSi {
        int Id PK
        int NguoiDungId FK_UK
        int KhoaId FK
        string ChucVu "CHECK"
        bool DaXoa "moi"
    }
    BenhNhan {
        int Id PK
        int NguoiDungId FK_UK
        string SoCCCD UK "nullable"
        string SoBHYT UK "nullable"
        date NgaySinh "CHECK"
        bool DaXoa "moi"
    }
    LichKham {
        int Id PK
        int BenhNhanId FK
        int BacSiId FK "nullable"
        datetime ThoiGian "index"
        string TrangThai "CHECK, index"
    }
    PhieuKham {
        int Id PK
        int LichKhamId FK_UK
    }
    DonThuoc {
        int Id PK
        int PhieuKhamId FK_UK
    }
    HoaDon {
        int Id PK
        int PhieuKhamId FK_UK
        decimal TongTien "CHECK >= 0"
        string TrangThaiThanhToan "CHECK"
        string PhuongThuc "CHECK, nullable"
    }
    DanhGia {
        int Id PK
        int BenhNhanId FK
        int BacSiId FK
        int LichKhamId FK_UK "nullable, moi"
        int SoSao "CHECK 1-5"
    }
    Thuoc {
        int Id PK
        int TonKho "CHECK >= 0"
        int NguongToiThieu "CHECK >= 0"
    }
    LoThuoc {
        int Id PK
        int ThuocId FK
        date NgayNhap "moi"
        date HanSuDung "CHECK >= NgayNhap"
        int SoLuongNhap "CHECK > 0"
        int SoLuongTon "CHECK 0..SoLuongNhap"
    }
    LichLamViecBacSi {
        int Id PK
        int BacSiId FK
        time GioBatDau
        time GioKetThuc "CHECK > GioBatDau"
        int SoBenhNhanToiDa "CHECK > 0"
        int ThoiLuongKhamPhut "CHECK 5-240"
    }
```

## 7. Chạy migration

**Trạng thái: đã áp dụng thành công lên `hms.db` thật** (migration
`20260730044849_DataIntegrityConstraints`, xác nhận trong
`__EFMigrationsHistory`). Toàn bộ 312 `NguoiDung`, 279 `BacSi`, 28 `BenhNhan`
và các bảng còn lại giữ nguyên số dòng sau migrate (không mất dữ liệu),
`PRAGMA foreign_key_check` không báo vi phạm. Đã khởi động lại ứng dụng thật,
đăng nhập Admin và tải các trang Dashboard/Medicines/Batches/Staff/Doctors/
Patients/Invoices/Appointments - tất cả trả về 200, không còn lỗi
`no such column`.

Lưu ý sự cố đã gặp và cách xử lý trong lần chạy đầu: **một tiến trình
`QuanLyBenhVien.exe` đang chạy từ trước** (dev server cũ) đã tự động thử
migrate lúc khởi động, thất bại (dùng bản build cũ hơn bản vá cuối), rồi vẫn
tiếp tục chạy do khối `try/catch` bọc quanh `Migrate()`/`Seed()` trong
`Program.cs` chỉ log lỗi chứ không dừng ứng dụng (hành vi có sẵn từ trước,
không phải thay đổi của migration này) - khiến ứng dụng phục vụ request với
schema cũ trong khi code đã theo model mới. Đã dừng tiến trình cũ, build lại,
và áp dụng migration thành công. Nếu gặp lại tình huống tương tự: dừng mọi
tiến trình `dotnet`/`QuanLyBenhVien.exe` đang chạy trước khi build/migrate lại.

Các bước đã thực hiện (để tái sử dụng cho môi trường khác):

```powershell
# 1) LUÔN sao lưu trước (script đã tạo sẵn):
.\scripts\backup-sqlite.ps1

# 2) (Khuyến nghị) chạy thử script làm sạch dữ liệu độc lập để đối chiếu:
#    xem scripts/data-cleanup.sql - có thể copy hms.db ra bản test rồi mở
#    bằng "DB Browser for SQLite" / bất kỳ client SQLite nào để chạy thử.

# 3) Dừng mọi tiến trình dotnet/QuanLyBenhVien.exe đang chạy (tránh khóa file build).

# 4) Áp dụng migration:
dotnet ef database update --context ApplicationDbContext

# Hoặc chạy ứng dụng - Program.cs gọi context.Database.Migrate() tự động khi
# khởi động cho provider Sqlite (xem ghi chú provider ở mục 8).
dotnet run
```

Nếu cần hoàn tác migration này (trước khi có migration mới nào khác dựa lên nó):

```powershell
dotnet ef database update <migration_truoc_do> --context ApplicationDbContext
dotnet ef migrations remove --context ApplicationDbContext
```

Khôi phục dữ liệu nếu cần:

```powershell
.\scripts\restore-sqlite.ps1 -BackupFile "backups\hms_<timestamp>.db"
```

## 8. Tương thích SQLite / SQL Server / MySQL

- **SQLite (đang dùng)**: đã kiểm thử trực tiếp - migration áp dụng thành công
  lên bản sao đầy đủ của `hms.db` (312 NguoiDung, 279 BacSi, 28 BenhNhan...),
  không mất dòng nào (`PRAGMA foreign_key_check` không báo vi phạm), và 11
  tình huống dữ liệu sai (SoSao ngoài khoảng, trùng Email, tồn kho âm,
  VaiTro/TrangThai/ChucVu không hợp lệ, xóa tài khoản còn hồ sơ bác sĩ, phiếu
  khám thứ 2 cho cùng lịch khám...) đều bị từ chối đúng như thiết kế.
- **SQL Server**: đính chính lại so với ghi chú ban đầu - `Program.cs` hiện
  cho **cả SQL Server lẫn MySQL** dùng `context.Database.EnsureCreated()`,
  không đi qua hệ thống Migrations (chỉ Sqlite mới gọi `Migrate()`). Điều này
  đã có từ trước (comment trong `Program.cs`: "legacy migrations ... generated
  for SQLite ... For SQL Server and MySQL, build the schema from the current
  model instead"), không phải thay đổi của lần này. Nghĩa là khi triển khai
  SQL Server, EF Core sẽ tạo bảng trực tiếp từ model hiện tại (đã bao gồm mọi
  UNIQUE/CHECK/FK ở trên) trên một database rỗng - script làm sạch dữ liệu
  trong migration sẽ không chạy (không cần thiết vì không có dữ liệu cũ).
  `ApplicationDbContext` vẫn tự phát hiện provider (`Database.ProviderName`)
  để dùng đúng cú pháp - `GETDATE()` thay `CURRENT_TIMESTAMP`, filtered UNIQUE
  INDEX (`WHERE [Col] IS NOT NULL`) vì SQL Server mặc định chỉ cho 1 giá trị
  NULL trong UNIQUE INDEX/CONSTRAINT (khác SQLite/MySQL coi mỗi NULL là khác
  biệt) - áp dụng cho trường hợp sau này có ai đó chuyển sang chạy migration
  thật trên SQL Server. Chưa kiểm thử trực tiếp trên SQL Server thật (dự án
  hiện chưa cấu hình instance nào) - dùng `scripts/backup-sqlserver.sql` nếu
  triển khai lên SQL Server đã có dữ liệu.
- **MySQL**: tương tự SQL Server, dùng `EnsureCreated()` (xem
  `docs/DATABASE-MYSQL.md`). Các CHECK constraint có được tạo hay không phụ
  thuộc vào hỗ trợ của `MySql.EntityFrameworkCore` khi dịch model sang DDL;
  nên xác minh riêng nếu triển khai MySQL production.

## 9.bis Sự cố phát hiện khi triển khai thật: 2 file `hms.db` song song

Khi áp dụng migration lần đầu qua Visual Studio, gặp lỗi `CHECK constraint
failed: CK_BacSi_ChucVu` dù đã kiểm thử kỹ trên bản sao `hms.db`. Điều tra
cho thấy nguyên nhân sâu hơn nhiều so với một CHECK constraint đơn thuần:

**Nguyên nhân gốc**: chuỗi kết nối `"Data Source=hms.db"` trong
`appsettings.json` là đường dẫn **tương đối**. SQLite/EF Core phân giải nó
theo thư mục làm việc hiện tại của tiến trình - và thư mục đó khác nhau tùy
cách khởi chạy:
- `dotnet run` / `dotnet ef` (terminal): thư mục làm việc là **gốc project**
  (`D:\QuanLyBenhVien`) → dùng `D:\QuanLyBenhVien\hms.db`.
- Visual Studio (F5/Debug): chạy trực tiếp file `.exe` đã build, thư mục làm
  việc mặc định là **thư mục build output** → dùng
  `D:\QuanLyBenhVien\bin\Debug\net10.0\hms.db` - một file HOÀN TOÀN KHÁC.

Hai file này đã tồn tại song song và **phân kỳ dữ liệu thật theo cả hai
hướng** qua nhiều phiên làm việc trước đó (một số bảng nhiều hơn ở file này,
một số bảng nhiều hơn ở file kia) - đây là một lỗi tiềm ẩn từ trước, migration
chỉ là dịp phát hiện ra nó (khi cả hai file có schema khác nhau, chạy nhầm
file sẽ gặp đúng loại lỗi "thiếu cột"/"CHECK vi phạm" như đã gặp).

**Xử lý đã thực hiện** (sau khi xác nhận với người dùng, không tự ý ghi đè):
1. Sao lưu CẢ HAI file (mỗi file 2 bản: trong `backups/` và một bản ngoài
   project) trước khi động vào bất kỳ file nào.
2. Xác định file `bin\Debug\net10.0\hms.db` chứa dữ liệu thật của người dùng
   (tài khoản `nongvannguyen20202023@gmail.com`, 34 lịch khám...) - nhiều hơn
   và quan trọng hơn dữ liệu ở file gốc (chủ yếu là tài khoản QA test).
3. Áp dụng cùng migration lên bản sao dữ liệu thật đó (làm sạch thêm một
   trường hợp mới phát hiện: 15 bác sĩ có `ChucVu` rỗng `''` - bổ sung vào
   câu lệnh `UPDATE` sẵn có, xem mục 2.2/lại `scripts/data-cleanup.sql`).
4. Thay nội dung `D:\QuanLyBenhVien\hms.db` bằng bản dữ liệu thật đã migrate
   (bản QA-test cũ đã lưu riêng tại
   `backups/hms_root_QAdata_superseded_*.db` nếu cần tham khảo lại).
5. **Sửa tận gốc** trong `Program.cs`: thêm hàm `ResolveSqliteConnectionString`
   neo mọi đường dẫn SQLite tương đối về đúng thư mục chứa `QuanLyBenhVien.csproj`
   (tìm bằng cách đi ngược thư mục từ `AppContext.BaseDirectory`), bất kể
   tiến trình được khởi chạy từ đâu. Nếu không tìm thấy `.csproj` (trường hợp
   publish/Docker, nơi mã nguồn không được deploy kèm theo), giữ nguyên hành
   vi cũ (dùng thư mục chứa file thực thi) - không ảnh hưởng triển khai
   production hiện tại.
6. Đã kiểm chứng bằng cách khởi chạy `.exe` với thư mục làm việc giả lập
   giống hệt Visual Studio (`bin\Debug\net10.0`) - log xác nhận
   `Content root path: ...\bin\Debug\net10.0` (giống hệt Visual Studio) nhưng
   `"No migrations were applied. The database is already up to date"` -
   tức là đã đọc đúng file gốc đã migrate, file `bin\Debug\net10.0\hms.db` cũ
   không còn bị đụng tới nữa (mtime không đổi sau khi chạy).

**Khuyến nghị cho người dùng**: từ nay, dù mở bằng Visual Studio hay chạy
`dotnet run`/`dotnet ef` từ terminal, ứng dụng đều chỉ đọc/ghi đúng một file
`D:\QuanLyBenhVien\hms.db`. File cũ trong `bin\Debug\net10.0` không còn được
dùng (an toàn, không cần xóa - thư mục `bin/` vốn được tạo lại mỗi lần build
và đã nằm trong `.gitignore`).

## 9. Phạm vi cố ý không thực hiện (và lý do)

- Không thêm bảng danh mục (lookup table) cho các trạng thái - dùng CHECK
  constraint theo đúng lựa chọn được liệt kê trong yêu cầu, tránh thêm bảng
  mới không cần thiết.
- Không thêm `NgayCapNhat` cho các bảng chưa có cột này (xem mục 2.6).
- Không thêm xóa mềm cho `PhieuKham`/`DonThuoc`/`HoaDon` (xem mục 3) - chưa có
  luồng xóa nào cần bảo vệ.
- Không ép `HocVi` vào danh sách cố định (xem mục 2.2).
- Không kiểm tra CHECK cho "ngày khám ≥ ngày đặt lịch" bằng ràng buộc CSDL:
  `LichKham.ThoiGian` (giờ hẹn) có thể sớm hơn thời điểm `NgayTao` (thời điểm
  bấm đặt) trong cùng một ngày (đặt lịch buổi sáng cho slot 08:00 lúc 09:00
  hôm đó là hợp lệ) - so sánh trọn vẹn ngày-giờ bằng CHECK sẽ từ chối nhầm các
  trường hợp hợp lệ này. Quy tắc "không đặt lịch cho ngày trong quá khứ" đã
  được kiểm tra đúng mức ở tầng ứng dụng (`BookController.ValidateBookingDate`,
  đã có từ trước, theo ngày - không theo giờ chính xác).
- Không viết trigger CSDL cho các quy tắc cần so sánh nhiều dòng (chồng ca,
  vượt sức chứa slot...) - các quy tắc này được xử lý ở tầng ứng dụng (mục 4)
  để nhất quán giữa 3 provider mà không cần viết/bảo trì trigger riêng cho
  từng loại CSDL.
