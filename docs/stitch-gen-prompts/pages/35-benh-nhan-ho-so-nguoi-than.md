# 35. Bệnh nhân Hồ sơ người thân

```text
Tạo page Hồ sơ người thân cho Bệnh nhân.

Bối cảnh:
- Bệnh nhân quản lý hồ sơ phụ thuộc để đặt lịch hộ trẻ em, người già hoặc người thân.
- Mobile-first, dùng card.

Navigation:
- Nằm trong khu vực Hồ sơ hoặc Trang chủ bệnh nhân.

Bố cục:
- Header: "Người thân".
- Mô tả: "Quản lý hồ sơ phụ thuộc để đặt lịch hộ trẻ em hoặc người thân".
- Button "Thêm người thân".

Nội dung:
- Danh sách card người thân:
  - Họ tên.
  - Quan hệ.
  - Ngày sinh.
  - Giới tính.
  - BHYT.
  - Trạng thái hồ sơ.
- Actions:
  - Xem hồ sơ.
  - Đặt lịch hộ.
  - Sửa.
  - Xóa liên kết.

Form thêm người thân:
- Họ tên.
- Ngày sinh.
- Giới tính.
- Quan hệ.
- SĐT nếu có.
- BHYT/CCCD tùy chọn.
- Người giám hộ.
- Checkbox xác nhận có quyền quản lý hồ sơ người thân.

Trạng thái:
- Empty nếu chưa có người thân.
- Confirm khi xóa liên kết hồ sơ.
- Validation thông tin bắt buộc.
```

