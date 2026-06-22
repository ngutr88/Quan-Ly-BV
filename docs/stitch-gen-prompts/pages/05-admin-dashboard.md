# 05. Admin Dashboard

```text
Tạo page Admin Dashboard cho HMS.

Bối cảnh:
- Đây là trang tổng quan vận hành bệnh viện cho Admin.
- Admin dùng desktop là chính.
- Page cần dày thông tin nhưng dễ scan nhanh.

App shell:
- Sidebar trái cố định gồm: Dashboard, Lịch khám, Bác sĩ, Bệnh nhân, Khoa & Dịch vụ, Thuốc & Kho, Hóa đơn, Nhân sự & Phân quyền, Báo cáo, Cấu hình, Nhật ký.
- Topbar có search toàn hệ thống, notification bell, avatar Admin và breadcrumb.

Bố cục page:
- Header: "Dashboard tổng quan".
- Hiển thị ngày hiện tại.
- Bộ lọc thời gian: Hôm nay, 7 ngày, 30 ngày, Tùy chọn.
- Button "Xuất báo cáo" có menu PDF và Excel.

Nội dung chính:
- Hàng KPI cards:
  1. Tổng bác sĩ đang hoạt động / tổng khoa.
  2. Bệnh nhân đã đăng ký và bệnh nhân mới hôm nay.
  3. Lịch khám hôm nay, chia nhỏ: chờ xác nhận, đã xác nhận, hoàn thành, đã hủy.
  4. Doanh thu hôm nay và tháng này, có phần trăm tăng/giảm.
- Biểu đồ doanh thu theo ngày trong tháng.
- Biểu đồ lượt khám theo khoa.
- Bảng "Lịch khám sắp diễn ra trong 2 giờ tới": giờ, bệnh nhân, bác sĩ, khoa, trạng thái.
- Card "Cảnh báo vận hành":
  - Thuốc sắp hết hàng.
  - Thuốc sắp hết hạn.
  - Hóa đơn quá hạn.
  - Lịch khám xung đột.
- Bảng "Top bác sĩ" có avatar, khoa, lượt khám, rating.
- Widget "Nhật ký hoạt động gần đây": ai vừa thao tác gì, thời gian, module.

Trạng thái:
- Loading skeleton cho KPI và chart.
- Empty chart khi chưa có dữ liệu.
- Alert nổi bật khi có cảnh báo nghiêm trọng.

Phong cách:
- Sử dụng xanh y tế và màu cảnh báo vừa phải.
- Không dùng hero/landing.
- Ưu tiên dashboard vận hành thật.
```

