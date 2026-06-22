# 03. Xác thực OTP

```text
Tạo page Xác thực OTP cho bệnh nhân.

Bối cảnh:
- Page xuất hiện sau đăng ký hoặc khôi phục mật khẩu.
- Mục tiêu là xác thực email/SĐT bằng mã OTP.

Bố cục:
- Card trung tâm màn hình.
- Header có icon bảo mật hoặc điện thoại.
- Có link quay lại chỉnh thông tin đăng ký.
- Trên mobile, input OTP phải lớn và dễ bấm.

Nội dung:
- Tiêu đề "Xác thực tài khoản".
- Text: "Mã OTP đã được gửi đến email/SĐT của bạn".
- Input OTP gồm 6 ô số.
- Countdown "Gửi lại mã sau 59 giây".
- Button chính "Xác thực".
- Link "Gửi lại mã" khi hết countdown.
- Ghi chú bảo mật: "Không chia sẻ mã OTP với bất kỳ ai".

Trạng thái cần có:
- Loading khi xác thực.
- OTP sai.
- OTP hết hạn.
- Sai quá 5 lần: alert "Tài khoản bị khóa tạm thời, vui lòng thử lại sau".
- Gửi lại mã thành công.
- Thành công: message "Tài khoản đã được kích hoạt".

Phong cách:
- Tối giản, tập trung vào OTP.
- Dùng màu warning cho lỗi nhập sai, success cho xác thực thành công.
```

