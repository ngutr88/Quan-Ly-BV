# 09. Admin Chi tiết bác sĩ

```text
Tạo page Chi tiết bác sĩ cho Admin.

Bối cảnh:
- Admin xem hồ sơ chuyên môn, lịch làm việc, hiệu suất và tài khoản của một bác sĩ.

Bố cục:
- Header có avatar, họ tên, học vị, chuyên khoa, trạng thái.
- Actions:
  - Sửa hồ sơ.
  - Thiết lập lịch.
  - Đặt lại mật khẩu.
  - Khóa tài khoản.
- Tabs:
  - Tổng quan.
  - Lịch làm việc.
  - Hiệu suất.
  - Hồ sơ chuyên môn.
  - Nhật ký.

Tab Tổng quan:
- Thông tin liên hệ.
- Khoa phụ trách.
- Mô tả chuyên môn.
- KPI:
  - Lượt khám tháng này.
  - Rating trung bình.
  - Số lịch sắp tới.
  - Tỷ lệ hoàn thành.

Tab Lịch làm việc:
- Calendar tuần.
- Ca sáng/chiều/tối.
- Ngày nghỉ phép.
- Ngày trực.
- Số bệnh nhân tối đa mỗi khung giờ.
- Button chỉnh lịch.

Tab Hiệu suất:
- Biểu đồ lượt khám theo tháng.
- Danh sách đánh giá gần đây từ bệnh nhân.
- Tỷ lệ lịch hoàn thành/vắng mặt/hủy.

Tab Hồ sơ chuyên môn:
- Chứng chỉ hành nghề.
- Bằng cấp đính kèm.
- Ngày cấp/nơi cấp.
- Kinh nghiệm và mô tả chuyên môn.

Tab Nhật ký:
- Thay đổi hồ sơ.
- Thay đổi tài khoản.
- Thay đổi lịch làm việc.

Trạng thái:
- Loading khi tải hồ sơ.
- Empty cho tab chưa có dữ liệu.
- Alert nếu tài khoản đang bị khóa.
```

