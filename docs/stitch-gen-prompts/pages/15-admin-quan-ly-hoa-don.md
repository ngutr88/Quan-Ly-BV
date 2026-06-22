# 15. Admin Quản lý hóa đơn

```text
Tạo page Admin Quản lý hóa đơn.

Bối cảnh:
- Admin hoặc kế toán quản lý hóa đơn, trạng thái thanh toán, công nợ và xuất hóa đơn.
- Page desktop-first với bảng và filter mạnh.

App shell:
- Dùng Admin sidebar và topbar.
- Breadcrumb: Dashboard / Hóa đơn.

Bố cục:
- Header: "Quản lý hóa đơn".
- Actions:
  - "Tạo hóa đơn thủ công".
  - "Xuất báo cáo".
- Filter bar:
  - Mã hóa đơn.
  - Bệnh nhân.
  - Ngày/khoảng ngày.
  - Khoa.
  - Trạng thái thanh toán.
  - Phương thức thanh toán.

Nội dung:
- KPI cards:
  - Doanh thu hôm nay.
  - Hóa đơn chưa thanh toán.
  - Hóa đơn quá hạn.
  - Doanh thu tháng.
- Data table cột:
  - Mã hóa đơn.
  - Ngày tạo.
  - Bệnh nhân.
  - Phiếu khám.
  - Khoa.
  - Tổng tiền.
  - Phương thức.
  - Trạng thái.
  - Người thu.
  - Hành động.
- Row actions:
  - Xem chi tiết.
  - Xác nhận thanh toán.
  - Gửi nhắc thanh toán.
  - Hủy hóa đơn.
  - In hóa đơn.
- Status:
  - Chưa thanh toán.
  - Đã thanh toán.
  - Đã hủy.
  - Quá hạn.

Trạng thái:
- Số tiền định dạng VND.
- Hóa đơn quá hạn có alert nhẹ.
- Confirm dialog khi hủy hóa đơn, bắt buộc nhập lý do.
- Empty khi chưa có hóa đơn.
```

