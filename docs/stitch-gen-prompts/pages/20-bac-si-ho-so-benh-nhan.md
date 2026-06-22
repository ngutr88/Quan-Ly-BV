# 20. Bác sĩ Hồ sơ bệnh nhân

```text
Tạo page Hồ sơ bệnh nhân dành cho Bác sĩ.

Bối cảnh:
- Bác sĩ xem hồ sơ các bệnh nhân được phân công.
- Nếu không có quyền, hiển thị permission denied trong nội dung.

App shell:
- Dùng shell Bác sĩ.
- Breadcrumb: Bác sĩ / Hồ sơ bệnh nhân.

Bố cục:
- Header: "Hồ sơ bệnh nhân".
- Search bệnh nhân trong phạm vi được phân công.
- Nếu chọn bệnh nhân, hiển thị detail với tabs.

Nội dung đầu trang:
- Card thông tin cá nhân:
  - Họ tên.
  - Tuổi.
  - Giới tính.
  - SĐT.
  - BHYT nếu có.
- Alert dị ứng và bệnh nền.

Tabs:
- Tổng quan.
- Lịch sử khám.
- Đơn thuốc.
- Kết quả cận lâm sàng.
- Chỉ số sức khỏe.

Tab Tổng quan:
- Tiền sử bệnh.
- Bệnh nền.
- Dị ứng.
- Người liên hệ khẩn cấp.

Tab Lịch sử khám:
- Timeline các lần khám:
  - Ngày.
  - Khoa.
  - Bác sĩ.
  - Chẩn đoán.
  - Lời dặn.

Tab Đơn thuốc:
- Danh sách đơn thuốc cũ.
- Thuốc, liều dùng, thời gian.

Tab Kết quả cận lâm sàng:
- Danh sách kết quả xét nghiệm/siêu âm/nội soi mock.

Tab Chỉ số sức khỏe:
- Biểu đồ huyết áp, cân nặng, đường huyết nếu có mock.

Trạng thái:
- Loading khi tải hồ sơ.
- Empty nếu chưa có lịch sử.
- Permission denied nếu bệnh nhân không thuộc phạm vi bác sĩ.
```

