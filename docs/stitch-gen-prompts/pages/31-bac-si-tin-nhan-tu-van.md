# 31. Bác sĩ Tin nhắn tư vấn

```text
Tạo page Tin nhắn tư vấn cho Bác sĩ.

Bối cảnh:
- Bác sĩ trao đổi/tư vấn trực tuyến với bệnh nhân.
- Đây không thay thế cấp cứu, cần có cảnh báo rõ.

App shell:
- Dùng shell Bác sĩ.
- Breadcrumb: Bác sĩ / Tin nhắn.

Bố cục:
- Layout inbox 3 cột trên desktop:
  1. Danh sách hội thoại.
  2. Khung chat.
  3. Panel bệnh nhân.
- Trên mobile/tablet có thể chuyển thành từng màn hình.

Danh sách hội thoại:
- Tên bệnh nhân.
- Avatar.
- Tin nhắn cuối.
- Thời gian.
- Badge chưa đọc.
- Filter:
  - Chưa đọc.
  - Đang theo dõi.
  - Đã đóng.

Khung chat:
- Tin nhắn text.
- Gửi file/ảnh ở trạng thái mock.
- Ô nhập tin nhắn.
- Quick replies:
  - "Vui lòng đặt lịch tái khám".
  - "Uống thuốc theo đơn".
  - "Liên hệ cấp cứu nếu có dấu hiệu nặng".
- Cảnh báo: "Tin nhắn tư vấn không thay thế cấp cứu".

Panel bệnh nhân:
- Thông tin tóm tắt.
- Lịch khám gần nhất.
- Đơn thuốc gần nhất.
- Cảnh báo dị ứng.

Trạng thái:
- Empty khi chưa chọn hội thoại.
- Loading tin nhắn.
- Tin nhắn gửi lỗi.
```

