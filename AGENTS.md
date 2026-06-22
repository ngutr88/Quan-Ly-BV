# AGENTS.md

## Vai trò của tài liệu này

Tài liệu này là bộ quy tắc làm việc cho mọi agent khi phát triển repo Hệ thống Quản lý Bệnh viện (Hospital Management System - HMS). Khi có mâu thuẫn giữa suy đoán triển khai và yêu cầu sản phẩm, luôn ưu tiên `PRD.md` làm nguồn sự thật chính.

Repo hiện mô tả một web application quản lý bệnh viện toàn diện, phục vụ ba nhóm người dùng chính: Admin, Bác sĩ và Bệnh nhân. Hệ thống có thể mở rộng về sau cho Lễ tân, Dược sĩ, Kế toán và Điều dưỡng.

## Nguyên tắc cốt lõi

1. Luôn đọc `PRD.md` trước khi thiết kế hoặc sửa chức năng nghiệp vụ.
2. Không triển khai tính năng y tế, thanh toán, phân quyền hoặc dữ liệu bệnh nhân dựa trên giả định mơ hồ. Nếu PRD chưa rõ, ghi nhận giả định trong code, test hoặc tài liệu đi kèm.
3. Ưu tiên đúng luồng nghiệp vụ bệnh viện hơn giao diện đẹp đơn thuần.
4. Mọi dữ liệu y tế, lịch khám, đơn thuốc, hóa đơn và phân quyền phải được xử lý như dữ liệu nhạy cảm.
5. Không phá vỡ khả năng mở rộng vai trò trong tương lai: Lễ tân, Dược sĩ, Kế toán, Điều dưỡng.
6. Không hard-code nghiệp vụ có thể cấu hình được như giờ làm việc, số slot khám, ngưỡng tồn kho, template thông báo, bảng giá dịch vụ.
7. Với chức năng đã có dữ liệu lịch sử như hồ sơ khám, đơn thuốc, hóa đơn, ưu tiên soft delete hoặc vô hiệu hóa thay vì xóa cứng.

## Project Superpowers

Các “superpowers” của repo là những năng lực sản phẩm cần được bảo toàn khi thiết kế:

1. Quản trị tập trung cho bệnh viện: dashboard, danh mục, nhân sự, lịch khám, thuốc, hóa đơn, báo cáo và cấu hình hệ thống.
2. Luồng khám bệnh khép kín: đặt lịch, xác nhận, check-in, bác sĩ khám, kê đơn, trừ kho, sinh hóa đơn, thanh toán, đánh giá.
3. Phân quyền rõ theo vai trò: Admin toàn quyền, Bác sĩ chỉ truy cập bệnh nhân/lịch được phân công, Bệnh nhân chỉ xem dữ liệu của chính mình.
4. Điều phối lịch khám thông minh: tránh trùng lịch bác sĩ, hỗ trợ dời/hủy lịch, nhắc lịch, queue trong ngày và gợi ý slot trống.
5. Hồ sơ bệnh nhân có tính liên tục: thông tin cá nhân, tiền sử, dị ứng, lịch sử khám, đơn thuốc, hóa đơn và chỉ số sức khỏe theo thời gian.
6. Kê đơn an toàn: kiểm tra tồn kho, dị ứng, tương tác thuốc, số lượng dùng và cảnh báo thuốc thay thế khi hết hàng.
7. Quản lý kho thuốc theo lô: hạn sử dụng, nhập/xuất kho, cảnh báo sắp hết hạn, cảnh báo dưới ngưỡng tối thiểu và nguyên tắc FEFO.
8. Thanh toán linh hoạt: tại quầy và online qua VNPay, Momo, ZaloPay; có trạng thái hóa đơn, biên lai, đối soát và xuất PDF/Excel.
9. Thông báo và realtime: nhắc lịch, trạng thái lịch khám, chat/tư vấn, cảnh báo tồn kho và thông báo thanh toán.
10. Auditability: các thao tác quan trọng phải có nhật ký hệ thống đủ để truy vết ai làm gì, khi nào, với đối tượng nào.

