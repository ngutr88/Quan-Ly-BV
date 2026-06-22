# Stitch Gen Page Prompts - HMS

Tài liệu này liệt kê các page cần có cho giao diện Hệ thống Quản lý Bệnh viện (HMS) và prompt chi tiết cho từng page để dùng với Stitch Gen. Mỗi prompt tập trung vào nội dung, bố cục và trạng thái giao diện, không yêu cầu viết code.

Phong cách chung cho toàn bộ page:

- Ngôn ngữ giao diện: tiếng Việt.
- Chủ đề: bệnh viện hiện đại, sạch, tin cậy, dễ đọc, ưu tiên thao tác nghiệp vụ.
- Màu sắc: xanh y tế làm primary, nền sáng, neutral rõ ràng, màu cảnh báo cho lỗi/tồn kho/thanh toán.
- Tránh thiết kế dạng landing page. Đây là app vận hành thật.
- Dữ liệu mẫu dùng tên, khoa, bác sĩ, bệnh nhân và địa chỉ Việt Nam.
- Trạng thái luôn có text rõ ràng, không chỉ dùng màu.
- Admin và Bác sĩ desktop-first. Bệnh nhân mobile-first nhưng vẫn đẹp trên desktop.

## Danh sách page cần thiết

### MVP Core

1. Đăng nhập
2. Đăng ký bệnh nhân
3. Xác thực OTP
4. Quên mật khẩu
5. Admin Dashboard
6. Admin Quản lý lịch khám
7. Admin Chi tiết lịch khám
8. Admin Quản lý bác sĩ
9. Admin Chi tiết bác sĩ
10. Admin Quản lý bệnh nhân
11. Admin Chi tiết bệnh nhân
12. Admin Quản lý khoa và dịch vụ
13. Admin Quản lý thuốc
14. Admin Nhập kho và lô thuốc
15. Admin Quản lý hóa đơn
16. Admin Chi tiết hóa đơn
17. Bác sĩ Lịch khám hôm nay
18. Bác sĩ Phiên khám bệnh
19. Bác sĩ Kê đơn thuốc
20. Bác sĩ Hồ sơ bệnh nhân
21. Bệnh nhân Trang chủ
22. Bệnh nhân Đặt lịch khám
23. Bệnh nhân Lịch khám của tôi
24. Bệnh nhân Chi tiết đơn thuốc
25. Bệnh nhân Hóa đơn và thanh toán

### Nên có sau MVP

26. Admin Báo cáo thống kê
27. Admin Nhân sự và phân quyền
28. Admin Cấu hình hệ thống
29. Admin Nhật ký hoạt động
30. Bác sĩ Thống kê cá nhân
31. Bác sĩ Tin nhắn tư vấn
32. Bệnh nhân Hồ sơ sức khỏe cá nhân
33. Bệnh nhân Thông báo
34. Bệnh nhân Đánh giá dịch vụ
35. Bệnh nhân Hồ sơ người thân
36. Trang Không có quyền truy cập
37. Trang Không tìm thấy

---

## 01. Đăng nhập

```text
Tạo page Đăng nhập cho web app Hệ thống Quản lý Bệnh viện.

Bố cục:
- Nền sáng, sạch, có logo hoặc tên hệ thống "HMS - Quản lý Bệnh viện".
- Card đăng nhập ở trung tâm màn hình trên desktop; trên mobile card chiếm gần hết chiều ngang.
- Bên dưới tên hệ thống có dòng phụ: "Đăng nhập để tiếp tục quản lý lịch khám, hồ sơ và thanh toán".

Nội dung card:
- Tiêu đề "Đăng nhập".
- Input Email/Số điện thoại/Tên đăng nhập.
- Input Mật khẩu với nút hiện/ẩn mật khẩu.
- Checkbox "Ghi nhớ đăng nhập".
- Link "Quên mật khẩu?".
- Button chính "Đăng nhập".
- Divider nhỏ.
- Link "Bệnh nhân mới? Đăng ký tài khoản".

Demo nhanh:
- Một khu vực nhỏ "Tài khoản demo" với 3 pill/button: Admin, Bác sĩ, Bệnh nhân.
- Khi chọn demo, hiển thị mô tả ngắn vai trò: Admin quản trị hệ thống, Bác sĩ khám bệnh, Bệnh nhân đặt lịch.

Trạng thái:
- Lỗi sai thông tin đăng nhập.
- Tài khoản bị khóa.
- Loading khi bấm đăng nhập.
- Permission redirect nếu vai trò không hợp lệ.

Phong cách:
- Chuyên nghiệp, tin cậy, ít trang trí.
- Dùng icon bệnh viện hoặc shield nhỏ ở phần tiêu đề.
```

## 02. Đăng ký bệnh nhân

```text
Tạo page Đăng ký tài khoản cho Bệnh nhân trong HMS.

Bố cục:
- Form nhiều bước hoặc form 2 cột trên desktop, một cột trên mobile.
- Header đơn giản có logo HMS và link quay lại đăng nhập.
- Thanh tiến trình 3 bước: Thông tin tài khoản, Thông tin cá nhân, Xác thực OTP.

Nội dung:
- Tiêu đề "Đăng ký tài khoản bệnh nhân".
- Mô tả ngắn: "Tạo tài khoản để đặt lịch khám, xem đơn thuốc và thanh toán hóa đơn".
- Section Thông tin tài khoản: email hoặc SĐT, mật khẩu, nhập lại mật khẩu.
- Section Thông tin cá nhân: họ tên, ngày sinh, giới tính, số CCCD tùy chọn, số thẻ BHYT tùy chọn, địa chỉ, người liên hệ khẩn cấp tùy chọn.
- Checkbox đồng ý điều khoản sử dụng và chính sách bảo mật dữ liệu y tế.
- Button "Tiếp tục xác thực".

Validation hiển thị:
- Email/SĐT đã tồn tại.
- Mật khẩu quá yếu.
- Ngày sinh không hợp lệ.
- Chưa đồng ý điều khoản.

Trạng thái:
- Loading khi gửi thông tin.
- Empty không cần.
- Error network.

Phong cách:
- Thân thiện với bệnh nhân, dễ đọc, không quá nhiều thuật ngữ y khoa.
```

## 03. Xác thực OTP

