# 17. Bác sĩ Lịch khám hôm nay

```text
Tạo page Lịch khám hôm nay cho Bác sĩ trong HMS.

Bối cảnh:
- Bác sĩ dùng page này để xem bệnh nhân được phân công trong ngày.
- Bác sĩ chỉ được xem lịch và bệnh nhân của mình.

App shell:
- Sidebar cho Bác sĩ gồm: Lịch khám hôm nay, Hồ sơ bệnh nhân, Khám bệnh, Đơn thuốc, Tin nhắn, Thống kê cá nhân.
- Topbar hiển thị lịch làm việc hôm nay, notification và avatar bác sĩ.

Bố cục:
- Header: "Lịch khám hôm nay".
- Hiển thị tên bác sĩ, khoa và ngày hiện tại.
- Bộ lọc:
  - Trạng thái.
  - Ca khám.
  - Tìm bệnh nhân.
- Layout 2 cột:
  - Danh sách lịch bên trái.
  - Chi tiết lịch được chọn bên phải.

Danh sách lịch:
- Card từng lịch:
  - Giờ.
  - Số thứ tự.
  - Tên bệnh nhân.
  - Tuổi/giới tính.
  - Lý do khám.
  - Trạng thái.
- Nhóm theo ca sáng/chiều/tối.
- Status:
  - Đã xác nhận.
  - Đang chờ check-in.
  - Đang khám.
  - Hoàn thành.
  - Vắng mặt.

Chi tiết lịch:
- Thông tin bệnh nhân tóm tắt.
- Alert dị ứng/bệnh nền quan trọng.
- Lịch sử khám gần nhất.
- Button "Bắt đầu khám" nếu đã check-in.
- Button "Xem hồ sơ".

Trạng thái:
- Empty nếu hôm nay không có lịch.
- Alert nếu bệnh nhân chưa check-in.
- Loading khi chuyển lịch.
```

