# 25. Bệnh nhân Hóa đơn và thanh toán

```text
Tạo page Hóa đơn và thanh toán cho Bệnh nhân.

Bối cảnh:
- Bệnh nhân xem hóa đơn và thanh toán online.
- Mobile-first, số tiền và trạng thái phải rõ ràng.

Bố cục:
- Header: "Hóa đơn".
- Tabs:
  - Chưa thanh toán.
  - Đã thanh toán.
  - Tất cả.
- Danh sách hóa đơn dạng card trên mobile, table trên desktop.

Card hóa đơn:
- Mã hóa đơn.
- Ngày tạo.
- Dịch vụ/khoa.
- Tổng tiền VND.
- Trạng thái.
- Button "Xem chi tiết".
- Button "Thanh toán" nếu chưa thanh toán.

Chi tiết hóa đơn:
- Thông tin phiên khám.
- Bảng phí:
  - Phí khám.
  - Xét nghiệm.
  - Thuốc.
  - Dịch vụ khác.
  - Giảm trừ.
  - Tổng tiền.
- Phương thức thanh toán.
- Biên lai nếu đã thanh toán.

Thanh toán online mock:
- Chọn phương thức:
  - VNPay.
  - Momo.
  - ZaloPay.
  - Chuyển khoản.
- Màn hình xác nhận thanh toán.
- State thành công.
- State thất bại.
- State timeout.
- Nếu thất bại, giữ trạng thái Chưa thanh toán và có button "Thử lại".

Trạng thái:
- Empty khi chưa có hóa đơn.
- Warning hóa đơn quá hạn.
- Loading khi chuyển sang thanh toán.
```

