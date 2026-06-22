# 24. Bệnh nhân Chi tiết đơn thuốc

```text
Tạo page Chi tiết đơn thuốc cho Bệnh nhân.

Bối cảnh:
- Bệnh nhân xem lại đơn thuốc đã được bác sĩ kê.
- Nội dung phải dễ đọc, tránh thuật ngữ quá khó.

Bố cục:
- Header: "Đơn thuốc".
- Có danh sách đơn thuốc ở page list hoặc dropdown nếu nhiều đơn.
- Chi tiết đơn thuốc dạng card dễ đọc.

Nội dung chi tiết:
- Thông tin đơn:
  - Mã đơn.
  - Ngày kê.
  - Bác sĩ.
  - Khoa.
  - Liên kết phiên khám.
- Lời dặn bác sĩ.
- Danh sách thuốc:
  - Tên thuốc.
  - Hàm lượng/dạng bào chế nếu có.
  - Liều dùng.
  - Cách dùng.
  - Số ngày dùng.
  - Số lượng.
  - Ghi chú.
- Cảnh báo:
  - "Đọc kỹ hướng dẫn trước khi dùng".
  - "Không tự ý ngưng thuốc".
  - "Liên hệ bác sĩ khi có phản ứng bất thường".

Actions:
- Tải PDF.
- In đơn.
- Đặt lịch tái khám.

Trạng thái:
- Empty nếu chưa có đơn thuốc.
- Thuốc đã hết liệu trình nếu có mock.
- Loading khi tải đơn.
```