```text
Tạo page Xác thực OTP cho bệnh nhân.

Bố cục:
- Card trung tâm màn hình.
- Header có icon bảo mật hoặc điện thoại.
- Có link quay lại chỉnh thông tin đăng ký.

Nội dung:
- Tiêu đề "Xác thực tài khoản".
- Text: "Mã OTP đã được gửi đến email/SĐT của bạn".
- Input OTP 6 ô số lớn, dễ bấm trên mobile.
- Countdown "Gửi lại mã sau 59 giây".
- Button "Xác thực".
- Link "Gửi lại mã" khi hết countdown.
- Ghi chú bảo mật: "Không chia sẻ mã OTP với bất kỳ ai".

Trạng thái:
- OTP sai.
- OTP hết hạn.
- Sai quá 5 lần: hiển thị alert "Tài khoản bị khóa tạm thời, vui lòng thử lại sau".
- Loading khi xác thực.
- Thành công: message "Tài khoản đã được kích hoạt".
```

## 04. Quên mật khẩu

```text
Tạo page Quên mật khẩu cho HMS.

Bố cục:
- Card gọn, cùng phong cách với Đăng nhập.
- Có bước nhập email/SĐT, xác thực OTP, đặt mật khẩu mới.

Nội dung:
- Tiêu đề "Khôi phục mật khẩu".
- Input Email hoặc SĐT.
- Button "Gửi mã xác thực".
- Sau khi gửi mã: input OTP, input mật khẩu mới, nhập lại mật khẩu mới.
- Link quay lại đăng nhập.

Trạng thái:
- Không tìm thấy tài khoản.
- Gửi OTP thành công.
- OTP sai/hết hạn.
- Mật khẩu mới không khớp.
- Đổi mật khẩu thành công.
```

---

# Admin Pages

Tất cả Admin page dùng chung app shell:

- Sidebar trái cố định trên desktop với các mục: Dashboard, Lịch khám, Bác sĩ, Bệnh nhân, Khoa & Dịch vụ, Thuốc & Kho, Hóa đơn, Nhân sự & Phân quyền, Báo cáo, Cấu hình, Nhật ký.
- Topbar có search toàn hệ thống, notification bell, avatar Admin, breadcrumb.
- Nội dung chính desktop-first, nhiều bảng, filter, KPI và action rõ ràng.

## 05. Admin Dashboard

```text
Tạo page Admin Dashboard cho HMS.

Bố cục:
- App shell Admin với sidebar và topbar.
- Header page: "Dashboard tổng quan", ngày hiện tại, bộ lọc thời gian: Hôm nay, 7 ngày, 30 ngày, Tùy chọn.
- Button "Xuất báo cáo" có menu PDF và Excel.

Nội dung chính:
- Hàng KPI cards:
  1. Tổng bác sĩ đang hoạt động / tổng khoa.
  2. Bệnh nhân đã đăng ký và bệnh nhân mới hôm nay.
  3. Lịch khám hôm nay, chia nhỏ: chờ xác nhận, đã xác nhận, hoàn thành, đã hủy.
  4. Doanh thu hôm nay và tháng này, có phần trăm tăng/giảm.
- Biểu đồ doanh thu theo ngày trong tháng.
- Biểu đồ lượt khám theo khoa.
- Bảng "Lịch khám sắp diễn ra trong 2 giờ tới": giờ, bệnh nhân, bác sĩ, khoa, trạng thái.
- Card "Cảnh báo vận hành":
  - Thuốc sắp hết hàng.
  - Thuốc sắp hết hạn.
  - Hóa đơn quá hạn.
  - Lịch khám xung đột.
- Bảng "Top bác sĩ" có avatar, khoa, lượt khám, rating.
- Widget "Nhật ký hoạt động gần đây": ai vừa thao tác gì, thời gian, module.

Trạng thái:
- Loading skeleton cho KPI và chart.
- Empty chart khi chưa có dữ liệu.
- Alert khi có cảnh báo nghiêm trọng.

Phong cách:
- Dày thông tin nhưng thoáng, ưu tiên khả năng scan nhanh.
```

## 06. Admin Quản lý lịch khám

```text
Tạo page Admin Quản lý lịch khám cho HMS.

Bố cục:
- Header: "Quản lý lịch khám".
- Actions: "Tạo lịch thủ công", "Xuất danh sách".
- Tabs: Danh sách, Calendar, Hàng đợi hôm nay.
- Filter bar: ngày/khoảng ngày, khoa, bác sĩ, trạng thái, ô tìm bệnh nhân/SĐT/mã lịch.

Tab Danh sách:
- Data table cột: Mã lịch, Thời gian, Bệnh nhân, SĐT, Khoa, Bác sĩ, Phòng, Trạng thái, Nguồn đặt, Hành động.
- Row actions: Xem chi tiết, Xác nhận, Từ chối, Dời lịch, Hủy lịch.
- Status badge: Chờ xác nhận, Đã xác nhận, Đang khám, Hoàn thành, Đã hủy, Vắng mặt.

Tab Calendar:
- Calendar ngày/tuần/tháng.
- Mỗi lịch là một event có giờ, tên bệnh nhân, bác sĩ, trạng thái.
- Cảnh báo visual khi có trùng lịch bác sĩ.

Tab Hàng đợi hôm nay:
- Danh sách số thứ tự check-in theo khoa.
- Cột: STT, bệnh nhân, giờ hẹn, giờ check-in, bác sĩ, trạng thái hiện tại.

Modal tạo lịch:
- Chọn bệnh nhân, khoa, dịch vụ, bác sĩ, ngày, khung giờ, phòng, lý do khám.
- Hiển thị cảnh báo nếu trùng lịch hoặc slot đã đầy.

Trạng thái:
- Empty khi không có lịch.
- Confirm dialog khi hủy/dời/từ chối.
- Toast sau khi xác nhận lịch và gửi thông báo.
```

## 07. Admin Chi tiết lịch khám

