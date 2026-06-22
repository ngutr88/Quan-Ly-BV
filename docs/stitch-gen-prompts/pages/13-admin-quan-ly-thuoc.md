# 13. Admin Quản lý thuốc

```text
Tạo page Admin Quản lý thuốc.

Bối cảnh:
- Admin hoặc Dược sĩ quản lý danh mục thuốc và tồn kho tổng quan.
- Page cần thể hiện rõ cảnh báo sắp hết hàng, hết hàng và sắp hết hạn.

App shell:
- Dùng Admin sidebar và topbar.
- Breadcrumb: Dashboard / Thuốc & Kho.

Bố cục:
- Header: "Quản lý thuốc".
- Actions:
  - "Thêm thuốc".
  - "Nhập kho".
  - "Xuất báo cáo tồn kho".
- Filter bar:
  - Tên thuốc/SKU/hoạt chất.
  - Nhóm thuốc.
  - Nhà sản xuất.
  - Trạng thái tồn kho.
  - Hạn sử dụng.

Nội dung:
- KPI cards:
  - Tổng loại thuốc.
  - Sắp hết hàng.
  - Hết hàng.
  - Sắp hết hạn.
- Data table cột:
  - SKU.
  - Tên thuốc.
  - Hoạt chất.
  - Đơn vị tính.
  - Dạng bào chế.
  - Nhà sản xuất.
  - Tồn hiện tại.
  - Ngưỡng tối thiểu.
  - Giá bán.
  - Trạng thái.
  - Hành động.
- Row actions:
  - Xem chi tiết.
  - Sửa.
  - Nhập kho.
  - Xem lô.
  - Vô hiệu hóa.
- Status:
  - Còn hàng.
  - Sắp hết.
  - Hết hàng.
  - Sắp hết hạn.

Chi tiết thuốc drawer:
- Thông tin danh mục thuốc.
- Tồn kho tổng.
- Danh sách lô theo hạn sử dụng.
- Lịch sử nhập/xuất gần đây.

Trạng thái:
- Cảnh báo thuốc dưới ngưỡng tối thiểu.
- Cảnh báo lô thuốc hết hạn trong 30/60/90 ngày.
- Empty khi chưa có thuốc.
```

