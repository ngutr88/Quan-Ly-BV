# 30. Bác sĩ Thống kê cá nhân

```text
Tạo page Thống kê cá nhân cho Bác sĩ.

Bối cảnh:
- Bác sĩ xem hiệu suất khám của bản thân.
- Không hiển thị doanh thu nếu vai trò bác sĩ không được phép xem.

App shell:
- Dùng shell Bác sĩ.
- Breadcrumb: Bác sĩ / Thống kê cá nhân.

Bố cục:
- Header: "Thống kê cá nhân".
- Bộ lọc thời gian:
  - 7 ngày.
  - 30 ngày.
  - Tháng này.
  - Tùy chọn.

Nội dung:
- KPI cards:
  - Số lượt khám.
  - Số bệnh nhân mới.
  - Tỷ lệ hoàn thành lịch.
  - Rating trung bình.
- Chart lượt khám theo ngày.
- Chart phân bố bệnh nhân theo khoa/dịch vụ hoặc nhóm chẩn đoán phổ biến.
- Danh sách đánh giá mới nhất từ bệnh nhân.
- Bảng lịch sử hoạt động khám gần đây:
  - Ngày.
  - Bệnh nhân.
  - Khoa.
  - Trạng thái.
  - Chẩn đoán tóm tắt.

Trạng thái:
- Empty khi chưa có dữ liệu.
- Loading chart.
- Không hiển thị số liệu tài chính nếu chưa được cấp quyền.
```

