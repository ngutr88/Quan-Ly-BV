# 04. Quên mật khẩu

```text
Tạo page Quên mật khẩu cho HMS.

Bối cảnh:
- Dành cho Admin, Bác sĩ hoặc Bệnh nhân cần khôi phục mật khẩu.
- Giao diện cùng phong cách với page Đăng nhập.

Bố cục:
- Card gọn nằm giữa màn hình.
- Có 3 bước trong cùng page hoặc wizard nhỏ:
  1. Nhập email/SĐT.
  2. Xác thực OTP.
  3. Đặt mật khẩu mới.

Nội dung:
- Tiêu đề "Khôi phục mật khẩu".
- Mô tả: "Nhập email hoặc số điện thoại đã đăng ký để nhận mã xác thực".
- Input Email hoặc SĐT.
- Button "Gửi mã xác thực".
- Sau khi gửi mã:
  - Input OTP.
  - Input mật khẩu mới.
  - Input nhập lại mật khẩu mới.
  - Button "Đổi mật khẩu".
- Link quay lại đăng nhập.

Trạng thái:
- Không tìm thấy tài khoản.
- Gửi OTP thành công.
- OTP sai hoặc hết hạn.
- Mật khẩu mới không khớp.
- Mật khẩu mới quá yếu.
- Đổi mật khẩu thành công, hiển thị CTA quay lại đăng nhập.

Phong cách:
- Rõ ràng, an toàn, không gây hoang mang.
- Không hiển thị mật khẩu hoặc OTP dạng plain text.
```