```text
Tạo page hoặc drawer Chi tiết lịch khám cho Admin.

Bố cục:
- Drawer bên phải hoặc page detail.
- Header có mã lịch, trạng thái, nút đóng/quay lại.
- Actions theo trạng thái: Xác nhận, Dời lịch, Hủy lịch, Đánh dấu vắng mặt, In phiếu hẹn.

Nội dung:
- Card thông tin bệnh nhân: họ tên, mã bệnh nhân, ngày sinh, giới tính, SĐT, BHYT nếu có.
- Card lịch hẹn: khoa, dịch vụ, bác sĩ, phòng, ngày giờ, nguồn đặt, lý do khám.
- Timeline trạng thái: tạo lịch, xác nhận, gửi nhắc lịch, check-in, bắt đầu khám, hoàn thành.
- Lịch sử thay đổi: ai thay đổi, thời gian, nội dung thay đổi.
- Thông báo đã gửi: email/SMS/in-app, trạng thái gửi.
- Khu vực ghi chú nội bộ cho lễ tân/admin.

Modal dời lịch:
- Ngày mới, khung giờ mới, bác sĩ mới nếu cần, lý do dời.
- Cảnh báo xung đột lịch.

Modal hủy lịch:
- Lý do hủy bắt buộc.
- Checkbox gửi thông báo cho bệnh nhân.
```

## 08. Admin Quản lý bác sĩ

```text
Tạo page Admin Quản lý bác sĩ.

Bố cục:
- Header: "Quản lý bác sĩ".
- Actions: "Thêm bác sĩ", "Xuất Excel/PDF".
- Filter bar: tên, khoa, chuyên môn, học vị, trạng thái hoạt động.

Nội dung:
- Data table cột: avatar, mã nhân viên, họ tên, khoa, chuyên môn, học vị, số năm kinh nghiệm, lượt khám tháng này, rating, trạng thái, hành động.
- Row actions: Xem hồ sơ, Sửa, Thiết lập lịch làm việc, Khóa/Mở tài khoản, Vô hiệu hóa.
- Summary cards nhỏ: tổng bác sĩ, đang hoạt động, đang nghỉ, lịch trực hôm nay.

Form thêm/sửa bác sĩ:
- Thông tin cá nhân: họ tên, ngày sinh, giới tính, SĐT, email, địa chỉ, CCCD, ảnh đại diện.
- Thông tin chuyên môn: khoa chính, khoa phụ, học hàm/học vị, chứng chỉ hành nghề, năm kinh nghiệm, mô tả chuyên môn.
- Tài khoản: username/email, trạng thái kích hoạt, gửi email kích hoạt.

Trạng thái:
- Empty khi chưa có bác sĩ.
- Confirm dialog khi khóa hoặc vô hiệu hóa.
- Validation email/SĐT/CCCD trùng.
```

## 09. Admin Chi tiết bác sĩ

```text
Tạo page Chi tiết bác sĩ cho Admin.

Bố cục:
- Header có avatar, họ tên, học vị, chuyên khoa, trạng thái.
- Actions: Sửa hồ sơ, Thiết lập lịch, Đặt lại mật khẩu, Khóa tài khoản.
- Tabs: Tổng quan, Lịch làm việc, Hiệu suất, Hồ sơ chuyên môn, Nhật ký.

Tab Tổng quan:
- Thông tin liên hệ, khoa phụ trách, mô tả chuyên môn.
- KPI: lượt khám tháng này, rating trung bình, số lịch sắp tới, tỷ lệ hoàn thành.

Tab Lịch làm việc:
- Calendar tuần.
- Ca sáng/chiều/tối, ngày nghỉ phép, ngày trực.
- Số bệnh nhân tối đa mỗi khung giờ.
- Button chỉnh lịch.

Tab Hiệu suất:
- Biểu đồ lượt khám theo tháng.
- Danh sách đánh giá gần đây từ bệnh nhân.

Tab Hồ sơ chuyên môn:
- Chứng chỉ hành nghề, bằng cấp đính kèm, ngày cấp/nơi cấp.

Tab Nhật ký:
- Thay đổi hồ sơ, tài khoản, lịch làm việc.
```

## 10. Admin Quản lý bệnh nhân

```text
Tạo page Admin Quản lý bệnh nhân.

Bố cục:
- Header: "Quản lý bệnh nhân".
- Actions: "Thêm bệnh nhân tại quầy", "Gộp hồ sơ", "Xuất danh sách".
- Filter bar: tên, SĐT, mã bệnh nhân, CCCD, BHYT, trạng thái tài khoản, nhóm rủi ro.

Nội dung:
- Data table cột: mã bệnh nhân, họ tên, ngày sinh, giới tính, SĐT, BHYT, lần khám gần nhất, số lịch sắp tới, trạng thái tài khoản, hành động.
- Row actions: Xem hồ sơ, Sửa thông tin, Khóa/Mở tài khoản, Gộp hồ sơ.
- Cảnh báo hồ sơ trùng lặp: tên + SĐT hoặc CCCD giống nhau.

Form thêm/sửa:
- Họ tên, ngày sinh, giới tính, địa chỉ, SĐT, email, CCCD, số BHYT, nhóm máu, người liên hệ khẩn cấp.
- Tiền sử bệnh, dị ứng thuốc/thực phẩm, ghi chú nội bộ.

Trạng thái:
- Không hiển thị chi tiết y tế nhạy cảm trực tiếp trên bảng.
- Confirm khi khóa tài khoản hoặc gộp hồ sơ.
```

## 11. Admin Chi tiết bệnh nhân

```text
Tạo page Chi tiết bệnh nhân cho Admin.

Bố cục:
- Header có họ tên, mã bệnh nhân, tuổi, giới tính, trạng thái tài khoản.
- Actions: Sửa thông tin, Tạo lịch khám, Khóa tài khoản, Gộp hồ sơ.
- Tabs: Tổng quan, Hồ sơ y tế, Lịch khám, Đơn thuốc, Hóa đơn, Ghi chú nội bộ, Nhật ký.

Tab Tổng quan:
- Thông tin cá nhân, liên hệ, CCCD, BHYT, người liên hệ khẩn cấp.
- Alert nếu có dị ứng hoặc bệnh nền quan trọng.
- KPI nhỏ: tổng lượt khám, hóa đơn chưa thanh toán, lịch sắp tới.

Tab Hồ sơ y tế:
- Tiền sử bệnh, bệnh nền, dị ứng.
- Chỉ số sức khỏe gần nhất: cân nặng, huyết áp, chiều cao nếu có.

Tab Lịch khám:
- Bảng lịch sử lịch khám, trạng thái, bác sĩ, khoa, lý do khám.

Tab Đơn thuốc:
- Danh sách đơn thuốc, ngày kê, bác sĩ, trạng thái phát thuốc.

Tab Hóa đơn:
- Danh sách hóa đơn, tổng tiền, trạng thái thanh toán.

Tab Ghi chú nội bộ:
- Ghi chú của admin/lễ tân, có thời gian và người tạo.
```

