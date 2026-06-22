# 06. Admin Quản lý lịch khám

```text
Tạo page Admin Quản lý lịch khám cho HMS.

Bối cảnh:
- Admin hoặc lễ tân dùng page này để điều phối lịch hẹn khám bệnh.
- Cần hỗ trợ xác nhận, từ chối, dời lịch, hủy lịch và theo dõi hàng đợi.

App shell:
- Dùng Admin sidebar và topbar.
- Breadcrumb: Dashboard / Lịch khám.

Bố cục:
- Header: "Quản lý lịch khám".
- Actions trên header:
  - "Tạo lịch thủ công".
  - "Xuất danh sách".
- Tabs: Danh sách, Calendar, Hàng đợi hôm nay.
- Filter bar:
  - Ngày/khoảng ngày.
  - Khoa.
  - Bác sĩ.
  - Trạng thái.
  - Ô tìm bệnh nhân/SĐT/mã lịch.

Tab Danh sách:
- Data table cột:
  - Mã lịch.
  - Thời gian.
  - Bệnh nhân.
  - SĐT.
  - Khoa.
  - Bác sĩ.
  - Phòng.
  - Trạng thái.
  - Nguồn đặt.
  - Hành động.
- Row actions:
  - Xem chi tiết.
  - Xác nhận.
  - Từ chối.
  - Dời lịch.
  - Hủy lịch.
- Status badge:
  - Chờ xác nhận.
  - Đã xác nhận.
  - Đang khám.
  - Hoàn thành.
  - Đã hủy.
  - Vắng mặt.

Tab Calendar:
- Calendar ngày/tuần/tháng.
- Mỗi lịch là event có giờ, tên bệnh nhân, bác sĩ, trạng thái.
- Event trùng lịch bác sĩ phải có cảnh báo visual.

Tab Hàng đợi hôm nay:
- Danh sách số thứ tự check-in theo khoa.
- Cột: STT, bệnh nhân, giờ hẹn, giờ check-in, bác sĩ, trạng thái hiện tại.

Modal tạo lịch:
- Chọn bệnh nhân.
- Chọn khoa.
- Chọn dịch vụ.
- Chọn bác sĩ.
- Chọn ngày.
- Chọn khung giờ.
- Chọn phòng.
- Nhập lý do khám.
- Hiển thị cảnh báo nếu trùng lịch hoặc slot đã đầy.

Trạng thái:
- Empty khi không có lịch.
- Confirm dialog khi hủy/dời/từ chối.
- Toast sau khi xác nhận lịch và gửi thông báo.
```

