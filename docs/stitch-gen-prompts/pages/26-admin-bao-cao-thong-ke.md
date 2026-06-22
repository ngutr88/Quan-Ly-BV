# 26. Admin Báo cáo thống kê

```text
Tạo page Admin Báo cáo thống kê.

Bối cảnh:
- Admin dùng page này để xem báo cáo vận hành, doanh thu, lượt khám, tồn kho và công nợ.
- Page desktop-first, nhiều chart và bảng.

App shell:
- Dùng Admin sidebar và topbar.
- Breadcrumb: Dashboard / Báo cáo.

Bố cục:
- Header: "Báo cáo thống kê".
- Bộ lọc thời gian lớn:
  - Hôm nay.
  - Tuần này.
  - Tháng này.
  - Quý này.
  - Tùy chọn.
- Tabs:
  - Doanh thu.
  - Lượt khám.
  - Kho thuốc.
  - Công nợ.
  - Hiệu suất bác sĩ.

Tab Doanh thu:
- Chart doanh thu theo ngày.
- Bảng doanh thu theo khoa.
- Bảng doanh thu theo bác sĩ.
- Bảng doanh thu theo loại dịch vụ.

Tab Lượt khám:
- Chart lượt khám theo khoa.
- Top giờ cao điểm.
- Tỷ lệ hủy/vắng mặt.
- Số lượt khám hoàn thành.

Tab Kho thuốc:
- Thuốc tiêu thụ nhiều.
- Thuốc sắp hết.
- Thuốc sắp hết hạn.
- Tổng giá trị tồn kho.

Tab Công nợ:
- Hóa đơn chưa thanh toán.
- Hóa đơn quá hạn.
- Tổng công nợ.
- Danh sách bệnh nhân còn nợ.

Tab Hiệu suất bác sĩ:
- Lượt khám.
- Rating.
- Tỷ lệ hoàn thành.
- Danh sách top bác sĩ.

Actions:
- Xuất PDF.
- Xuất Excel.
- Lưu bộ lọc báo cáo.

Trạng thái:
- Loading chart.
- Empty report.
- Ghi chú "Dữ liệu báo cáo chỉ mang tính demo" nếu dùng mock data.
```

