# 12. Admin Quản lý khoa và dịch vụ

```text
Tạo page Admin Quản lý khoa và dịch vụ.

Bối cảnh:
- Admin quản lý danh mục khoa/phòng chuyên môn và dịch vụ khám tương ứng.
- Page cần giúp cấu hình dịch vụ, giá và công suất theo khoa.

App shell:
- Dùng Admin sidebar và topbar.
- Breadcrumb: Dashboard / Khoa & Dịch vụ.

Bố cục:
- Header: "Khoa và dịch vụ".
- Actions:
  - "Thêm khoa".
  - "Thêm dịch vụ".
- Layout 2 vùng:
  - Danh sách khoa bên trái hoặc tab trên cùng.
  - Chi tiết khoa và danh sách dịch vụ bên phải.

Nội dung Khoa:
- Danh sách khoa:
  - Mã khoa.
  - Tên khoa.
  - Vị trí.
  - Trưởng khoa.
  - Số bác sĩ.
  - Lượt khám tháng này.
  - Trạng thái.
- Detail khoa:
  - Mô tả.
  - Vị trí tòa/tầng/phòng.
  - Trưởng khoa.
  - Số bệnh nhân tối đa/ngày.
  - Danh sách bác sĩ thuộc khoa.

Nội dung Dịch vụ:
- Table dịch vụ thuộc khoa:
  - Tên dịch vụ.
  - Giá VND.
  - Thời gian thực hiện.
  - Mô tả.
  - Trạng thái.
- Form thêm/sửa dịch vụ:
  - Tên.
  - Khoa.
  - Giá VND.
  - Thời lượng.
  - Mô tả.
  - Trạng thái.

Trạng thái:
- Vô hiệu hóa khoa/dịch vụ thay vì xóa cứng.
- Cảnh báo nếu khoa còn bác sĩ hoặc lịch khám đang hoạt động.
- Empty khi khoa chưa có dịch vụ.
```

