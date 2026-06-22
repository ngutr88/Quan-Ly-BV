# 28. Admin Cấu hình hệ thống

```text
Tạo page Admin Cấu hình hệ thống.

Bối cảnh:
- Admin cấu hình giờ làm việc, lịch khám, kho thuốc, thông báo và thanh toán.
- Page cần tránh hard-code nghiệp vụ trong các page khác.

App shell:
- Dùng Admin sidebar và topbar.
- Breadcrumb: Dashboard / Cấu hình.

Bố cục:
- Header: "Cấu hình hệ thống".
- Tabs:
  - Giờ làm việc.
  - Lịch khám.
  - Kho thuốc.
  - Thông báo.
  - Thanh toán.
  - Chung.
- Footer sticky có button "Lưu thay đổi" và "Khôi phục mặc định".

Tab Giờ làm việc:
- Ngày trong tuần.
- Ca sáng/chiều/tối.
- Ngày nghỉ.
- Giờ mở cửa/đóng cửa.

Tab Lịch khám:
- Thời lượng khám mặc định.
- Số slot tối đa.
- Thời gian nghỉ giữa ca.
- Quy tắc hủy/dời lịch.
- Thời điểm gửi nhắc lịch.

Tab Kho thuốc:
- Ngưỡng tồn kho mặc định.
- Cảnh báo hết hạn 30/60/90 ngày.
- Cấu hình nhóm thuốc cần kiểm soát đặc biệt.

Tab Thông báo:
- Template xác nhận lịch.
- Template nhắc lịch.
- Template hủy lịch.
- Template thanh toán.
- Template OTP.
- Button xem trước template.

Tab Thanh toán:
- Bật/tắt VNPay.
- Bật/tắt Momo.
- Bật/tắt ZaloPay.
- Tiền mặt.
- Chuyển khoản.
- Cấu hình trạng thái mock.

Tab Chung:
- Tên bệnh viện.
- Địa chỉ.
- Hotline.
- Logo.

Trạng thái:
- Confirm khi lưu cấu hình ảnh hưởng nghiệp vụ.
- Validation dữ liệu cấu hình.
- Toast lưu thành công.
```

