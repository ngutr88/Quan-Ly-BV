# 18. Bác sĩ Phiên khám bệnh

```text
Tạo page Phiên khám bệnh cho Bác sĩ.

Bối cảnh:
- Đây là màn hình bác sĩ ghi nhận thông tin khám bệnh.
- Mục tiêu là thao tác nhanh, rõ, giảm sai sót và dễ chuyển sang kê đơn.

App shell:
- Dùng shell Bác sĩ.
- Header sticky luôn hiển thị bệnh nhân đang khám.

Bố cục:
- Header sticky:
  - Tên bệnh nhân.
  - Mã bệnh nhân.
  - Tuổi.
  - Giới tính.
  - Trạng thái phiên khám.
- Layout 3 vùng trên desktop:
  1. Sidebar tóm tắt bệnh nhân.
  2. Form khám chính.
  3. Panel lịch sử liên quan.

Sidebar bệnh nhân:
- Thông tin cơ bản.
- Dị ứng thuốc/thực phẩm.
- Bệnh nền.
- Chỉ số gần nhất.
- Người liên hệ khẩn cấp nếu cần.

Form khám chính:
- Triệu chứng.
- Chỉ số sinh tồn:
  - Nhiệt độ.
  - Mạch.
  - Huyết áp.
  - SpO2.
  - Cân nặng.
- Chẩn đoán sơ bộ.
- Chỉ định cận lâm sàng: checkbox hoặc multi-select xét nghiệm/siêu âm/nội soi.
- Kết quả cận lâm sàng: vùng nhập hoặc upload mock.
- Chẩn đoán cuối cùng.
- Lời dặn.
- Button:
  - "Lưu nháp".
  - "Kê đơn thuốc".
  - "Hoàn tất khám".

Panel lịch sử:
- Lịch sử khám gần đây.
- Đơn thuốc cũ.
- Kết quả xét nghiệm gần đây.

Trạng thái:
- Autosave draft mock.
- Cảnh báo khi rời page chưa lưu.
- Không cho hoàn tất nếu thiếu chẩn đoán cuối cùng.
- Loading khi lưu phiếu khám.
```

