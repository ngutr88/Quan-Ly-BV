# 14. Admin Nhập kho và lô thuốc

```text
Tạo page hoặc modal Nhập kho thuốc và quản lý lô thuốc.

Bối cảnh:
- Dùng để ghi nhận thuốc nhập kho theo từng lô.
- Cần giúp Admin/Dược sĩ thấy tồn kho trước và sau nhập.

Bố cục:
- Header: "Nhập kho thuốc".
- Layout desktop 2 cột:
  - Form nhập kho bên trái.
  - Preview lô nhập bên phải.
- Bên dưới là danh sách lô thuốc.

Form nhập kho:
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
- Cảnh báo nếu hạn sử dụng quá gần.
- Cảnh báo nếu số lượng hoặc giá nhập không hợp lệ.

Danh sách lô:
- Table:
  - Số lô.
  - Thuốc.
  - Ngày nhập.
  - Hạn sử dụng.
  - Số lượng còn.
  - Trạng thái.
  - Nhà cung cấp.
  - Hành động.
- Filter theo thuốc, hạn sử dụng, trạng thái.

Trạng thái:
- Validation số lượng phải lớn hơn 0.
- Hạn sử dụng phải sau ngày nhập.
- Toast nhập kho thành công.
- Empty khi chưa có lô thuốc.
```

