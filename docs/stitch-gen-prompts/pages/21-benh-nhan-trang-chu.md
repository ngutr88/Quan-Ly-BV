# 21. Bệnh nhân Trang chủ

```text
Tạo page Trang chủ Bệnh nhân cho HMS.

Bối cảnh:
- Đây là dashboard cá nhân cho bệnh nhân.
- Ưu tiên mobile-first, thông tin đơn giản, dễ thao tác.

Navigation:
- Mobile bottom navigation: Trang chủ, Đặt lịch, Lịch khám, Hóa đơn, Hồ sơ.
- Desktop dùng top navigation hoặc sidebar nhẹ.

Bố cục:
- Header chào người dùng: "Xin chào, Nguyễn Văn A".
- Card nổi bật "Lịch khám sắp tới" nếu có.
- Quick actions dạng icon button:
  - Đặt lịch khám.
  - Xem lịch.
  - Xem đơn thuốc.
  - Thanh toán hóa đơn.
  - Hồ sơ sức khỏe.

Nội dung:
- Lịch khám sắp tới:
  - Ngày giờ.
  - Khoa.
  - Bác sĩ.
  - Trạng thái.
  - Button xem chi tiết/hủy/dời nếu được phép.
- Hóa đơn chưa thanh toán:
  - Mã hóa đơn.
  - Tổng tiền.
  - Button Thanh toán.
- Đơn thuốc gần nhất:
  - Ngày kê.
  - Bác sĩ.
  - Số thuốc.
  - Button Xem đơn.
- Thông báo mới:
  - Nhắc lịch.
  - Thanh toán.
  - Lời dặn.
- Card hồ sơ sức khỏe:
  - BHYT.
  - Dị ứng.
  - Chỉ số gần nhất.

Trạng thái:
- Bệnh nhân mới chưa có lịch: empty state với CTA "Đặt lịch khám đầu tiên".
- Loading skeleton.
- Error tải dữ liệu.
```

