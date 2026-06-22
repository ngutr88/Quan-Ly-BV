# 02. Đăng ký bệnh nhân

```text
Tạo page Đăng ký tài khoản cho Bệnh nhân trong HMS.

Bối cảnh:
- Dành cho bệnh nhân mới chưa có tài khoản.
- Mục tiêu là tạo tài khoản để đặt lịch khám, xem đơn thuốc, xem hóa đơn và thanh toán.
- Page phải thân thiện trên mobile.

Bố cục:
- Header đơn giản có logo HMS và link "Đăng nhập".
- Form dạng nhiều bước hoặc 2 cột trên desktop, một cột trên mobile.
- Thanh tiến trình 3 bước:
  1. Thông tin tài khoản.
  2. Thông tin cá nhân.
  3. Xác thực OTP.

Nội dung:
- Tiêu đề "Đăng ký tài khoản bệnh nhân".
- Mô tả: "Tạo tài khoản để đặt lịch khám, xem đơn thuốc và thanh toán hóa đơn".
- Section Thông tin tài khoản:
  - Email hoặc SĐT.
  - Mật khẩu.
  - Nhập lại mật khẩu.
- Section Thông tin cá nhân:
  - Họ tên.
  - Ngày sinh.
  - Giới tính.
  - Số CCCD tùy chọn.
  - Số thẻ BHYT tùy chọn.
  - Địa chỉ.
  - Người liên hệ khẩn cấp tùy chọn.
- Checkbox đồng ý điều khoản sử dụng và chính sách bảo mật dữ liệu y tế.
- Button chính "Tiếp tục xác thực".
- Link quay lại "Đã có tài khoản? Đăng nhập".

Validation cần có:
- Email/SĐT không hợp lệ.
- Email/SĐT đã tồn tại.
- Mật khẩu quá yếu.
- Mật khẩu nhập lại không khớp.
- Ngày sinh không hợp lệ.
- Chưa đồng ý điều khoản.

Trạng thái:
- Loading khi gửi thông tin.
- Error network.
- Success chuyển sang bước OTP.

Phong cách:
- Dễ đọc, ít thuật ngữ y khoa.
- Các field dài nên nhóm rõ ràng để bệnh nhân không thấy quá tải.
```

