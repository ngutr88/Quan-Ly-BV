# 32. Bệnh nhân Hồ sơ sức khỏe cá nhân

```text
Tạo page Hồ sơ sức khỏe cá nhân cho Bệnh nhân.

Bối cảnh:
- Bệnh nhân xem và cập nhật một số thông tin cá nhân/sức khỏe được phép.
- Mobile-first, dễ đọc, không quá kỹ thuật.

Navigation:
- Mobile bottom navigation giữ mục Hồ sơ active.

Bố cục:
- Header: "Hồ sơ sức khỏe".
- Progress card nếu hồ sơ chưa hoàn thiện.
- Tabs:
  - Thông tin cá nhân.
  - Tiền sử & dị ứng.
  - Chỉ số sức khỏe.
  - Người liên hệ.

Tab Thông tin cá nhân:
- Họ tên.
- Ngày sinh.
- Giới tính.
- SĐT.
- Email.
- Địa chỉ.
- CCCD.
- BHYT.
- Button chỉnh sửa thông tin cho các trường được phép.

Tab Tiền sử & dị ứng:
- Bệnh nền.
- Dị ứng thuốc/thực phẩm.
- Ghi chú sức khỏe cá nhân.
- Alert: "Thông tin này giúp bác sĩ kê đơn an toàn hơn".

Tab Chỉ số sức khỏe:
- Cards:
  - Cân nặng.
  - Huyết áp.
  - Nhịp tim.
  - Đường huyết nếu có.
- Chart theo thời gian.
- Button "Thêm chỉ số" ở trạng thái mock.

Tab Người liên hệ:
- Người liên hệ khẩn cấp.
- Quan hệ.
- SĐT.

Trạng thái:
- Bệnh nhân chưa hoàn thiện hồ sơ: progress card và CTA "Cập nhật hồ sơ".
- Loading khi tải hồ sơ.
- Success khi lưu chỉnh sửa.
```

