# Danh sách test case - Phiếu nhập kho thuốc

Phạm vi: validation (Yêu cầu 3) và luồng trạng thái (Yêu cầu 4) của tính năng
phiếu nhập kho tại `Areas/Admin/Controllers/MedicinesController.cs` +
`Areas/Admin/Views/Medicines/ReceiveBatch.cshtml`. Dự án hiện chưa có project
test tự động (không có `*.Tests.csproj`, không tham chiếu xUnit/NUnit/MSTest),
nên đây là checklist test case thủ công/kiểm thử theo kịch bản, không phải
code test. Cột "Đã xác minh" đánh dấu các case đã kiểm thử trực tiếp qua HTTP
thật (`curl` + phiên đăng nhập thật) trong quá trình phát triển tính năng này.

## A. Validation (Yêu cầu 3)

### A1. Hạn sử dụng

| # | Kịch bản | Bước | Kết quả mong đợi | Đã xác minh |
|---|---|---|---|---|
| A1.1 | Hạn dùng là ngày trong quá khứ | Chọn 1 thuốc, nhập Hạn dùng = hôm qua, Lưu nháp | Bị từ chối, lỗi inline "Hạn sử dụng phải ở trong tương lai" tại đúng ô Hạn dùng của dòng đó | ✅ (server, test 1) |
| A1.2 | Hạn dùng = hôm nay | Nhập Hạn dùng = hôm nay | Bị từ chối (không chấp nhận đúng ngày hiện tại, phải lớn hơn) | Suy ra từ điều kiện `<= DateTime.Today` trong `ValidateReceiptAsync` - chưa test riêng, khuyến nghị test thủ công |
| A1.3 | Hạn dùng còn dưới 6 tháng, chưa tích xác nhận | Nhập Hạn dùng = hôm nay + 5 tháng, không đánh dấu "Xác nhận vẫn nhập" | Client: hiện modal vàng "Thuốc cận date"; nếu cố tình bỏ qua và gửi thẳng lên server: bị từ chối với lỗi "Thuốc cận date - cần xác nhận trước khi lưu" | ✅ (server, test 1 và test 3) |
| A1.4 | Hạn dùng còn dưới 6 tháng, đã xác nhận | Xác nhận modal cận date (client set `XacNhanCanDate=true`) | Được chấp nhận, dòng vẫn hiển thị nhãn cảnh báo cận date màu vàng nhưng cho lưu | Chưa test qua HTTP (cần giả lập `XacNhanCanDate=true` trong payload) - khuyến nghị bổ sung |
| A1.5 | Hạn dùng hợp lệ (> 6 tháng) | Nhập Hạn dùng = hôm nay + 1 năm | Không có cảnh báo, được chấp nhận | ✅ (test tạo phiếu #1, #3 thành công) |
| A1.6 | Định dạng hiển thị dd/mm/yyyy | Mở form, quan sát ô Hạn dùng và các trang xem/in phiếu | `ReceiptDetails.cshtml`/`PrintReceipt.cshtml`/`PrintInspectionRecord.cshtml` hiển thị `dd/MM/yyyy`; ô nhập dùng `<input type="date">` (định dạng hiển thị theo locale trình duyệt/OS - xem mục "Phạm vi không làm" trong README tính năng) | Xác nhận qua code, chưa test trên trình duyệt vi-VN thật |

### A2. Số lượng

| # | Kịch bản | Bước | Kết quả mong đợi | Đã xác minh |
|---|---|---|---|---|
| A2.1 | Số lượng = 0 hoặc âm | Nhập SoLuong = -5 | Bị từ chối: "Số lượng phải là số nguyên dương" | ✅ (test 1) |
| A2.2 | Số lượng để trống | Không nhập SoLuong (mặc định 0 khi submit) | Bị từ chối cùng lỗi trên | Suy ra từ `line.SoLuong <= 0`, tương đương A2.1 |
| A2.3 | Số lượng thập phân cho ĐVT viên/ống/lọ | Nhập "10.5" vào ô số lượng khi thuốc có ĐVT "Viên" | Input HTML `type="number" step="1"` chặn nhập phần thập phân trên trình duyệt; model binding phía server nhận `SoLuong` kiểu `int` nên giá trị thập phân gửi thẳng qua API sẽ bị từ chối ở tầng model binding (400) trước khi tới logic nghiệp vụ | Chưa test qua trình duyệt thật; đã xác nhận kiểu dữ liệu server là `int` (không có ngoại lệ thập phân cho đơn vị khác - xem ghi chú "Phạm vi không làm" bên dưới) |
| A2.4 | Số lượng hợp lệ | Nhập 100 | Được chấp nhận, thành tiền tính đúng | ✅ (test tạo + duyệt phiếu #1: 100 x 5.000 x 1.05 = 525.000đ) |

### A3. Đơn giá

| # | Kịch bản | Bước | Kết quả mong đợi | Đã xác minh |
|---|---|---|---|---|
| A3.1 | Đơn giá âm | Nhập DonGia = -100 | Bị từ chối: "Đơn giá không được âm" | ✅ (test 1) |
| A3.2 | Đơn giá = 0 | Nhập DonGia = 0 (hàng viện trợ/tài trợ) | Được chấp nhận (CHECK chỉ yêu cầu `>= 0`) | Suy ra từ điều kiện, phù hợp với loại nhập "Viện trợ" |
| A3.3 | Định dạng phân cách hàng nghìn kiểu VN | Nhập "1000000" vào ô đơn giá, rời khỏi ô (blur) | Ô tự hiển thị "1.000.000"; khi tính thành tiền/gửi server, giá trị được parse lại đúng bằng `parseVNNumber()` bỏ dấu chấm | Đã xác nhận qua code JS (`formatVNInputNumber`/`parseVNNumber`); chưa test bằng trình duyệt thật (Playwright/Selenium không có sẵn trong môi trường) |
| A3.4 | %VAT ngoài khoảng 0-100 | Nhập PhanTramVAT = 150 | Bị từ chối: "% VAT phải nằm trong khoảng 0-100" | ✅ (test 1) |

### A4. Số lô

| # | Kịch bản | Bước | Kết quả mong đợi | Đã xác minh |
|---|---|---|---|---|
| A4.1 | Số lô để trống | Không nhập SoLo | Bị từ chối: "Số lô là bắt buộc" | ✅ (test 1) |
| A4.2 | Placeholder & helper text | Mở dòng chi tiết mới | Ô Số lô hiện placeholder "vd: B2024051" và dòng chữ nhỏ "Nhập theo số lô in trên bao bì" | ✅ (xác nhận trong `ReceiveBatch.cshtml`, dòng render `rowHtml`) |
| A4.3 | Tổ hợp (thuốc, số lô) đã tồn tại trong kho | Chọn thuốc + số lô trùng với 1 `LoThuoc` đã có, rời ô Số lô | AJAX `CheckExistingLot` trả `exists: true` → mở modal "Số lô đã tồn tại" hỏi cộng dồn hay tạo mới | ✅ (endpoint `CheckExistingLot` xác nhận qua code; luồng phê duyệt phía sau đã test đầy đủ ở mục B) |
| A4.4 | Chọn "Cộng dồn vào lô hiện có" rồi duyệt phiếu | `CongDonVaoLoHienCo=true`, duyệt phiếu | Không tạo `LoThuoc` mới; lô hiện có được cộng thêm đúng `SoLuongNhap`/`SoLuongTon`, cập nhật `GiaNhap`/`NhaCungCapId` theo giá mới nhất | ✅ **Đã test qua HTTP thật**: lô `B2026TEST01` từ 100/100 lên đúng 150/150 sau khi duyệt phiếu #3 với `CongDonVaoLoHienCo=true`, không phát sinh dòng `LoThuoc` trùng |
| A4.5 | Chọn "Tạo bản ghi mới" dù trùng số lô | `CongDonVaoLoHienCo=false` dù đã cảnh báo trùng | Khi duyệt, tạo `LoThuoc` mới độc lập (chấp nhận 2 lô cùng SoLo - hệ thống hiện không ràng buộc UNIQUE trên (ThuocId, SoLo), xem ghi chú thiết kế trong `ApplicationDbContext`) | Suy ra từ logic `ApproveReceipt` (chỉ tìm lô hiện có khi `CongDonVaoLoHienCo == true`), chưa test riêng nhánh này |

### A5. Bảng chi tiết trống / thiếu trường bắt buộc

| # | Kịch bản | Bước | Kết quả mong đợi | Đã xác minh |
|---|---|---|---|---|
| A5.1 | Gửi phiếu không có dòng thuốc nào | Xóa hết dòng, Lưu nháp | Bị từ chối: "Phiếu phải có ít nhất một dòng thuốc" (lỗi tại `chiTiet`, không phải lỗi từng ô) | ✅ (test 2) |
| A5.2 | Loại nhập "Mua NCC" nhưng chưa chọn NCC | Để trống Nhà cung cấp, loại nhập = MuaNCC | Bị từ chối: "Loại nhập Mua nhà cung cấp bắt buộc chọn nhà cung cấp" tại `header.nhaCungCapId` | ✅ (test 3) |
| A5.3 | Nhiều lỗi cùng lúc trên nhiều dòng | Gửi phiếu 2 dòng, mỗi dòng thiếu 1-2 trường khác nhau | Toàn bộ lỗi được trả về cùng lúc (mảng `errors`), mỗi lỗi có `field` dạng `chiTiet.<index>.<cot>` để tô đỏ đúng ô, không dừng lại ở lỗi đầu tiên | ✅ (test 1 trả về đủ 5 lỗi cùng lúc cho 1 dòng) |
| A5.4 | Báo lỗi không dùng alert chung chung | Gửi phiếu lỗi | Không có `alert()`/`confirm()` trình duyệt nào xuất hiện; lỗi hiện inline dưới từng ô (`field-error-msg`) + toast tổng "Vui lòng kiểm tra lại các ô được đánh dấu đỏ" | ✅ (xác nhận qua code `paintErrors()`, không gọi `window.alert`) |

## B. Luồng trạng thái (Yêu cầu 4)

### B1. Chuyển trạng thái cơ bản

| # | Kịch bản | Bước | Kết quả mong đợi | Đã xác minh |
|---|---|---|---|---|
| B1.1 | Tạo phiếu mới, Lưu nháp | Điền form hợp lệ, bấm "Lưu nháp" | Tạo `PhieuNhapKho` với `TrangThai=Nhap`, sinh `MaPhieu` dạng `PN-YYYY-XXXX` | ✅ (`PN-2026-0001` tạo thành công) |
| B1.2 | Gửi duyệt từ Nháp | Mở lại phiếu Nháp, bấm "Gửi duyệt" | `TrangThai` chuyển `Nhap → ChoDuyet` | ✅ |
| B1.3 | Duyệt phiếu (người có quyền) | Đăng nhập tài khoản có `DuocDuyetPhieuNhapKho=true`, bấm "Duyệt phiếu" | `TrangThai → DaDuyet`, ghi `NguoiDuyetId`/`NgayDuyet` | ✅ |
| B1.4 | Từ chối phiếu (kèm lý do) | Bấm "Từ chối", nhập lý do, xác nhận | `TrangThai → TuChoi`, lưu `LyDoTuChoi` chính xác | ✅ |
| B1.5 | Từ chối không nhập lý do | Bấm "Từ chối", để trống lý do | Bị chặn phía client (toast "Vui lòng nhập lý do từ chối"); nếu cố POST thẳng lên server, action trả về lỗi tương tự, không đổi trạng thái | Xác nhận qua code (`RejectReceipt` kiểm tra `string.IsNullOrWhiteSpace(lyDo)`), chưa test gọi trực tiếp server bỏ qua client |
| B1.6 | Hủy phiếu từ Nháp/Chờ duyệt/Từ chối | Bấm "Hủy phiếu" ở từng trạng thái | `TrangThai → DaHuy` | ✅ (test hủy từ trạng thái Nháp) |
| B1.7 | Không cho hủy phiếu Đã duyệt | Thử hủy 1 phiếu `DaDuyet` | Bị từ chối: "Không thể hủy phiếu đã duyệt hoặc đã hủy", trạng thái không đổi | Suy ra từ điều kiện `CancelReceipt`, chưa test riêng (đã test tương đương ở B2.2 cho việc CHỈNH SỬA phiếu đã duyệt) |
| B1.8 | Không cho hủy phiếu Đã hủy (hủy 2 lần) | Hủy 1 phiếu đã `DaHuy` | Bị từ chối tương tự B1.7 | Suy ra từ cùng điều kiện, chưa test riêng |

### B2. Khóa phiếu đã duyệt

| # | Kịch bản | Bước | Kết quả mong đợi | Đã xác minh |
|---|---|---|---|---|
| B2.1 | Mở lại phiếu Đã duyệt để sửa | Vào `/Admin/Medicines/ReceiveBatch/{id}` với id của phiếu `DaDuyet` | Bị chuyển hướng về trang chi tiết kèm thông báo "Phiếu này đã khóa..., không thể chỉnh sửa"; không hiện form chỉnh sửa | ✅ (xác nhận qua `ReceiveBatch` GET action; trang `ReceiptDetails` phiếu `DaDuyet` chỉ hiện nút In/Lập phiếu điều chỉnh, không có nút Chỉnh sửa) |
| B2.2 | Gọi thẳng SaveDraft/SubmitReceipt cho phiếu đã duyệt | POST `SaveDraft` với `id` của phiếu `DaDuyet` | Bị từ chối: "Phiếu đã gửi duyệt/đã duyệt/đã hủy, không thể chỉnh sửa" | Xác nhận qua code (`SaveReceiptAsync` kiểm tra `TrangThai != "Nhap" && != "TuChoi"`), chưa gọi trực tiếp API để test |
| B2.3 | Lập phiếu điều chỉnh tham chiếu phiếu gốc | Từ trang chi tiết phiếu `DaDuyet`, bấm "Lập phiếu điều chỉnh" | Mở form tạo phiếu MỚI với `PhieuGocId` gán sẵn, hiển thị nhãn "Phiếu điều chỉnh cho PN-...", phiếu gốc không bị sửa | ✅ (xác nhận route `ReceiveBatch?phieuGocId=`, hiển thị nhãn trong `ReceiveBatch.cshtml`/`ReceiptDetails.cshtml`) |

### B3. Phân quyền duyệt

| # | Kịch bản | Bước | Kết quả mong đợi | Đã xác minh |
|---|---|---|---|---|
| B3.1 | Admin thường (không có quyền duyệt) cố duyệt phiếu | Đăng nhập tài khoản `DuocDuyetPhieuNhapKho=false`, POST `ApproveReceipt` | Bị từ chối, `TrangThai` giữ nguyên `ChoDuyet`, không cộng kho | ✅ **Đã test qua HTTP thật** (dùng `admin@hms.com`) |
| B3.2 | Admin thường không thấy nút Duyệt/Từ chối | Xem trang chi tiết phiếu `ChoDuyet` bằng tài khoản không có quyền | Không hiển thị 2 nút này (chỉ hiện "Hủy phiếu") | ✅ (xác nhận `@if (canApprove)` trong `ReceiptDetails.cshtml`) |
| B3.3 | Admin có quyền duyệt (`admin.operations@hms.com`) | Đăng nhập, xem/duyệt phiếu | Thấy đủ nút Duyệt/Từ chối, duyệt thành công | ✅ |
| B3.4 | Cấp/thu quyền duyệt qua Staff/Edit | Vào `/Admin/Staff/Edit/{id}`, tick/bỏ tick "Được phép duyệt phiếu nhập kho thuốc" | `NguoiDung.DuocDuyetPhieuNhapKho` cập nhật đúng theo checkbox | Xác nhận qua code (`Edit` action gán trực tiếp tham số vào field), chưa test UI trực tiếp qua trình duyệt |

### B4. Cập nhật tồn kho khi duyệt

| # | Kịch bản | Bước | Kết quả mong đợi | Đã xác minh |
|---|---|---|---|---|
| B4.1 | Duyệt phiếu tạo lô mới | Duyệt phiếu có dòng số lô chưa từng tồn tại | Tạo `LoThuoc` mới với đúng `SoLuongNhap=SoLuongTon=SoLuong`, `GiaNhap`, `NhaCungCapId`; `Thuoc.TonKho` cộng thêm đúng số lượng | ✅ (phiếu #1: tạo lô `B2026TEST01` 100/100, `Thuoc.TonKho` 480→580) |
| B4.2 | Duyệt phiếu cộng dồn lô có sẵn | Duyệt phiếu với `CongDonVaoLoHienCo=true` trên số lô đã tồn tại | Không tạo lô mới; `SoLuongNhap`/`SoLuongTon` của lô hiện có tăng đúng; `Thuoc.TonKho` cộng thêm đúng | ✅ (phiếu #3: lô `B2026TEST01` 100/100 → 150/150, `Thuoc.TonKho` 580→630) |
| B4.3 | Phiếu bị từ chối/hủy không ảnh hưởng tồn kho | Từ chối phiếu #2, hủy phiếu #4 | `Thuoc.TonKho` của các thuốc liên quan không đổi | ✅ (xác nhận qua kiểm tra DB: `Thuoc` Id=2/Id=3 giữ nguyên `TonKho` sau khi từ chối/hủy) |
| B4.4 | Duyệt phiếu nhiều dòng | Phiếu có 3+ dòng thuốc khác nhau | Mỗi dòng xử lý độc lập đúng logic B4.1/B4.2 trong cùng 1 transaction; nếu 1 dòng lỗi, toàn bộ rollback | Chưa test (các phiếu test đều chỉ có 1 dòng) - khuyến nghị bổ sung test với phiếu nhiều dòng |

### B5. Nhật ký kiểm toán (Audit log)

| # | Kịch bản | Bước | Kết quả mong đợi | Đã xác minh |
|---|---|---|---|---|
| B5.1 | Ghi log khi tạo/gửi duyệt/duyệt/từ chối/hủy | Thực hiện lần lượt 5 hành động | Mỗi hành động tạo đúng 1 dòng `NhatKyHeThong` với `HanhDong` tương ứng, `DoiTuongLoai="PhieuNhapKho"`, `DoiTuongId` đúng ID phiếu, `NguoiDungId` đúng người thực hiện | ✅ (xác nhận nội dung log của phiếu #1: "Tạo phiếu nhập kho" → "Gửi duyệt phiếu nhập kho" → "Duyệt phiếu nhập kho", đúng thứ tự và nội dung số tiền) |
| B5.2 | Nội dung log phản ánh đúng dữ liệu thay đổi | Kiểm tra `ChiTiet` của log duyệt phiếu | Chứa đúng mã phiếu, tổng thanh toán, số dòng thuốc | ✅ |

### B6. In ấn (chỉ khi Đã duyệt)

| # | Kịch bản | Bước | Kết quả mong đợi | Đã xác minh |
|---|---|---|---|---|
| B6.1 | In phiếu nhập kho khi chưa duyệt | Truy cập `/Admin/Medicines/PrintReceipt/{id}` với phiếu `Nhap`/`ChoDuyet` | Bị từ chối, chuyển hướng về trang chi tiết kèm thông báo "Chỉ có thể in phiếu đã được duyệt" | Suy ra từ điều kiện trong action, chưa test riêng (đã test chiều ngược lại ở B6.2/B6.3) |
| B6.2 | In phiếu nhập kho khi đã duyệt | Truy cập trang in với phiếu `DaDuyet` | Trả về 200, hiển thị đầy đủ header bệnh viện, bảng chi tiết, tổng tiền, 4 ô ký tên (Người giao hàng/Người lập phiếu/Thủ kho/Trưởng khoa Dược) | ✅ |
| B6.3 | In biên bản kiểm nhập khi đã duyệt | Truy cập `/Admin/Medicines/PrintInspectionRecord/{id}` | Trả về 200, hiển thị đúng bảng kiểm tra (kết quả "Đạt" từng dòng) và kết luận, 4 ô ký tên hội đồng kiểm nhập | ✅ |
| B6.4 | Bố cục in A4 | Mở 2 trang in, kiểm tra CSS `@@page { size: A4; }` | Có khai báo kích thước A4 và margin phù hợp, ẩn thanh nút "In phiếu"/"In biên bản" khi in (`@@media print`) | Xác nhận qua code, chưa test bằng in thử/xuất PDF thật |

## C. Ghi chú phạm vi (để người kiểm thử không báo nhầm là lỗi)

- **Số lượng thập phân cho đơn vị không phải viên/ống/lọ**: hệ thống hiện lưu
  số lượng dưới dạng số nguyên cho MỌI đơn vị tính (giữ nguyên kiểu dữ liệu
  `int` sẵn có của `Thuoc.TonKho`/`LoThuoc.SoLuongNhap`/`SoLuongTon` để không
  phải sửa dây chuyền sang `ExamController`, `PrescriptionDetail.SoLuong`...).
  Đây là quyết định phạm vi có chủ đích, không phải thiếu sót.
- **Quét mã vạch/DataMatrix GS1, import Excel, cảnh báo giá trúng thầu, luồng
  2 người xác nhận thuốc gây nghiện/hướng thần**: đúng như đánh dấu TODO ở
  Yêu cầu 6, chưa triển khai trong lần này.
- **Định dạng ngày dd/mm/yyyy trên ô nhập**: dùng `<input type="date">` gốc
  của trình duyệt (không có thư viện datepicker nào sẵn có trong dự án để
  ép định dạng hiển thị tuyệt đối) - hiển thị phụ thuộc locale hệ điều hành/
  trình duyệt của người dùng, thường đã là dd/mm/yyyy trên máy cấu hình
  tiếng Việt.
