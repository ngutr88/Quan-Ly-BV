# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
