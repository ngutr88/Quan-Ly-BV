# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.1] - 2026-07-15

### Added
- Thêm trang Landing Page giới thiệu tổng quan Bệnh viện (`Home/Index`) tích hợp đầy đủ thông tin giới thiệu, các chuyên khoa nổi bật, thống kê, quy trình y tế số hóa, ý kiến bệnh nhân, giờ làm việc và thông tin liên hệ.
- Tích hợp cụm nút Đăng nhập / Đăng ký khám trực tuyến ở góc trên bên phải thanh điều hướng của Landing Page.
- Bổ sung ảnh minh họa vector đại diện cho bệnh viện số tại `/images/hospital_hero_banner.png`.

### Changed
- Cấu hình bổ sung middleware `app.UseStaticFiles()` trong `Program.cs` để hỗ trợ hiển thị hình ảnh và các tệp tĩnh từ thư mục `wwwroot`.
- Cải tiến `HomeController.Index` tự động chuyển hướng người dùng đã đăng nhập về Dashboard của họ, và hiển thị trang Landing Page đối với khách vãng lai.
- Đồng nhất hoàn toàn màu sắc chủ đạo của phân hệ Bác sĩ (Doctor views & layout) sang màu xanh dương (`primary`) giống Admin và Bệnh nhân.
- Cập nhật active state của Sidebar nav, focus ring ô tìm kiếm, và avatar của bác sĩ trong `_DoctorLayout` sang màu xanh dương.
- Đồng nhất màu sắc của các thẻ ca khám chiều trong hàng đợi khám (`Queue/Index`) sang màu xanh dương.
- Chuyển đổi avatar người dùng/bệnh nhân trong phần nhắn tin tư vấn (`Chat/Index`) sang tông màu xanh dương.
- Giữ nguyên màu xanh lá (`secondary`) cho các trạng thái trực tuyến, tick đã xem tin nhắn, và các badge trạng thái nghiệp vụ (đã khám xong, đã thanh toán).

## [1.2.0] - 2026-07-14

### Added
- Thống kê cá nhân Bác sĩ (`/Doctor/Stats`) hiển thị KPIs (hôm nay, tháng này, năm này, trung bình đánh giá), biểu đồ lượt khám 6 tháng, phân bổ sao đánh giá bệnh nhân, top bệnh nhân tái khám và đánh giá gần đây.

### Changed
- Đồng nhất Sidebar, Topbar, Logo, Avatar và Footer trên 3 layout cốt lõi của hệ thống (`_AdminLayout`, `_DoctorLayout`, `_PatientLayout`) để đảm bảo trải nghiệm thương hiệu nhất quán (MediFlow HMS - icon `medical_services`).
- Chuyển đổi toàn bộ màu sắc chủ đạo của phân hệ Bác sĩ (Doctor views) sang màu xanh dương (`primary`) giống Admin và Patient nhằm đồng bộ giao diện, chỉ giữ màu xanh lá (`secondary`) cho các badge trạng thái nghiệp vụ và trạng thái trực tuyến.
- Ẩn thanh tìm kiếm mockup tĩnh trên Topbar của Admin Layout theo mặc định (chỉ hiển thị khi `ViewData["ShowTopSearch"] == true`), tránh dư thừa và chồng chéo với các bộ lọc tìm kiếm chuyên biệt đã có ở nội dung chính của các trang.

## [1.1.0] - 2026-06-23

### Added
- Bổ sung model `Dependent` và đăng ký DbSet, Fluent API liên kết 1-Nhiều với `Patient` để quản lý hồ sơ người thân.
- Triển khai chức năng thêm hồ sơ người thân và lưu trữ trực tiếp vào cơ sở dữ liệu (SQLite) thông qua AJAX POST.
- Tích hợp tính năng đặt lịch khám hộ cho người thân, tự động lựa chọn người bệnh từ tham số URL và lưu thông tin khám hộ trực tiếp vào trường lý do khám.
- Bổ sung chức năng đánh giá bác sĩ trực tuyến (1-5 sao và nhận xét) trên trang lịch sử khám bệnh.
- Bổ sung trường `MaGiaoDich` (mã giao dịch) vào model `Invoice` và cập nhật database schema thông qua EF Core Migration.
- Nâng cấp trang hóa đơn thanh toán bệnh nhân (Patient/Payment) với Dashboard tài chính, bộ lọc nâng cao (mã hóa đơn, ngày tháng, trạng thái, phương thức), sidebar chi tiết breakdown phí, tính năng in/tải PDF biên lai và gửi email AJAX.
- Triển khai trang Cổng thanh toán mô phỏng (Simulate) hỗ trợ nhiều phương thức (VNPay, MoMo, ZaloPay, Chuyển khoản, Tại quầy) và 3 kịch bản kết quả (Thành công, Thất bại, Đang xử lý) để kiểm thử luồng nghiệp vụ hoàn chỉnh.
- Thêm bảng Lịch sử giao dịch trực tuyến ghi nhận lịch sử các nỗ lực thanh toán của bệnh nhân.
- Cập nhật dữ liệu seeding (`DbSeeder.cs`) tự động thiết lập phong phú dữ liệu hóa đơn mẫu phục vụ demo.