## 12. Admin Quản lý khoa và dịch vụ

```text
Tạo page Admin Quản lý khoa và dịch vụ.

Bố cục:
- Header: "Khoa và dịch vụ".
- Actions: "Thêm khoa", "Thêm dịch vụ".
- Layout 2 vùng: danh sách khoa bên trái hoặc tab trên cùng; chi tiết khoa và dịch vụ bên phải.

Nội dung Khoa:
- Danh sách khoa: mã khoa, tên khoa, vị trí, trưởng khoa, số bác sĩ, lượt khám tháng này, trạng thái.
- Detail khoa: mô tả, vị trí tòa/tầng/phòng, trưởng khoa, số bệnh nhân tối đa/ngày.

Nội dung Dịch vụ:
- Table dịch vụ thuộc khoa: tên dịch vụ, giá, thời gian thực hiện, mô tả, trạng thái.
- Form thêm/sửa dịch vụ: tên, khoa, giá VND, thời lượng, mô tả, trạng thái.

Trạng thái:
- Vô hiệu hóa khoa/dịch vụ thay vì xóa cứng.
- Cảnh báo nếu khoa còn bác sĩ hoặc lịch khám đang hoạt động.
```

## 13. Admin Quản lý thuốc

```text
Tạo page Admin Quản lý thuốc.

Bố cục:
- Header: "Quản lý thuốc".
- Actions: "Thêm thuốc", "Nhập kho", "Xuất báo cáo tồn kho".
- Filter bar: tên thuốc/SKU/hoạt chất, nhóm thuốc, nhà sản xuất, trạng thái tồn kho, hạn sử dụng.

Nội dung:
- KPI cards: tổng loại thuốc, sắp hết hàng, hết hàng, sắp hết hạn.
- Data table cột: SKU, tên thuốc, hoạt chất, đơn vị tính, dạng bào chế, nhà sản xuất, tồn hiện tại, ngưỡng tối thiểu, giá bán, trạng thái.
- Row actions: Xem chi tiết, Sửa, Nhập kho, Xem lô, Vô hiệu hóa.
- Status: Còn hàng, Sắp hết, Hết hàng, Sắp hết hạn.

Chi tiết thuốc drawer:
- Thông tin danh mục thuốc.
- Tồn kho tổng.
- Danh sách lô theo hạn sử dụng.
- Lịch sử nhập/xuất gần đây.

Trạng thái:
- Cảnh báo thuốc dưới ngưỡng tối thiểu.
- Cảnh báo lô thuốc hết hạn trong 30/60/90 ngày.
```

## 14. Admin Nhập kho và lô thuốc

```text
Tạo page hoặc modal Nhập kho thuốc và quản lý lô thuốc.

Bố cục:
- Header: "Nhập kho thuốc".
- Form nhập kho ở bên trái, preview lô nhập ở bên phải.

Form:
- Chọn thuốc.
- Số lô.
- Số lượng.
- Hạn sử dụng.
- Nhà cung cấp.
- Giá nhập.
- Ngày nhập.
- Số hóa đơn nhập tùy chọn.
- Ghi chú.

Preview:
- Thông tin thuốc đã chọn.
- Tồn kho hiện tại.
- Tồn kho sau nhập.
- Cảnh báo nếu hạn sử dụng quá gần hoặc số lượng không hợp lệ.

Danh sách lô:
- Table: số lô, thuốc, ngày nhập, hạn sử dụng, số lượng còn, trạng thái, nhà cung cấp.
- Filter theo thuốc, hạn sử dụng, trạng thái.

Trạng thái:
- Validation số lượng phải lớn hơn 0.
- Hạn sử dụng phải sau ngày nhập.
- Toast nhập kho thành công.
```

## 15. Admin Quản lý hóa đơn

```text
Tạo page Admin Quản lý hóa đơn.

Bố cục:
- Header: "Quản lý hóa đơn".
- Actions: "Tạo hóa đơn thủ công", "Xuất báo cáo".
- Filter bar: mã hóa đơn, bệnh nhân, ngày, khoa, trạng thái thanh toán, phương thức thanh toán.

Nội dung:
- KPI cards: doanh thu hôm nay, hóa đơn chưa thanh toán, hóa đơn quá hạn, doanh thu tháng.
- Data table cột: mã hóa đơn, ngày tạo, bệnh nhân, phiếu khám, khoa, tổng tiền, phương thức, trạng thái, người thu, hành động.
- Row actions: Xem chi tiết, Xác nhận thanh toán, Gửi nhắc thanh toán, Hủy hóa đơn, In hóa đơn.
- Status: Chưa thanh toán, Đã thanh toán, Đã hủy, Quá hạn.

Trạng thái:
- Số tiền định dạng VND.
- Hóa đơn quá hạn có alert nhẹ.
- Confirm dialog khi hủy hóa đơn, bắt buộc nhập lý do.
```

## 16. Admin Chi tiết hóa đơn

```text
Tạo page Chi tiết hóa đơn cho Admin.

Bố cục:
- Header có mã hóa đơn, trạng thái, tổng tiền.
- Actions: Xác nhận thanh toán, In hóa đơn, Xuất PDF, Hủy hóa đơn.
- Layout 2 cột: nội dung hóa đơn chính và panel thanh toán/audit.

Nội dung hóa đơn:
- Thông tin bệnh nhân: họ tên, mã bệnh nhân, SĐT, BHYT nếu có.
- Thông tin phiên khám: ngày khám, khoa, bác sĩ, mã phiếu khám.
- Bảng chi tiết phí:
  - Phí khám.
  - Phí xét nghiệm/cận lâm sàng nếu có.
  - Phí thuốc.
  - Dịch vụ khác.
  - BHYT/giảm giá nếu có.
  - Tổng tiền.
- Ghi chú hóa đơn.

Panel thanh toán:
- Trạng thái thanh toán.
- Phương thức: tiền mặt, chuyển khoản, thẻ, VNPay, Momo, ZaloPay.
- Thời gian thanh toán.
- Người xác nhận.
- Mã giao dịch online nếu có.

Audit trail:
- Tạo hóa đơn, chỉnh sửa, gửi nhắc, xác nhận thanh toán, hủy.

Modal xác nhận thanh toán:
- Chọn phương thức.
- Nhập số tiền nhận.
- Ghi chú.
```