## Ưu tiên tính năng

Khi phải chọn thứ tự triển khai, ưu tiên nhóm use case cao trong PRD:

- UC-01: Đặt lịch khám.
- UC-02: Khám bệnh.
- UC-03: Kê đơn thuốc.
- UC-04: Quản lý hóa đơn và thanh toán.
- UC-05: Quản lý tồn kho thuốc.
- UC-06: Đăng ký tài khoản bệnh nhân.
- UC-07: Đăng nhập và phân quyền.
- UC-08: Quản lý lịch khám.
- UC-09: Xem hồ sơ bệnh nhân.

Nhóm trung bình gồm lịch sử khám, quản lý bác sĩ, bệnh nhân, khoa, dịch vụ, đơn thuốc, hóa đơn và dashboard. Nhóm thấp gồm đánh giá, tư vấn, hồ sơ sức khỏe cá nhân và báo cáo nâng cao. Không để tính năng thấp làm chậm luồng khám và thanh toán cốt lõi.

## Quy tắc nghiệp vụ bắt buộc

### Phân quyền

- Admin được truy cập toàn bộ dữ liệu hệ thống theo module được cấp.
- Bác sĩ chỉ xem và thao tác với lịch khám, phiếu khám, bệnh nhân được phân công.
- Bệnh nhân chỉ truy cập dữ liệu cá nhân của chính mình hoặc hồ sơ phụ thuộc được ủy quyền.
- Mọi API hoặc action phía server phải tự kiểm tra quyền, không chỉ dựa vào ẩn/hiện UI.
- Thiết kế RBAC có thể mở rộng thành permission theo module hoặc action.

### Lịch khám

- Không cho tạo hai lịch khám trùng cùng bác sĩ, cùng khung giờ, cùng phòng nếu mô hình có phòng.
- Trạng thái lịch khám cần đủ tối thiểu: chờ xác nhận, đã xác nhận, đang khám, hoàn thành, đã hủy, vắng mặt.
- Khi dời hoặc hủy lịch, phải lưu lý do và gửi thông báo phù hợp.
- Slot khám phải tôn trọng lịch làm việc bác sĩ, ngày nghỉ, số bệnh nhân tối đa và thời lượng khám trung bình.

### Khám bệnh và hồ sơ y tế

- Phiếu khám phải liên kết với lịch khám.
- Phiếu khám cần lưu triệu chứng, chỉ số sinh tồn nếu có, chẩn đoán, chỉ định cận lâm sàng nếu có, kết quả xét nghiệm nếu có và lời dặn.
- Hồ sơ y tế của bệnh nhân phải tổng hợp lịch sử khám, đơn thuốc, hóa đơn và các cảnh báo như dị ứng hoặc bệnh nền.
- Không cho bệnh nhân tự sửa dữ liệu chuyên môn đã được bác sĩ ghi nhận.

### Kê đơn và kho thuốc

- Đơn thuốc phải liên kết với phiếu khám.
- Chi tiết đơn thuốc phải tham chiếu thuốc trong danh mục, có liều dùng, cách dùng, số lượng và số ngày dùng nếu áp dụng.
- Khi kê đơn, hệ thống phải kiểm tra tồn kho và cảnh báo dị ứng nếu có dữ liệu.
- Khi xuất kho theo đơn, ưu tiên FEFO: lô hết hạn trước được xuất trước.
- Không để tồn kho âm nếu không có cơ chế override có quyền và lý do rõ ràng.
- Cảnh báo thuốc dưới ngưỡng tối thiểu và thuốc sắp hết hạn phải hiện cho Admin hoặc Dược sĩ.

### Hóa đơn và thanh toán

- Hóa đơn phát sinh từ phiên khám, đơn thuốc và dịch vụ liên quan.
- Trạng thái thanh toán tối thiểu: chưa thanh toán, đã thanh toán, đã hủy, quá hạn nếu có công nợ.
- Thanh toán online thất bại hoặc timeout phải giữ hóa đơn ở trạng thái chưa thanh toán và cho phép thử lại.
- Callback từ cổng thanh toán phải idempotent, xác thực được nguồn và không ghi nhận thanh toán trùng.
- Mọi thay đổi hóa đơn quan trọng phải có audit log.

