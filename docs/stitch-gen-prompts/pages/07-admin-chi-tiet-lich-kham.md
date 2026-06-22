# 07. Admin Chi tiết lịch khám

```text
Tạo page hoặc drawer Chi tiết lịch khám cho Admin.

Bối cảnh:
- Admin cần xem đầy đủ thông tin một lịch hẹn và thực hiện các hành động điều phối.
- Có thể thiết kế dưới dạng drawer bên phải từ trang Quản lý lịch khám hoặc một page detail riêng.

Bố cục:
- Header có mã lịch, trạng thái hiện tại, nút đóng/quay lại.
- Actions theo trạng thái:
  - Xác nhận.
  - Dời lịch.
  - Hủy lịch.
  - Đánh dấu vắng mặt.
  - In phiếu hẹn.

Nội dung:
- Card thông tin bệnh nhân:
  - Họ tên.
  - Mã bệnh nhân.
  - Ngày sinh.
  - Giới tính.
  - SĐT.
  - BHYT nếu có.
- Card lịch hẹn:
  - Khoa.
  - Dịch vụ.
  - Bác sĩ.
  - Phòng.
  - Ngày giờ.
  - Nguồn đặt.
  - Lý do khám.
- Timeline trạng thái:
  - Tạo lịch.
  - Xác nhận.
  - Gửi nhắc lịch.
  - Check-in.
  - Bắt đầu khám.
  - Hoàn thành.
- Lịch sử thay đổi:
  - Ai thay đổi.
  - Thời gian.
  - Nội dung thay đổi.
- Thông báo đã gửi:
  - Email/SMS/in-app.
  - Trạng thái gửi.
- Khu vực ghi chú nội bộ cho lễ tân/admin.

Modal dời lịch:
- Ngày mới.
- Khung giờ mới.
- Bác sĩ mới nếu cần.
- Lý do dời bắt buộc.
- Cảnh báo xung đột lịch.

Modal hủy lịch:
- Lý do hủy bắt buộc.
- Checkbox gửi thông báo cho bệnh nhân.
- Confirm dialog trước khi hủy.
```

