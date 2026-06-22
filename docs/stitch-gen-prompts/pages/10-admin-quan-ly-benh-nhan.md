# 10. Admin Quản lý bệnh nhân

```text
Tạo page Admin Quản lý bệnh nhân.

Bối cảnh:
- Admin/lễ tân quản lý hồ sơ và tài khoản bệnh nhân.
- Không hiển thị chi tiết y tế nhạy cảm quá mức trên bảng danh sách.

App shell:
- Dùng Admin sidebar và topbar.
- Breadcrumb: Dashboard / Bệnh nhân.

Bố cục:
- Header: "Quản lý bệnh nhân".
- Actions:
  - "Thêm bệnh nhân tại quầy".
  - "Gộp hồ sơ".
  - "Xuất danh sách".
- Filter bar:
  - Tên.
  - SĐT.
  - Mã bệnh nhân.
  - CCCD.
  - BHYT.
  - Trạng thái tài khoản.
  - Nhóm rủi ro.

Nội dung:
- Data table cột:
  - Mã bệnh nhân.
  - Họ tên.
  - Ngày sinh.
  - Giới tính.
  - SĐT.
  - BHYT.
  - Lần khám gần nhất.
  - Số lịch sắp tới.
  - Trạng thái tài khoản.
  - Hành động.
- Row actions:
  - Xem hồ sơ.
  - Sửa thông tin.
  - Khóa/Mở tài khoản.
  - Gộp hồ sơ.
- Cảnh báo hồ sơ trùng lặp: tên + SĐT hoặc CCCD giống nhau.

Form thêm/sửa bệnh nhân:
- Họ tên.
- Ngày sinh.
- Giới tính.
- Địa chỉ.
- SĐT.
- Email.
- CCCD.
- Số BHYT.
- Nhóm máu.
- Người liên hệ khẩn cấp.
- Tiền sử bệnh.
- Dị ứng thuốc/thực phẩm.
- Ghi chú nội bộ.

Trạng thái:
- Empty khi chưa có bệnh nhân.
- Confirm khi khóa tài khoản.
- Confirm khi gộp hồ sơ.
- Validation SĐT/email/CCCD trùng.
```