## 17. Admin Báo cáo thống kê

```text
Tạo page Admin Báo cáo thống kê.

Bố cục:
- Header: "Báo cáo thống kê".
- Bộ lọc thời gian lớn: hôm nay, tuần này, tháng này, quý này, tùy chọn.
- Tabs: Doanh thu, Lượt khám, Kho thuốc, Công nợ, Hiệu suất bác sĩ.

Nội dung:
- Tab Doanh thu: chart doanh thu theo ngày, bảng doanh thu theo khoa, theo bác sĩ, theo loại dịch vụ.
- Tab Lượt khám: lượt khám theo khoa, top giờ cao điểm, tỷ lệ hủy/vắng mặt.
- Tab Kho thuốc: thuốc tiêu thụ nhiều, thuốc sắp hết, thuốc sắp hết hạn.
- Tab Công nợ: hóa đơn chưa thanh toán, quá hạn, tổng công nợ.
- Tab Hiệu suất bác sĩ: lượt khám, rating, tỷ lệ hoàn thành.

Actions:
- Xuất PDF.
- Xuất Excel.
- Lưu bộ lọc báo cáo.

Trạng thái:
- Loading chart.
- Empty report.
- Ghi chú "Dữ liệu báo cáo chỉ mang tính demo" nếu dùng mock data.
```

## 18. Admin Nhân sự và phân quyền

```text
Tạo page Admin Nhân sự và phân quyền.

Bố cục:
- Header: "Nhân sự và phân quyền".
- Tabs: Tài khoản nội bộ, Vai trò, Ma trận quyền.
- Actions: "Thêm nhân sự", "Tạo vai trò".

Tab Tài khoản nội bộ:
- Table: họ tên, email/SĐT, vai trò, khoa/phòng ban, trạng thái, đăng nhập gần nhất, hành động.
- Vai trò mở rộng: Lễ tân, Dược sĩ, Kế toán, Điều dưỡng.

Tab Vai trò:
- Danh sách vai trò và mô tả.
- Số người dùng trong từng vai trò.

Tab Ma trận quyền:
- Bảng module x action: xem, thêm, sửa, xóa/vô hiệu hóa, duyệt, xuất báo cáo.
- Module: lịch khám, bệnh nhân, bác sĩ, thuốc, hóa đơn, báo cáo, cấu hình.

Trạng thái:
- Confirm khi đổi quyền quan trọng.
- Alert: "Thay đổi quyền có thể ảnh hưởng đến truy cập dữ liệu nhạy cảm".
```

## 19. Admin Cấu hình hệ thống

```text
Tạo page Admin Cấu hình hệ thống.

Bố cục:
- Header: "Cấu hình hệ thống".
- Tabs: Giờ làm việc, Lịch khám, Kho thuốc, Thông báo, Thanh toán, Chung.

Nội dung:
- Giờ làm việc: ngày trong tuần, ca sáng/chiều/tối, ngày nghỉ.
- Lịch khám: thời lượng khám mặc định, số slot tối đa, thời gian nghỉ giữa ca, quy tắc hủy/dời lịch.
- Kho thuốc: ngưỡng tồn kho mặc định, cảnh báo hết hạn 30/60/90 ngày.
- Thông báo: template xác nhận lịch, nhắc lịch, hủy lịch, thanh toán, OTP.
- Thanh toán: bật/tắt VNPay, Momo, ZaloPay, tiền mặt, chuyển khoản.
- Chung: tên bệnh viện, địa chỉ, hotline, logo.

Actions:
- Lưu thay đổi.
- Khôi phục mặc định.
- Xem trước template thông báo.

Trạng thái:
- Confirm khi lưu cấu hình ảnh hưởng nghiệp vụ.
- Validation dữ liệu cấu hình.
```

## 20. Admin Nhật ký hoạt động

```text
Tạo page Admin Nhật ký hoạt động.

Bố cục:
- Header: "Nhật ký hoạt động".
- Filter bar: thời gian, người dùng, vai trò, module, hành động, mức độ.
- Table toàn màn hình.

Nội dung table:
- Thời gian.
- Người thao tác.
- Vai trò.
- Module.
- Hành động.
- Đối tượng.
- Kết quả.
- IP hoặc thiết bị nếu có mock.
- Nút xem chi tiết.

Detail drawer:
- Trước/sau thay đổi ở mức mô tả an toàn.
- Không hiển thị mật khẩu, OTP, token hoặc dữ liệu y tế quá chi tiết.

Mức độ:
- Thông tin.
- Cảnh báo.
- Nhạy cảm.
- Lỗi.

Use cases cần có log mẫu:
- Đổi quyền.
- Khóa tài khoản.
- Sửa hồ sơ bệnh nhân.
- Hủy lịch.
- Hủy hóa đơn.
- Nhập kho.
- Override cảnh báo thuốc.
```

---

# Bác sĩ Pages

Tất cả Bác sĩ page dùng app shell riêng:

- Sidebar: Lịch khám hôm nay, Hồ sơ bệnh nhân, Khám bệnh, Đơn thuốc, Tin nhắn, Thống kê cá nhân.
- Topbar: lịch làm việc hôm nay, notification, avatar bác sĩ.
- Desktop-first, thao tác nhanh, ít trang trí.

## 21. Bác sĩ Lịch khám hôm nay

```text
Tạo page Lịch khám hôm nay cho Bác sĩ trong HMS.

Bố cục:
- Header: "Lịch khám hôm nay", tên bác sĩ, khoa, ngày hiện tại.
- Bộ lọc: trạng thái, ca khám, tìm bệnh nhân.
- Layout 2 cột: danh sách lịch bên trái, chi tiết lịch được chọn bên phải.

Danh sách lịch:
- Card từng lịch: giờ, số thứ tự, tên bệnh nhân, tuổi/giới tính, lý do khám, trạng thái.
- Nhóm theo ca sáng/chiều/tối.
- Status: Đã xác nhận, Đang chờ check-in, Đang khám, Hoàn thành, Vắng mặt.

Chi tiết lịch:
- Thông tin bệnh nhân tóm tắt.
- Dị ứng/bệnh nền quan trọng dạng alert.
- Lịch sử khám gần nhất.
- Button "Bắt đầu khám" nếu đã check-in.
- Button "Xem hồ sơ".

Trạng thái:
- Empty nếu hôm nay không có lịch.
- Alert nếu bệnh nhân chưa check-in.
- Loading khi chuyển lịch.
```