### Thông báo

- Các sự kiện cần thông báo: đăng ký OTP, xác nhận lịch, dời lịch, hủy lịch, nhắc lịch, thanh toán thành công/thất bại, thuốc sắp hết hoặc sắp hết hạn.
- Template thông báo nên cấu hình được.
- Không ghi thông tin y tế nhạy cảm quá chi tiết trong SMS/email nếu không cần thiết.

## Mô hình dữ liệu nền tảng

Các thực thể chính cần được giữ đồng bộ với PRD:

- `NguoiDung`: tài khoản, thông tin đăng nhập, vai trò, trạng thái.
- `BacSi`: hồ sơ chuyên môn, khoa, học vị, kinh nghiệm, lịch làm việc.
- `BenhNhan`: hồ sơ cá nhân, BHYT, tiền sử bệnh, dị ứng, người liên hệ khẩn cấp.
- `Khoa`: danh mục khoa, vị trí, trưởng khoa, dịch vụ liên quan.
- `DichVu`: dịch vụ khám/chữa bệnh và bảng giá.
- `LichKham`: lịch hẹn, bác sĩ, bệnh nhân, thời gian, trạng thái, lý do khám.
- `PhieuKham`: nội dung khám, chẩn đoán, lời dặn, cận lâm sàng.
- `DonThuoc` và `ChiTietDonThuoc`: đơn thuốc và từng thuốc trong đơn.
- `Thuoc` và `LoThuoc`: danh mục thuốc, giá, tồn kho, lô, hạn sử dụng.
- `HoaDon` và `ChiTietHoaDon`: hóa đơn, phí khám, phí thuốc, phí dịch vụ, trạng thái thanh toán.
- `DanhGia`: phản hồi bệnh nhân về bác sĩ hoặc dịch vụ.
- `ThongBao`: thông báo hệ thống.
- `NhatKyHeThong`: audit log.

Tên bảng, field và route có thể theo convention của stack được chọn, nhưng ý nghĩa nghiệp vụ phải giữ nguyên.

## Gợi ý kiến trúc

PRD cho phép lựa chọn stack. Nếu chưa có codebase cố định, ưu tiên một trong các hướng sau:

- Frontend: React hoặc Vue với Tailwind CSS, responsive cho bệnh nhân, desktop-first cho Admin và Bác sĩ.
- Backend: Node.js Express/NestJS hoặc Java Spring Boot.
- Database: PostgreSQL hoặc MySQL.
- Authentication: JWT hoặc session bảo mật, RBAC rõ ràng.
- Realtime: WebSocket hoặc Socket.io cho lịch khám, thông báo, chat.
- Export: PDF và Excel cho báo cáo, hóa đơn, danh sách.
- Triển khai: Docker và CI/CD khi dự án đủ cấu trúc.

Khi codebase đã hình thành, luôn đi theo framework, convention, formatter và test runner hiện có của repo.

## Quy tắc frontend

- Admin và Bác sĩ ưu tiên giao diện dày thông tin, dễ lọc, dễ tìm kiếm, phù hợp desktop.
- Bệnh nhân cần responsive tốt trên mobile và desktop.
- Các màn hình chính cần có trạng thái loading, empty, error và permission denied.
- Bảng dữ liệu cần có tìm kiếm, lọc, phân trang và trạng thái rõ ràng.
- Form y tế và thanh toán phải có validation phía client và server.
- Không dùng màu sắc làm tín hiệu duy nhất cho cảnh báo; cần text/icon/trạng thái kèm theo.
- Các hành động nguy hiểm như hủy lịch, khóa tài khoản, hủy hóa đơn, override dị ứng thuốc phải có xác nhận.

## Quy tắc backend

