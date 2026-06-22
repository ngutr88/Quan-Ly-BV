# 27. Admin Nhân sự và phân quyền

```text
Tạo page Admin Nhân sự và phân quyền.

Bối cảnh:
- Admin quản lý tài khoản nội bộ và phân quyền theo vai trò.
- Page cần chuẩn bị cho vai trò mở rộng: Lễ tân, Dược sĩ, Kế toán, Điều dưỡng.

App shell:
- Dùng Admin sidebar và topbar.
- Breadcrumb: Dashboard / Nhân sự & Phân quyền.

Bố cục:
- Header: "Nhân sự và phân quyền".
- Actions:
  - "Thêm nhân sự".
  - "Tạo vai trò".
- Tabs:
  - Tài khoản nội bộ.
  - Vai trò.
  - Ma trận quyền.

Tab Tài khoản nội bộ:
- Table:
  - Họ tên.
  - Email/SĐT.
  - Vai trò.
  - Khoa/phòng ban.
  - Trạng thái.
  - Đăng nhập gần nhất.
  - Hành động.
- Vai trò mẫu:
  - Admin.
  - Lễ tân.
  - Dược sĩ.
  - Kế toán.
  - Điều dưỡng.
- Row actions:
  - Xem chi tiết.
  - Sửa.
  - Khóa/Mở tài khoản.
  - Đặt lại mật khẩu.

Tab Vai trò:
- Danh sách vai trò.
- Mô tả vai trò.
- Số người dùng trong từng vai trò.
- Button chỉnh sửa quyền.

Tab Ma trận quyền:
- Bảng module x action:
  - Xem.
  - Thêm.
  - Sửa.
  - Xóa/Vô hiệu hóa.
  - Duyệt.
  - Xuất báo cáo.
- Module:
  - Lịch khám.
  - Bệnh nhân.
  - Bác sĩ.
  - Thuốc.
  - Hóa đơn.
  - Báo cáo.
  - Cấu hình.

Trạng thái:
- Confirm khi đổi quyền quan trọng.
- Alert: "Thay đổi quyền có thể ảnh hưởng đến truy cập dữ liệu nhạy cảm".
- Empty khi chưa có nhân sự.
```

