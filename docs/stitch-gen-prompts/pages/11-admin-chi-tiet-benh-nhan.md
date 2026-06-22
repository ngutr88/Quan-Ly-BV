# 11. Admin Chi tiết bệnh nhân

```text
Tạo page Chi tiết bệnh nhân cho Admin.

Bối cảnh:
- Admin cần xem hồ sơ bệnh nhân, lịch sử khám, đơn thuốc, hóa đơn và ghi chú nội bộ.
- Đây là dữ liệu nhạy cảm, giao diện cần rõ ràng và có cảm giác được kiểm soát.

Bố cục:
- Header có họ tên, mã bệnh nhân, tuổi, giới tính, trạng thái tài khoản.
- Actions:
  - Sửa thông tin.
  - Tạo lịch khám.
  - Khóa tài khoản.
  - Gộp hồ sơ.
- Tabs:
  - Tổng quan.
  - Hồ sơ y tế.
  - Lịch khám.
  - Đơn thuốc.
  - Hóa đơn.
  - Ghi chú nội bộ.
  - Nhật ký.

Tab Tổng quan:
- Thông tin cá nhân: họ tên, ngày sinh, giới tính, SĐT, email, địa chỉ.
- CCCD, BHYT, người liên hệ khẩn cấp.
- Alert nếu có dị ứng hoặc bệnh nền quan trọng.
- KPI nhỏ:
  - Tổng lượt khám.
  - Hóa đơn chưa thanh toán.
  - Lịch sắp tới.
  - Lần khám gần nhất.

Tab Hồ sơ y tế:
- Tiền sử bệnh.
- Bệnh nền.
- Dị ứng thuốc/thực phẩm.
- Chỉ số sức khỏe gần nhất: cân nặng, huyết áp, chiều cao nếu có.

Tab Lịch khám:
- Bảng lịch sử lịch khám.
- Cột: ngày giờ, khoa, bác sĩ, lý do khám, trạng thái, hành động.

Tab Đơn thuốc:
- Danh sách đơn thuốc.
- Cột: ngày kê, bác sĩ, số thuốc, trạng thái phát thuốc, hành động xem chi tiết.

Tab Hóa đơn:
- Danh sách hóa đơn.
- Cột: mã hóa đơn, ngày tạo, tổng tiền, trạng thái thanh toán, phương thức.

Tab Ghi chú nội bộ:
- Ghi chú của admin/lễ tân.
- Hiển thị người tạo và thời gian.
- Form thêm ghi chú mới.

Tab Nhật ký:
- Các thay đổi hồ sơ, tài khoản, lịch khám và hóa đơn.

Trạng thái:
- Loading khi tải hồ sơ.
- Empty cho tab chưa có dữ liệu.
- Confirm khi khóa tài khoản hoặc gộp hồ sơ.
```