- Validate dữ liệu ở boundary: request DTO, service/domain layer và database constraints khi cần.
- Luồng nghiệp vụ quan trọng nên nằm ở service/domain layer, không nhồi hết vào controller.
- Các thao tác nhiều bước như hoàn tất khám, kê đơn, trừ kho, sinh hóa đơn phải dùng transaction.
- API phải idempotent khi xử lý callback thanh toán, gửi lại OTP, retry notification hoặc action dễ lặp.
- Luôn ghi audit log cho thao tác nhạy cảm: đăng nhập thất bại nhiều lần, đổi quyền, khóa tài khoản, sửa hồ sơ y tế, hủy lịch, hủy hóa đơn, nhập/xuất kho, override cảnh báo thuốc.
- Không lưu mật khẩu plain text. Luôn hash mật khẩu và bảo vệ secret bằng biến môi trường.

## Quy tắc kiểm thử

- Test tối thiểu cho logic phân quyền, đặt lịch, xung đột lịch, kê đơn, trừ kho FEFO, tạo hóa đơn và callback thanh toán.
- Với bug trong nghiệp vụ, thêm test tái hiện trước hoặc cùng lúc với sửa lỗi.
- Với form quan trọng, test validation các trường bắt buộc, format ngày giờ, số điện thoại, email, số lượng thuốc và số tiền.
- Với API bảo mật, test user không đúng vai trò hoặc không sở hữu dữ liệu bị từ chối.

## Quy tắc bảo mật và dữ liệu nhạy cảm

- Không log mật khẩu, token, OTP, thông tin thanh toán nhạy cảm hoặc nội dung hồ sơ y tế không cần thiết.
- OTP phải có thời hạn, giới hạn số lần nhập sai và khả năng gửi lại có cooldown.
- Dữ liệu bệnh nhân phải luôn được lọc theo phạm vi truy cập của người dùng hiện tại.
- File đính kèm như bằng cấp, kết quả xét nghiệm hoặc hóa đơn cần kiểm soát quyền tải xuống.
- Dữ liệu ngày giờ cần nhất quán timezone. Repo đang ở bối cảnh Việt Nam, ưu tiên Asia/Ho_Chi_Minh khi hiển thị cho người dùng.

## Quy tắc khi agent làm việc trong repo

1. Trước khi sửa code, đọc cấu trúc hiện tại bằng `rg --files` và mở các file liên quan.
2. Không tạo abstraction lớn nếu repo chưa cần. Bắt đầu bằng module rõ ràng, test được.
3. Giữ thay đổi nhỏ, có thể review được, bám đúng module yêu cầu.
4. Không xóa hoặc đảo ngược thay đổi không do mình tạo.
5. Sau khi sửa, chạy formatter, typecheck, lint hoặc test phù hợp nếu repo có script.
6. Nếu chưa có code, khi scaffold hãy tạo cấu trúc hỗ trợ module hóa theo domain: auth, users, doctors, patients, departments, appointments, examinations, prescriptions, medicines, invoices, notifications, reports.
7. Cập nhật tài liệu khi thêm quyết định kiến trúc hoặc thay đổi nghiệp vụ khác PRD.
8. Sau khi thực hiện xong một task, cập nhật vào folder `timelines` với file được tạo theo định dạng `DD-MM-YYYY.md` (ví dụ: `21-06-2026.md`).
9. Sau khi kết thúc một ngày hoặc khi được yêu cầu update version, cập nhật thông tin vào file `CHANGELOG.md`.

## Điều không nên làm

- Không cho Bác sĩ xem toàn bộ bệnh nhân nếu không được phân công.
- Không cho Bệnh nhân xem đơn thuốc, hóa đơn hoặc lịch khám của người khác.
- Không trừ kho thuốc mà không có đơn thuốc hoặc chứng từ rõ ràng.
- Không tạo hóa đơn đã thanh toán khi callback thanh toán chưa xác thực.
- Không xóa cứng hồ sơ y tế, lịch khám, đơn thuốc hoặc hóa đơn đã phát sinh nghiệp vụ.
- Không hard-code danh sách khoa, bác sĩ, thuốc, dịch vụ, giá hoặc lịch làm việc vào UI.
- Không triển khai chatbot, telemedicine hoặc big data trước khi luồng cốt lõi ổn định.

