# 08. Admin Quản lý bác sĩ

```text
Tạo page Admin Quản lý bác sĩ.

Bối cảnh:
- Admin quản lý hồ sơ, chuyên môn, lịch làm việc và tài khoản bác sĩ.
- Page desktop-first với bảng dữ liệu rõ ràng.

App shell:
- Dùng Admin sidebar và topbar.
- Breadcrumb: Dashboard / Bác sĩ.

Bố cục:
- Header: "Quản lý bác sĩ".
- Actions:
  - "Thêm bác sĩ".
  - "Xuất Excel/PDF".
- Summary cards:
  - Tổng bác sĩ.
  - Đang hoạt động.
  - Đang nghỉ.
  - Lịch trực hôm nay.
- Filter bar:
  - Tên.
  - Khoa.
  - Chuyên môn.
  - Học vị.
  - Trạng thái hoạt động.

Data table:
- Avatar.
- Mã nhân viên.
- Họ tên.
- Khoa.
- Chuyên môn.
- Học vị.
- Số năm kinh nghiệm.
- Lượt khám tháng này.
- Rating.
- Trạng thái.
- Hành động.

Row actions:
- Xem hồ sơ.
- Sửa.
- Thiết lập lịch làm việc.
- Khóa/Mở tài khoản.
- Vô hiệu hóa.

Form thêm/sửa bác sĩ:
- Thông tin cá nhân:
  - Họ tên.
  - Ngày sinh.
  - Giới tính.
  - SĐT.
  - Email.
  - Địa chỉ.
  - CCCD.
  - Ảnh đại diện.
- Thông tin chuyên môn:
  - Khoa chính.
  - Khoa phụ.
  - Học hàm/học vị.
  - Chứng chỉ hành nghề.
  - Năm kinh nghiệm.
  - Mô tả chuyên môn.
- Tài khoản:
  - Username/email.
  - Trạng thái kích hoạt.
  - Tùy chọn gửi email kích hoạt.

Trạng thái:
- Empty khi chưa có bác sĩ.
- Confirm dialog khi khóa hoặc vô hiệu hóa.
- Validation email/SĐT/CCCD trùng.
```