## 22. Bác sĩ Phiên khám bệnh

```text
Tạo page Phiên khám bệnh cho Bác sĩ.

Bố cục:
- Header sticky: tên bệnh nhân, mã bệnh nhân, tuổi, giới tính, trạng thái phiên khám.
- Layout 3 vùng trên desktop:
  1. Sidebar tóm tắt bệnh nhân.
  2. Form khám chính.
  3. Panel lịch sử liên quan.

Sidebar bệnh nhân:
- Thông tin cơ bản.
- Dị ứng thuốc/thực phẩm.
- Bệnh nền.
- Chỉ số gần nhất.

Form khám chính:
- Triệu chứng.
- Chỉ số sinh tồn: nhiệt độ, mạch, huyết áp, SpO2, cân nặng.
- Chẩn đoán sơ bộ.
- Chỉ định cận lâm sàng: checkbox hoặc multi-select xét nghiệm/siêu âm/nội soi.
- Kết quả cận lâm sàng: vùng nhập hoặc upload mock.
- Chẩn đoán cuối cùng.
- Lời dặn.
- Button "Lưu nháp", "Kê đơn thuốc", "Hoàn tất khám".

Panel lịch sử:
- Lịch sử khám gần đây.
- Đơn thuốc cũ.
- Kết quả xét nghiệm gần đây.

Trạng thái:
- Autosave draft mock.
- Cảnh báo khi rời page chưa lưu.
- Không cho hoàn tất nếu thiếu chẩn đoán cuối cùng.
```

## 23. Bác sĩ Kê đơn thuốc

```text
Tạo page hoặc step Kê đơn thuốc cho Bác sĩ.

Bố cục:
- Header: "Kê đơn thuốc", gắn với bệnh nhân và phiếu khám hiện tại.
- Layout 2 cột: tìm và chọn thuốc bên trái, đơn thuốc hiện tại bên phải.

Tìm thuốc:
- Search theo tên thuốc, hoạt chất, SKU.
- Filter nhóm thuốc.
- Card/list thuốc hiển thị: tên thuốc, hoạt chất, dạng bào chế, tồn kho, giá, cảnh báo.
- Status tồn kho: Còn hàng, Sắp hết, Hết hàng.

Đơn thuốc hiện tại:
- Table các thuốc đã chọn: tên thuốc, liều dùng, cách dùng, số ngày, số lượng, ghi chú, hành động xóa.
- Tự tính tổng số lượng dựa theo liều và số ngày ở mock.
- Cảnh báo tương tác thuốc ở đầu danh sách nếu có.
- Cảnh báo dị ứng nếu thuốc trùng dị ứng bệnh nhân.
- Button "Lưu đơn thuốc", "Quay lại phiên khám".

Override cảnh báo:
- Nếu dị ứng/tương tác nghiêm trọng, hiển thị modal bắt buộc nhập lý do override hoặc chọn thuốc khác.

Trạng thái:
- Không cho lưu đơn rỗng nếu bác sĩ đã chọn bước kê đơn.
- Thuốc hết hàng hiển thị đề xuất thuốc thay thế cùng hoạt chất.
```

## 24. Bác sĩ Hồ sơ bệnh nhân

```text
Tạo page Hồ sơ bệnh nhân dành cho Bác sĩ.

Bố cục:
- Header: "Hồ sơ bệnh nhân", search bệnh nhân trong phạm vi được phân công.
- Nếu chọn bệnh nhân, hiển thị detail với tabs.

Nội dung:
- Card thông tin cá nhân: họ tên, tuổi, giới tính, SĐT, BHYT nếu có.
- Alert dị ứng và bệnh nền.
- Tabs: Tổng quan, Lịch sử khám, Đơn thuốc, Kết quả cận lâm sàng, Chỉ số sức khỏe.

Tab Tổng quan:
- Tiền sử bệnh, bệnh nền, dị ứng, người liên hệ khẩn cấp.

Tab Lịch sử khám:
- Timeline các lần khám: ngày, khoa, bác sĩ, chẩn đoán, lời dặn.

Tab Đơn thuốc:
- Danh sách đơn thuốc cũ, thuốc, liều dùng, thời gian.

Tab Kết quả cận lâm sàng:
- Danh sách kết quả xét nghiệm/siêu âm/nội soi mock.

Tab Chỉ số sức khỏe:
- Biểu đồ huyết áp, cân nặng, đường huyết nếu có mock.

Quy tắc:
- Bác sĩ chỉ xem bệnh nhân được phân công.
- Nếu không có quyền, hiển thị permission denied trong nội dung.
```

## 25. Bác sĩ Thống kê cá nhân

```text
Tạo page Thống kê cá nhân cho Bác sĩ.

Bố cục:
- Header: "Thống kê cá nhân".
- Bộ lọc thời gian: 7 ngày, 30 ngày, tháng này, tùy chọn.

Nội dung:
- KPI: số lượt khám, số bệnh nhân mới, tỷ lệ hoàn thành lịch, rating trung bình.
- Chart lượt khám theo ngày.
- Chart phân bố bệnh nhân theo khoa/dịch vụ hoặc nhóm chẩn đoán phổ biến.
- Danh sách đánh giá mới nhất từ bệnh nhân.
- Bảng lịch sử hoạt động khám gần đây.

Trạng thái:
- Empty khi chưa có dữ liệu.
- Không hiển thị doanh thu nếu vai trò bác sĩ không được phép xem.
```

## 26. Bác sĩ Tin nhắn tư vấn

