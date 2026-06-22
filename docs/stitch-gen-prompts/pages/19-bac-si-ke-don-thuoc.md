# 19. Bác sĩ Kê đơn thuốc

```text
Tạo page hoặc step Kê đơn thuốc cho Bác sĩ.

Bối cảnh:
- Bác sĩ kê đơn trong phiên khám hiện tại.
- Page phải hỗ trợ cảnh báo dị ứng, tồn kho và tương tác thuốc.

Bố cục:
- Header: "Kê đơn thuốc".
- Header phụ gắn với bệnh nhân và phiếu khám hiện tại.
- Layout 2 cột:
  - Tìm và chọn thuốc bên trái.
  - Đơn thuốc hiện tại bên phải.

Tìm thuốc:
- Search theo tên thuốc, hoạt chất, SKU.
- Filter nhóm thuốc.
- Card/list thuốc hiển thị:
  - Tên thuốc.
  - Hoạt chất.
  - Dạng bào chế.
  - Tồn kho.
  - Giá.
  - Cảnh báo.
- Status tồn kho:
  - Còn hàng.
  - Sắp hết.
  - Hết hàng.

Đơn thuốc hiện tại:
- Table các thuốc đã chọn:
  - Tên thuốc.
  - Liều dùng.
  - Cách dùng.
  - Số ngày.
  - Số lượng.
  - Ghi chú.
  - Hành động xóa.
- Tự tính tổng số lượng dựa theo liều và số ngày ở mock.
- Cảnh báo tương tác thuốc ở đầu danh sách nếu có.
- Cảnh báo dị ứng nếu thuốc trùng dị ứng bệnh nhân.
- Button:
  - "Lưu đơn thuốc".
  - "Quay lại phiên khám".

Override cảnh báo:
- Nếu dị ứng/tương tác nghiêm trọng, hiển thị modal bắt buộc nhập lý do override hoặc chọn thuốc khác.

Trạng thái:
- Không cho lưu đơn rỗng nếu bác sĩ đã chọn bước kê đơn.
- Thuốc hết hàng hiển thị đề xuất thuốc thay thế cùng hoạt chất.
- Loading khi lưu đơn.
```