### Fixed
- Sửa lỗi truy vấn đơn thuốc chi tiết trong `RecordController.PrescriptionDetails` bằng cách so khớp với `PhieuKhamId` thay vì `Prescription.Id`.
- Đồng bộ hóa liên kết xem đơn thuốc gần đây từ Dashboard bệnh nhân truyền `PhieuKhamId`.
- Khắc phục lỗi hiển thị nút đơn thuốc rỗng bằng cách chỉ hiển thị liên kết đơn thuốc đối với các ca khám thực sự có kê đơn trong DB.
- Cải thiện thẩm mỹ Sổ sức khỏe điện tử bằng cách thêm trạng thái Empty State trực quan khi chưa có dữ liệu đo đạc chỉ số sinh tồn.
- Khắc phục lỗi khởi tạo cơ sở dữ liệu trên Visual Studio: Thay thế `EnsureCreated()` bằng `Migrate()` trong `DbSeeder` để áp dụng đầy đủ migrations (bao gồm bảng `NguoiThan`) khi chạy ứng dụng trên bất kỳ môi trường nào.


## [1.0.0] - 2026-06-21

### Added
- Thêm quy trình làm việc tự động cho agent (ghi chép nhật ký timelines và cập nhật CHANGELOG).
- Khởi tạo cấu trúc dự án ASP.NET Core MVC đầy đủ cho Hệ thống Quản lý Bệnh viện.
- Thêm các Models cốt lõi: `NguoiDung`, `BacSi`, `BenhNhan`, `Khoa`, `DichVu`, `LichKham`, `PhieuKham`, `DonThuoc`, `Thuoc`, `LoThuoc`, `HoaDon`, `ThongBao`, `NhatKyHeThong`.
- Triển khai phân quyền người dùng (Cookie Authentication & RBAC) phân biệt rõ Admin, Bác sĩ, Bệnh nhân.
- Tích hợp DbSeeder tự động tạo dữ liệu mẫu phong phú và hợp lệ (khoa, bác sĩ, thuốc theo lô, lịch khám mẫu).
- Xây dựng giao diện dùng chung Layout với Tailwind CSS và Material Icons.
- Thiết kế Dashboard trực quan cho từng đối tượng (Admin, Doctor, Patient).

### Fixed
- Đổi port mặc định sang `5233` (HTTP) và `7233` (HTTPS) để tránh xung đột cổng khi chạy cục bộ.
- Sửa lỗi truy vấn LINQ `string.Split` không tương thích với EF Core SQLite trong `AuthController.Login` bằng hàm `StartsWith`.
- Sửa lỗi điều hướng phân quyền Bác sĩ sang đúng `DashboardController` của Doctor Area thay vì `QueueController` lỗi.
- Khắc phục lỗi lồng thẻ `<body>` không hợp lệ ở trang đăng nhập, căn giữa card đăng nhập và khung demo bằng Flexbox.

### Verified
- Kiểm thử thành công toàn bộ giao diện và chức năng đăng nhập, phân quyền, hiển thị Dashboard của cả 3 vai trò Admin, Bác sĩ và Bệnh nhân thông qua Browser Automation Agent.
- Xây dựng giao diện dùng chung Layout với Tailwind CSS và Material Icons.
- Thiết kế Dashboard trực quan cho từng đối tượng (Admin, Doctor, Patient).

### Fixed
- Đổi port mặc định sang `5233` (HTTP) và `7233` (HTTPS) để tránh xung đột cổng khi chạy cục bộ.
- Sửa lỗi truy vấn LINQ `string.Split` không tương thích với EF Core SQLite trong `AuthController.Login` bằng hàm `StartsWith`.
- Sửa lỗi điều hướng phân quyền Bác sĩ sang đúng `DashboardController` của Doctor Area thay vì `QueueController` lỗi.
- Khắc phục lỗi lồng thẻ `<body>` không hợp lệ ở trang đăng nhập, căn giữa card đăng nhập và khung demo bằng Flexbox.

### Verified
- Kiểm thử thành công toàn bộ giao diện và chức năng đăng nhập, phân quyền, hiển thị Dashboard của cả 3 vai trò Admin, Bác sĩ và Bệnh nhân thông qua Browser Automation Agent.
