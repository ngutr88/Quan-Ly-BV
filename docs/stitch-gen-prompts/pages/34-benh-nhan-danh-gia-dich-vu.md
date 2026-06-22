# 34. Bệnh nhân Đánh giá dịch vụ

```text
Tạo page hoặc modal Đánh giá bác sĩ và dịch vụ sau khám.

Bối cảnh:
- Bệnh nhân đánh giá sau khi phiên khám hoàn thành.
- Giao diện nên ngắn, dễ thao tác, khuyến khích phản hồi chất lượng.

Bố cục:
- Modal sau khi phiên khám hoàn thành hoặc page từ lịch sử khám.
- Header: "Đánh giá buổi khám".
- Card thông tin phiên khám:
  - Bác sĩ.
  - Khoa.
  - Ngày khám.
  - Dịch vụ.

Nội dung:
- Rating sao cho bác sĩ.
- Rating sao cho dịch vụ bệnh viện.
- Tags nhanh:
  - Đúng giờ.
  - Tận tâm.
  - Dễ hiểu.
  - Cơ sở sạch.
  - Chờ lâu.
  - Cần cải thiện.
- Textarea nhận xét.
- Checkbox cho phép bệnh viện liên hệ lại.
- Button "Gửi đánh giá".

Trạng thái:
- Không cho gửi nếu chưa chọn rating.
- Loading khi gửi.
- Cảm ơn sau khi gửi.
- Nếu đã đánh giá, hiển thị review đã gửi và disable sửa nếu mock policy như vậy.
```