```text
Tạo page Tin nhắn tư vấn cho Bác sĩ.

Bố cục:
- Layout inbox 3 cột: danh sách hội thoại, khung chat, panel bệnh nhân.
- Filter: chưa đọc, đang theo dõi, đã đóng.

Danh sách hội thoại:
- Tên bệnh nhân, avatar, tin nhắn cuối, thời gian, badge chưa đọc.

Khung chat:
- Tin nhắn text.
- Gửi file/ảnh ở trạng thái mock.
- Quick replies: "Vui lòng đặt lịch tái khám", "Uống thuốc theo đơn", "Liên hệ cấp cứu nếu có dấu hiệu nặng".

Panel bệnh nhân:
- Thông tin tóm tắt.
- Lịch khám gần nhất.
- Đơn thuốc gần nhất.
- Cảnh báo dị ứng.

Trạng thái:
- Empty khi chưa chọn hội thoại.
- Cảnh báo: "Tin nhắn tư vấn không thay thế cấp cứu".
```

---

# Bệnh nhân Pages

Bệnh nhân page ưu tiên mobile-first:

- Bottom navigation trên mobile: Trang chủ, Đặt lịch, Lịch khám, Hóa đơn, Hồ sơ.
- Desktop có sidebar hoặc top navigation nhẹ.
- Ngôn ngữ đơn giản, thao tác ít bước, card dễ đọc.

## 27. Bệnh nhân Trang chủ

```text
Tạo page Trang chủ Bệnh nhân cho HMS.

Bố cục mobile-first:
- Header chào người dùng: "Xin chào, Nguyễn Văn A".
- Card nổi bật "Lịch khám sắp tới" nếu có.
- Quick actions dạng icon button: Đặt lịch khám, Xem lịch, Xem đơn thuốc, Thanh toán hóa đơn, Hồ sơ sức khỏe.

Nội dung:
- Lịch khám sắp tới: ngày giờ, khoa, bác sĩ, trạng thái, button xem chi tiết/hủy/dời nếu được phép.
- Hóa đơn chưa thanh toán: mã hóa đơn, tổng tiền, button Thanh toán.
- Đơn thuốc gần nhất: ngày kê, bác sĩ, số thuốc, button Xem đơn.
- Thông báo mới: nhắc lịch, thanh toán, lời dặn.
- Card hồ sơ sức khỏe: BHYT, dị ứng, chỉ số gần nhất.

Trạng thái:
- Bệnh nhân mới chưa có lịch: empty state với CTA "Đặt lịch khám đầu tiên".
- Loading skeleton.
- Error tải dữ liệu.
```

## 28. Bệnh nhân Đặt lịch khám

```text
Tạo page Đặt lịch khám cho Bệnh nhân.

Bố cục:
- Wizard 4 bước rõ ràng:
  1. Chọn khoa/dịch vụ.
  2. Chọn bác sĩ.
  3. Chọn ngày và khung giờ.
  4. Xác nhận thông tin.
- Mobile-first, mỗi bước thành một màn hình/card.

Bước 1:
- Danh sách khoa với icon, mô tả ngắn, vị trí.
- Danh sách dịch vụ thuộc khoa: tên, giá, thời gian dự kiến.
- Search khoa/dịch vụ.

Bước 2:
- Card bác sĩ: avatar, họ tên, học vị, chuyên khoa, số năm kinh nghiệm, rating, lịch trống gần nhất.
- Option "Bất kỳ bác sĩ phù hợp" nếu không muốn chọn.

Bước 3:
- Calendar chọn ngày.
- Time slot dạng chip: sáng, chiều, tối.
- Slot đầy phải disabled và ghi "Đã đầy".

Bước 4:
- Tóm tắt: bệnh nhân, khoa, dịch vụ, bác sĩ, ngày giờ, phí dự kiến, lý do khám.
- Textarea lý do khám.
- Checkbox xác nhận thông tin đúng.
- Button "Đặt lịch".

Trạng thái:
- Không có slot trống: gợi ý ngày gần nhất.
- Đặt lịch thành công: màn hình success có mã lịch và nút thêm vào lịch.
```

## 29. Bệnh nhân Lịch khám của tôi

```text
Tạo page Lịch khám của tôi cho Bệnh nhân.

Bố cục:
- Header: "Lịch khám của tôi".
- Tabs: Sắp tới, Chờ xác nhận, Đã hoàn thành, Đã hủy.
- Search/filter theo khoa, bác sĩ, ngày.

Nội dung:
- Card lịch khám:
  - Ngày giờ.
  - Khoa/dịch vụ.
  - Bác sĩ.
  - Phòng/vị trí nếu có.
  - Trạng thái.
  - Lý do khám.
  - Action: Xem chi tiết, Dời lịch, Hủy lịch, Xem hóa đơn sau khi hoàn thành.

Chi tiết lịch:
- Timeline: đặt lịch, xác nhận, nhắc lịch, check-in, khám, hoàn thành.
- Thông tin chuẩn bị trước khi đi khám.
- QR check-in mock nếu lịch đã xác nhận.

Modal hủy/dời:
- Bắt buộc nhập lý do nếu lịch đã xác nhận.
- Hiển thị lưu ý chính sách hủy/dời.

Trạng thái:
- Empty "Bạn chưa có lịch khám nào".
- CTA "Đặt lịch khám".
```

## 30. Bệnh nhân Chi tiết đơn thuốc

```text
Tạo page Chi tiết đơn thuốc cho Bệnh nhân.

Bố cục:
- Header: "Đơn thuốc".
- Danh sách đơn thuốc ở page list hoặc dropdown nếu nhiều đơn.
- Chi tiết đơn thuốc dạng card dễ đọc.

Nội dung chi tiết:
- Thông tin đơn: mã đơn, ngày kê, bác sĩ, khoa, liên kết phiên khám.
- Lời dặn bác sĩ.
- Danh sách thuốc:
  - Tên thuốc.
  - Hàm lượng/dạng bào chế nếu có.
  - Liều dùng.
  - Cách dùng.
  - Số ngày dùng.
  - Số lượng.
  - Ghi chú.
- Cảnh báo: đọc kỹ hướng dẫn, không tự ý ngưng thuốc, liên hệ bác sĩ khi có phản ứng bất thường.
- Button: Tải PDF, In đơn, Đặt lịch tái khám.

Trạng thái:
- Empty nếu chưa có đơn thuốc.
- Thuốc đã hết liệu trình nếu có mock.
```

## 31. Bệnh nhân Hóa đơn và thanh toán

