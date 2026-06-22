# 23. Bệnh nhân Lịch khám của tôi

```text
Tạo page Lịch khám của tôi cho Bệnh nhân.

Bối cảnh:
- Bệnh nhân xem, theo dõi, dời hoặc hủy lịch khám của mình.
- Mobile-first, dùng card rõ ràng.

Bố cục:
- Header: "Lịch khám của tôi".
- Tabs:
  - Sắp tới.
  - Chờ xác nhận.
  - Đã hoàn thành.
  - Đã hủy.
- Search/filter theo khoa, bác sĩ, ngày.

Nội dung:
- Card lịch khám:
  - Ngày giờ.
  - Khoa/dịch vụ.
  - Bác sĩ.
  - Phòng/vị trí nếu có.
  - Trạng thái.
  - Lý do khám.
  - Action:
    - Xem chi tiết.
    - Dời lịch.
    - Hủy lịch.
    - Xem hóa đơn sau khi hoàn thành.

Chi tiết lịch:
- Timeline:
  - Đặt lịch.
  - Xác nhận.
  - Nhắc lịch.
  - Check-in.
  - Khám.
  - Hoàn thành.
- Thông tin chuẩn bị trước khi đi khám.
- QR check-in mock nếu lịch đã xác nhận.

Modal hủy/dời:
- Bắt buộc nhập lý do nếu lịch đã xác nhận.
- Hiển thị lưu ý chính sách hủy/dời.
- Confirm trước khi thực hiện.

Trạng thái:
- Empty "Bạn chưa có lịch khám nào".
- CTA "Đặt lịch khám".
- Loading danh sách lịch.
```

