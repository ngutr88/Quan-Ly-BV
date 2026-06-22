# 16. Admin Chi tiết hóa đơn

```text
Tạo page Chi tiết hóa đơn cho Admin.

Bối cảnh:
- Admin/kế toán xem đầy đủ phí, trạng thái thanh toán, audit trail và thao tác trên hóa đơn.

Bố cục:
- Header có mã hóa đơn, trạng thái, tổng tiền.
- Actions:
  - Xác nhận thanh toán.
  - In hóa đơn.
  - Xuất PDF.
  - Hủy hóa đơn.
- Layout 2 cột:
  - Nội dung hóa đơn chính.
  - Panel thanh toán và audit.

Nội dung hóa đơn:
- Thông tin bệnh nhân:
  - Họ tên.
  - Mã bệnh nhân.
  - SĐT.
  - BHYT nếu có.
- Thông tin phiên khám:
  - Ngày khám.
  - Khoa.
  - Bác sĩ.
  - Mã phiếu khám.
- Bảng chi tiết phí:
  - Phí khám.
  - Phí xét nghiệm/cận lâm sàng nếu có.
  - Phí thuốc.
  - Dịch vụ khác.
  - BHYT/giảm giá nếu có.
  - Tổng tiền.
- Ghi chú hóa đơn.

Panel thanh toán:
- Trạng thái thanh toán.
- Phương thức:
  - Tiền mặt.
  - Chuyển khoản.
  - Thẻ.
  - VNPay.
  - Momo.
  - ZaloPay.
- Thời gian thanh toán.
- Người xác nhận.
- Mã giao dịch online nếu có.

Audit trail:
- Tạo hóa đơn.
- Chỉnh sửa.
- Gửi nhắc.
- Xác nhận thanh toán.
- Hủy hóa đơn.

Modal xác nhận thanh toán:
- Chọn phương thức.
- Nhập số tiền nhận.
- Ghi chú.
- Button xác nhận.

Modal hủy hóa đơn:
- Lý do hủy bắt buộc.
- Confirm trước khi hủy.
```

