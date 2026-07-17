# Danh sách tài khoản demo HMS

> Tài liệu này chỉ dành cho môi trường demo/local. Không sử dụng các mật khẩu bên dưới trong production và không đưa thông tin tài khoản thật vào repository.

## Mật khẩu mặc định

| Vai trò | Mật khẩu demo |
|---|---|
| Admin | `Admin@123` |
| Bác sĩ | `Doctor@123` |
| Bệnh nhân | `Patient@123` |

## Admin

| Họ tên | Email đăng nhập | Mật khẩu | Trạng thái |
|---|---|---|---|
| Quản trị viên Hệ thống | `admin@hms.com` | `Admin@123` | Active |
| Nguyen Minh Quan | `admin.operations@hms.com` | `Admin@123` | Active |
| Le Thu Huong | `admin.finance@hms.com` | `Admin@123` | Active |
| Tran Duc Long | `admin.reception@hms.com` | `Admin@123` | Active |

## Bác sĩ

### Tài khoản cố định

| Họ tên | Email đăng nhập | Chuyên khoa | Mật khẩu |
|---|---|---|---|
| Nguyễn Văn Trung | `doctor@hms.com` | Tim mạch | `Doctor@123` |
| Lê Thị Mai | `nhi@hms.com` | Nhi khoa | `Doctor@123` |
| Phạm Đức Hùng | `noitq@hms.com` | Nội tổng quát | `Doctor@123` |
| Vũ Thị Hương | `tamh@hms.com` | Tai Mũi Họng | `Doctor@123` |
| Trần Minh Khoa | `ngoai@hms.com` | Ngoại tổng hợp | `Doctor@123` |
| Nguyễn Thị Lan Anh | `dalieu@hms.com` | Da liễu | `Doctor@123` |
| Hoàng Thị Thu Hà | `sanpk@hms.com` | Sản phụ khoa | `Doctor@123` |
| Đỗ Quang Minh | `thankinh@hms.com` | Thần kinh | `Doctor@123` |
| Trần Quốc Anh | `timbach2@hms.com` | Tim mạch | `Doctor@123` |
| Phan Thị Ngọc Diệp | `nhi2@hms.com` | Nhi khoa | `Doctor@123` |
| Vũ Hoàng Nam | `noitq2@hms.com` | Nội tổng quát | `Doctor@123` |
| Lý Thị Bích Ngọc | `mat@hms.com` | Mắt | `Doctor@123` |
| Nguyễn Đình Toàn | `chinhhinh@hms.com` | Chấn thương chỉnh hình | `Doctor@123` |
| Huỳnh Văn Đạt | `hoitroc@hms.com` | Hồi sức cấp cứu | `Doctor@123` |
| Trần Thị Thanh Nhàn | `ungbuou@hms.com` | Ung bướu | `Doctor@123` |
| Lê Đức Thành | `rhm@hms.com` | Răng Hàm Mặt | `Doctor@123` |
| Phạm Văn Khánh | `tietnieu@hms.com` | Tiết niệu | `Doctor@123` |
| Nguyễn Thị Phương Lan | `yhct@hms.com` | Y học cổ truyền | `Doctor@123` |
| Nguyen Hoang Phuc | `doctor.phuc@hms.com` | Nội tổng quát | `Doctor@123` |
| Pham Ngoc Anh | `doctor.anh@hms.com` | Nhi khoa | `Doctor@123` |
| Vo Thanh Dat | `doctor.dat@hms.com` | Ngoại tổng hợp | `Doctor@123` |
| Do Minh Chau | `doctor.chau@hms.com` | Da liễu | `Doctor@123` |
| Bui Quoc Thai | `doctor.thai@hms.com` | Tai Mũi Họng | `Doctor@123` |
| Hoang Yen Nhi | `doctor.nhi@hms.com` | Sản phụ khoa | `Doctor@123` |

### Tài khoản bác sĩ sinh tự động

Seed tạo thêm các tài khoản dạng `bs###@hms.com` để mỗi khoa có khoảng 15–20 bác sĩ.

- Email: `bs100@hms.com`, `bs101@hms.com`, ... theo số lượng thực tế của từng khoa.
- Mật khẩu: `Doctor@123`.
- Tài khoản được phân khoa, chuyên khoa con, học vị, kinh nghiệm và lịch làm việc tự động.

## Bệnh nhân

### Tài khoản cố định

| Họ tên | Email đăng nhập | Mật khẩu |
|---|---|---|
| Trần Văn A | `patient@hms.com` | `Patient@123` |
| Phạm Thị B | `patient2@hms.com` | `Patient@123` |
| Lê Văn Cường | `patient3@hms.com` | `Patient@123` |
| Nguyễn Thị Hồng | `patient4@hms.com` | `Patient@123` |
| Đặng Văn Long | `patient5@hms.com` | `Patient@123` |
| Trần Thị Minh Tú | `patient6@hms.com` | `Patient@123` |
| Bùi Minh Đức | `patient7@hms.com` | `Patient@123` |
| Vũ Thị Thanh Tâm | `patient8@hms.com` | `Patient@123` |
| Hồ Quang Hiếu | `patient9@hms.com` | `Patient@123` |
| Lý Thị Mỹ Dung | `patient10@hms.com` | `Patient@123` |
| Nguyễn Hải Yến | `patient11@hms.com` | `Patient@123` |
| Trần Quốc Bảo | `patient12@hms.com` | `Patient@123` |
| Phạm Ngọc Lan | `patient13@hms.com` | `Patient@123` |
| Lâm Quốc Bảo | `patient15@hms.com` | `Patient@123` |

### Tài khoản gia đình và dữ liệu mở rộng

| Email đăng nhập | Mật khẩu | Ghi chú |
|---|---|---|
| `patient.family01@hms.com` | `Patient@123` | Có hồ sơ người phụ thuộc |
| `patient.family02@hms.com` | `Patient@123` | Có hồ sơ người phụ thuộc |
| `patient.family03@hms.com` | `Patient@123` | Có hồ sơ người phụ thuộc |
| `patient.family04@hms.com` | `Patient@123` | Có hồ sơ người phụ thuộc |
| `patient.demo01@hms.com` – `patient.demo08@hms.com` | `Patient@123` | Hồ sơ demo có BHYT, CCCD, tiền sử bệnh và dị ứng |

## Lưu ý khi sử dụng

- Các tài khoản được tạo bởi `Data/DbSeeder.cs` và chỉ thêm nếu email chưa tồn tại.
- Sau khi bổ sung seed, cần khởi động lại ứng dụng để dữ liệu được nạp vào database.
- Tài khoản `bs###@hms.com` là tài khoản sinh tự động, số lượng có thể thay đổi theo số khoa và dữ liệu hiện có.
- Không dùng file này để lưu tài khoản production, mật khẩu thật, token, OTP hoặc thông tin thanh toán.
