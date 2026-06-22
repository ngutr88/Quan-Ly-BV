# 33. Bệnh nhân Thông báo

```text
Tạo page Trung tâm thông báo cho Bệnh nhân.

Bối cảnh:
- Bệnh nhân xem nhắc lịch, thông báo thanh toán, đơn thuốc và thông báo hệ thống.
- Mobile-first, mỗi thông báo là một card dễ đọc.

Navigation:
- Có thể truy cập từ notification bell hoặc bottom navigation phụ.

Bố cục:
- Header: "Thông báo".
- Tabs/filter:
  - Tất cả.
  - Lịch khám.
  - Thanh toán.
  - Đơn thuốc.
  - Hệ thống.
- Danh sách notification dạng card.

Nội dung card:
- Icon loại thông báo.
- Tiêu đề.
- Nội dung ngắn.
- Thời gian.
- Trạng thái đã đọc/chưa đọc.
- Action nếu có:
  - Xem lịch.
  - Thanh toán.
  - Xem đơn thuốc.

Thông báo mẫu:
- Lịch khám đã được xác nhận.
- Nhắc lịch trước 24 giờ.
- Hóa đơn đã thanh toán.
- Thanh toán thất bại.
- Đơn thuốc mới đã được cập nhật.

Actions:
- Đánh dấu tất cả đã đọc.
- Cài đặt nhận thông báo.

Trạng thái:
- Empty "Bạn chưa có thông báo nào".
- Loading danh sách.
```

