# 22. Bệnh nhân Đặt lịch khám

```text
Tạo page Đặt lịch khám cho Bệnh nhân.

Bối cảnh:
- Đây là luồng quan trọng nhất của bệnh nhân.
- Page cần ít bước, dễ hiểu, mobile-first.

Navigation:
- Mobile bottom navigation giữ mục Đặt lịch active.

Bố cục:
- Wizard 4 bước rõ ràng:
  1. Chọn khoa/dịch vụ.
  2. Chọn bác sĩ.
  3. Chọn ngày và khung giờ.
  4. Xác nhận thông tin.
- Trên mobile, mỗi bước là một màn hình/card.
- Trên desktop, có thể dùng layout rộng hơn với summary bên phải.

Bước 1 - Chọn khoa/dịch vụ:
- Danh sách khoa với icon, mô tả ngắn, vị trí.
- Danh sách dịch vụ thuộc khoa:
  - Tên.
  - Giá.
  - Thời gian dự kiến.
- Search khoa/dịch vụ.

Bước 2 - Chọn bác sĩ:
- Card bác sĩ:
  - Avatar.
  - Họ tên.
  - Học vị.
  - Chuyên khoa.
  - Số năm kinh nghiệm.
  - Rating.
  - Lịch trống gần nhất.
- Option "Bất kỳ bác sĩ phù hợp".

Bước 3 - Chọn ngày và giờ:
- Calendar chọn ngày.
- Time slot dạng chip theo ca sáng/chiều/tối.
- Slot đầy disabled và ghi "Đã đầy".

Bước 4 - Xác nhận:
- Tóm tắt:
  - Bệnh nhân.
  - Khoa.
  - Dịch vụ.
  - Bác sĩ.
  - Ngày giờ.
  - Phí dự kiến.
  - Lý do khám.
- Textarea lý do khám.
- Checkbox xác nhận thông tin đúng.
- Button "Đặt lịch".

Trạng thái:
- Không có slot trống: gợi ý ngày gần nhất.
- Đặt lịch thành công: màn hình success có mã lịch và nút thêm vào lịch.
- Loading khi gửi đặt lịch.
```

