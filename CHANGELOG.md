# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