```text
Tạo page Hóa đơn và thanh toán cho Bệnh nhân.

Bố cục:
- Header: "Hóa đơn".
- Tabs: Chưa thanh toán, Đã thanh toán, Tất cả.
- Danh sách hóa đơn dạng card trên mobile, table trên desktop.

Card hóa đơn:
- Mã hóa đơn.
- Ngày tạo.
- Dịch vụ/khoa.
- Tổng tiền VND.
- Trạng thái.
- Button "Xem chi tiết" và "Thanh toán" nếu chưa thanh toán.

Chi tiết hóa đơn:
- Thông tin phiên khám.
- Bảng phí: phí khám, xét nghiệm, thuốc, dịch vụ khác, giảm trừ, tổng tiền.
- Phương thức thanh toán.
- Biên lai nếu đã thanh toán.

Thanh toán online mock:
- Chọn phương thức: VNPay, Momo, ZaloPay, chuyển khoản.
- Màn hình xác nhận thanh toán.
- State thành công, thất bại, timeout.
- Nếu thất bại, giữ trạng thái Chưa thanh toán và có button Thử lại.

Trạng thái:
- Empty khi chưa có hóa đơn.
- Warning hóa đơn quá hạn.
```

## 32. Bệnh nhân Hồ sơ sức khỏe cá nhân

```text
Tạo page Hồ sơ sức khỏe cá nhân cho Bệnh nhân.

Bố cục:
- Header: "Hồ sơ sức khỏe".
- Tabs: Thông tin cá nhân, Tiền sử & dị ứng, Chỉ số sức khỏe, Người liên hệ.

Tab Thông tin cá nhân:
- Họ tên, ngày sinh, giới tính, SĐT, email, địa chỉ, CCCD, BHYT.
- Button chỉnh sửa thông tin cho các trường được phép.

Tab Tiền sử & dị ứng:
- Bệnh nền.
- Dị ứng thuốc/thực phẩm.
- Ghi chú sức khỏe cá nhân.
- Alert "Thông tin này giúp bác sĩ kê đơn an toàn hơn".

Tab Chỉ số sức khỏe:
- Cards cân nặng, huyết áp, nhịp tim, đường huyết nếu có.
- Chart theo thời gian.
- Button "Thêm chỉ số" ở trạng thái mock.

Tab Người liên hệ:
- Người liên hệ khẩn cấp, quan hệ, SĐT.

Trạng thái:
- Bệnh nhân chưa hoàn thiện hồ sơ: progress card và CTA "Cập nhật hồ sơ".
```

## 33. Bệnh nhân Thông báo

```text
Tạo page Trung tâm thông báo cho Bệnh nhân.

Bố cục:
- Header: "Thông báo".
- Tabs/filter: Tất cả, Lịch khám, Thanh toán, Đơn thuốc, Hệ thống.
- Danh sách notification dạng card.

Nội dung card:
- Icon loại thông báo.
- Tiêu đề.
- Nội dung ngắn.
- Thời gian.
- Trạng thái đã đọc/chưa đọc.
- Action nếu có: Xem lịch, Thanh toán, Xem đơn thuốc.

Thông báo mẫu:
- Lịch khám đã được xác nhận.
- Nhắc lịch trước 24 giờ.
- Hóa đơn đã thanh toán.
- Thanh toán thất bại.
- Đơn thuốc mới đã được cập nhật.

Actions:
- Đánh dấu tất cả đã đọc.
- Cài đặt nhận thông báo.

Trạng thái:
- Empty "Bạn chưa có thông báo nào".
```

## 34. Bệnh nhân Đánh giá dịch vụ

```text
Tạo page hoặc modal Đánh giá bác sĩ và dịch vụ sau khám.

Bố cục:
- Modal sau khi phiên khám hoàn thành hoặc page từ lịch sử khám.
- Header: "Đánh giá buổi khám".
- Thông tin phiên khám: bác sĩ, khoa, ngày khám.

Nội dung:
- Rating sao cho bác sĩ.
- Rating sao cho dịch vụ bệnh viện.
- Tags nhanh: Đúng giờ, Tận tâm, Dễ hiểu, Cơ sở sạch, Chờ lâu, Cần cải thiện.
- Textarea nhận xét.
- Checkbox cho phép bệnh viện liên hệ lại.
- Button "Gửi đánh giá".

Trạng thái:
- Không cho gửi nếu chưa chọn rating.
- Cảm ơn sau khi gửi.
- Nếu đã đánh giá, hiển thị review đã gửi và disable sửa nếu mock policy như vậy.
```

## 35. Bệnh nhân Hồ sơ người thân

```text
Tạo page Hồ sơ người thân cho Bệnh nhân.

Bố cục:
- Header: "Người thân".
- Mô tả: "Quản lý hồ sơ phụ thuộc để đặt lịch hộ trẻ em hoặc người thân".
- Button "Thêm người thân".

Nội dung:
- Danh sách card người thân: họ tên, quan hệ, ngày sinh, giới tính, BHYT, trạng thái hồ sơ.
- Actions: Xem hồ sơ, Đặt lịch hộ, Sửa, Xóa liên kết.

Form thêm người thân:
- Họ tên, ngày sinh, giới tính, quan hệ, SĐT nếu có, BHYT/CCCD tùy chọn, người giám hộ.
- Checkbox xác nhận có quyền quản lý hồ sơ người thân.

Trạng thái:
- Empty nếu chưa có người thân.
- Confirm khi xóa liên kết hồ sơ.
```

---

# System Pages

## 36. Không có quyền truy cập

```text
Tạo page Không có quyền truy cập cho HMS.

Bố cục:
- Icon shield hoặc lock.
- Tiêu đề "Bạn không có quyền truy cập".
- Text giải thích: "Tài khoản hiện tại không được cấp quyền xem nội dung này".
- Hiển thị vai trò hiện tại.
- Button "Quay lại trang phù hợp" và "Đăng xuất".

Yêu cầu:
- Không tiết lộ dữ liệu hoặc tên resource nhạy cảm.
- Phù hợp cho cả Admin, Bác sĩ, Bệnh nhân.
```

## 37. Không tìm thấy

```text
Tạo page Không tìm thấy cho HMS.

Bố cục:
- Tiêu đề "Không tìm thấy trang".
- Mô tả: "Trang hoặc dữ liệu bạn đang tìm không tồn tại hoặc đã được di chuyển".
- Search nhỏ nếu phù hợp.
- Button "Về trang chủ".

Phong cách:
- Đơn giản, chuyên nghiệp, không dùng minh họa quá vui nhộn.
```

