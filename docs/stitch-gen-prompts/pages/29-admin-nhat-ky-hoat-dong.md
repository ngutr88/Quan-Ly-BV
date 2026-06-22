# 29. Admin Nhật ký hoạt động

```text
Tạo page Admin Nhật ký hoạt động.

Bối cảnh:
- Admin dùng page này để truy vết thao tác quan trọng trong hệ thống.
- Không hiển thị mật khẩu, OTP, token hoặc dữ liệu y tế quá chi tiết.

App shell:
- Dùng Admin sidebar và topbar.
- Breadcrumb: Dashboard / Nhật ký hoạt động.

Bố cục:
- Header: "Nhật ký hoạt động".
- Filter bar:
  - Thời gian.
  - Người dùng.
  - Vai trò.
  - Module.
  - Hành động.
  - Mức độ.
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
- Tóm tắt hành động.
- Trước/sau thay đổi ở mức mô tả an toàn.
- Thời gian.
- Người thao tác.
- Kết quả.
- Metadata an toàn.

Mức độ:
- Thông tin.
- Cảnh báo.
- Nhạy cảm.
- Lỗi.

Log mẫu cần có:
- Đổi quyền.
- Khóa tài khoản.
- Sửa hồ sơ bệnh nhân.
- Hủy lịch.
- Hủy hóa đơn.
- Nhập kho.
- Override cảnh báo thuốc.

Trạng thái:
- Empty khi không có log.
- Loading khi lọc.
- Error khi tải dữ liệu.
```

