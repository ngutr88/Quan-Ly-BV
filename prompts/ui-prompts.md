# UI Prompts cho Hệ thống Quản lý Bệnh viện

Tài liệu này chứa bộ prompt dùng để xây dựng giao diện HMS theo `PRD.md` và `AGENTS.md`. Dùng các prompt theo thứ tự từ trên xuống để tạo giao diện nhất quán, ưu tiên luồng cốt lõi: đăng nhập, đặt lịch, khám bệnh, kê đơn, kho thuốc, hóa đơn và thanh toán.

Khi dùng prompt với công cụ sinh code/UI, hãy thay phần `[STACK]` bằng stack thực tế. Nếu chưa chọn stack, mặc định dùng: React, TypeScript, Tailwind CSS, shadcn/ui hoặc component system tương đương, lucide-react cho icon.

## 00. Master Prompt

```text
Bạn là senior product UI engineer. Hãy xây dựng giao diện web application cho Hệ thống Quản lý Bệnh viện (Hospital Management System - HMS) dựa trên PRD sau:

- Sản phẩm phục vụ 3 vai trò chính: Admin, Bác sĩ, Bệnh nhân.
- Admin dùng desktop để quản trị dashboard, bác sĩ, bệnh nhân, khoa, lịch khám, thuốc, hóa đơn, nhân sự/phân quyền, dịch vụ/giá, báo cáo và cấu hình hệ thống.
- Bác sĩ dùng desktop để xem lịch khám, khám bệnh, xem hồ sơ bệnh nhân, kê đơn thuốc, xem lịch sử khám, thống kê cá nhân và trao đổi với bệnh nhân.
- Bệnh nhân dùng desktop và mobile responsive để đăng ký, đặt lịch khám, xem lịch khám, xem đơn thuốc, xem hóa đơn, thanh toán online, xem hồ sơ sức khỏe, nhận thông báo và đánh giá dịch vụ.
- Luồng nghiệp vụ quan trọng nhất: bệnh nhân đặt lịch -> hệ thống/Admin xác nhận -> bệnh nhân check-in -> bác sĩ khám -> kê đơn nếu cần -> trừ kho thuốc -> sinh hóa đơn -> thanh toán -> bệnh nhân xem lại đơn thuốc/hóa đơn -> đánh giá.
- Dữ liệu y tế và thanh toán là dữ liệu nhạy cảm. Giao diện phải thể hiện rõ phân quyền, trạng thái và cảnh báo.

Stack: [STACK]

Yêu cầu UI:
- Thiết kế giao diện thật sự dùng được, không phải landing page.
- Admin và Bác sĩ: desktop-first, dày thông tin, bảng/lọc/tìm kiếm rõ ràng.
- Bệnh nhân: responsive tốt, thao tác đặt lịch và thanh toán đơn giản.
- Có layout app shell theo vai trò: sidebar, topbar, breadcrumb, thông báo, avatar/tài khoản.
- Có trạng thái loading, empty, error, permission denied cho các màn hình chính.
- Có xác nhận cho hành động nguy hiểm: hủy lịch, khóa tài khoản, hủy hóa đơn, override cảnh báo thuốc.
- Không dùng dữ liệu thật. Tạo mock data Việt Nam hợp lý.
- Dùng tiếng Việt cho toàn bộ nhãn, trạng thái, button và thông báo.

Hãy tạo cấu trúc giao diện module hóa, dễ nối API sau này. Không triển khai backend thật. Tập trung vào UI, state giả lập, data model mock và các luồng tương tác chính.
```

## 01. Design System Prompt

```text
Tạo design system cho HMS bằng [STACK].

Mục tiêu:
- Giao diện bệnh viện hiện đại, sạch, tin cậy, dễ đọc trong môi trường vận hành.
- Tránh phong cách landing page, tránh gradient trang trí quá đà.
- Dùng màu chủ đạo xanh y tế và trung tính: primary, success, warning, danger, info, muted.
- Typography rõ ràng cho bảng dữ liệu, form y tế và dashboard.

Component cần có:
- AppShell theo vai trò: AdminShell, DoctorShell, PatientShell.
- Sidebar navigation, Topbar, Breadcrumbs, UserMenu, NotificationBell.
- Button, IconButton, Badge, StatusBadge, Alert, Toast.
- DataTable với search, filter, pagination, row action.
- FormField, Select, DatePicker, TimeSlotPicker, Textarea, NumberInput.
- Modal, ConfirmDialog, Drawer, Tabs, EmptyState, LoadingState, ErrorState.
- KPI Card, Chart Card, Timeline, Activity Log item.
- PatientSummaryCard, DoctorCard, AppointmentCard, InvoiceSummary, PrescriptionPreview.

Quy tắc:
- Status lịch khám gồm: Chờ xác nhận, Đã xác nhận, Đang khám, Hoàn thành, Đã hủy, Vắng mặt.
- Status hóa đơn gồm: Chưa thanh toán, Đã thanh toán, Đã hủy, Quá hạn.
- Status kho thuốc gồm: Còn hàng, Sắp hết, Hết hàng, Sắp hết hạn.
- Mỗi status phải có màu, icon và text, không chỉ màu.
- Component phải nhận props rõ ràng và có mock examples.

Kết quả mong muốn:
- Tạo theme/tokens, layout primitives và các component dùng lại cho toàn hệ thống.
- Tạo trang demo component hoặc story/demo section nếu phù hợp với stack.
```

## 02. Auth và Role Routing Prompt

```text
Xây dựng giao diện xác thực và điều hướng theo vai trò cho HMS bằng [STACK].

Màn hình cần có:
- Đăng nhập.
- Đăng ký bệnh nhân.
- Xác thực OTP.
- Quên mật khẩu.
- Chọn vai trò giả lập để demo nếu chưa có backend.

Yêu cầu nghiệp vụ:
- Sau đăng nhập, Admin vào trang Admin Dashboard.
- Bác sĩ vào trang Lịch khám hôm nay.
- Bệnh nhân vào trang Trang chủ bệnh nhân.
- Bệnh nhân đăng ký cần nhập họ tên, email hoặc SĐT, mật khẩu, ngày sinh, giới tính, số CCCD hoặc BHYT tùy chọn.
- OTP có trạng thái đếm ngược, gửi lại mã, sai quá nhiều lần thì hiển thị khóa tạm thời.
- Không hiển thị hoặc log mật khẩu/OTP thật.

Yêu cầu UI:
- Form rõ ràng, validation trực quan.
- Thông báo lỗi đăng nhập sai, tài khoản bị khóa, thiếu quyền.
- Responsive tốt trên mobile.
- Có mock account cho 3 vai trò để thử nhanh.
```

## 03. Admin Dashboard Prompt

```text
Xây dựng màn hình Admin Dashboard cho HMS bằng [STACK].

Mục tiêu:
- Cho Admin nhìn nhanh tình hình bệnh viện trong ngày và trong tháng.
- Ưu tiên tính vận hành, cảnh báo và khả năng ra quyết định.

Thành phần UI:
- KPI cards: tổng bác sĩ đang hoạt động, tổng bệnh nhân, lịch khám hôm nay, doanh thu hôm nay/tháng này.
- Biểu đồ doanh thu theo ngày/tuần/tháng.
- Biểu đồ lượt khám theo khoa.
- Top khoa có lượt khám nhiều nhất.
- Top bác sĩ có lượt khám/đánh giá cao nhất.
- Danh sách lịch khám sắp diễn ra trong 2 giờ tới.
- Cảnh báo: thuốc sắp hết hàng, thuốc sắp hết hạn, hóa đơn quá hạn, lịch khám xung đột.
- Widget nhật ký hoạt động gần đây.
- Bộ lọc thời gian: Hôm nay, 7 ngày, 30 ngày, tùy chọn khoảng ngày.
- Button xuất báo cáo PDF/Excel ở trạng thái mock.

Yêu cầu:
- Dữ liệu mock phải giống bệnh viện Việt Nam.
- Tất cả chart có fallback khi không có dữ liệu.
- Cảnh báo quan trọng phải nổi bật nhưng không gây rối.
- Desktop-first, vẫn không vỡ layout ở tablet.
```

## 04. Admin Quản lý Lịch khám Prompt

```text
Xây dựng module Admin Quản lý Lịch khám cho HMS bằng [STACK].

Màn hình cần có:
- Danh sách lịch khám dạng bảng.
- Lịch dạng calendar ngày/tuần/tháng.
- Bộ lọc theo ngày, khoa, bác sĩ, trạng thái.
- Drawer hoặc modal chi tiết lịch khám.
- Form tạo lịch thủ công tại quầy.
- Flow xác nhận, từ chối, dời lịch, hủy lịch kèm lý do.
- Queue số thứ tự khám trong ngày.

Quy tắc nghiệp vụ:
- Không cho tạo lịch trùng bác sĩ cùng khung giờ.
- Khi dời/hủy lịch phải bắt buộc nhập lý do.
- Khi xác nhận lịch, hiển thị mock notification gửi bệnh nhân và bác sĩ.
- Status gồm: Chờ xác nhận, Đã xác nhận, Đang khám, Hoàn thành, Đã hủy, Vắng mặt.

Yêu cầu UI:
- Bảng có row actions rõ ràng.
- Calendar thể hiện trạng thái bằng badge text + màu.
- Có cảnh báo xung đột lịch trước khi lưu.
- Có empty state khi không có lịch.
```

## 05. Admin Quản lý Bác sĩ, Bệnh nhân, Khoa và Dịch vụ Prompt

```text
Xây dựng các module quản trị danh mục cho HMS bằng [STACK].

Module 1: Quản lý Bác sĩ
- Danh sách, tìm kiếm, lọc theo khoa/chuyên môn/trạng thái.
- Hồ sơ chi tiết gồm thông tin cá nhân, chuyên môn, học vị, số năm kinh nghiệm, chứng chỉ, lịch làm việc, tài khoản.
- Form thêm/sửa bác sĩ.
- Hành động khóa/mở tài khoản, đặt lại mật khẩu, vô hiệu hóa mềm.

Module 2: Quản lý Bệnh nhân
- Danh sách, tìm kiếm theo tên, SĐT, mã bệnh nhân, CCCD, BHYT.
- Hồ sơ cá nhân, tiền sử bệnh, dị ứng, lịch sử khám, đơn thuốc, hóa đơn.
- Gộp hồ sơ trùng lặp ở trạng thái mock.
- Ghi chú nội bộ.

Module 3: Quản lý Khoa và Dịch vụ
- Danh sách khoa, vị trí, trưởng khoa, số bác sĩ, lượt khám, doanh thu.
- Quản lý dịch vụ theo khoa: tên dịch vụ, giá, thời gian thực hiện, trạng thái.
- Form thêm/sửa khoa và dịch vụ.

Yêu cầu:
- Dùng DataTable thống nhất.
- Hành động xóa phải là vô hiệu hóa mềm.
- Có trang/detail drawer cho từng entity.
- Không hiển thị thông tin y tế nhạy cảm quá mức ở danh sách; đưa vào detail có kiểm soát.
```

## 06. Admin Quản lý Thuốc và Kho Prompt

```text
Xây dựng module Quản lý Thuốc và Kho dược cho HMS bằng [STACK].

Màn hình cần có:
- Danh mục thuốc: tên thuốc, SKU, hoạt chất, đơn vị tính, dạng bào chế, nhà sản xuất, nhóm dược lý, giá nhập, giá bán, trạng thái.
- Tồn kho theo thuốc và theo lô.
- Chi tiết thuốc có danh sách lô: số lô, hạn sử dụng, số lượng, ngày nhập, nhà cung cấp.
- Form nhập kho.
- Lịch sử nhập/xuất kho.
- Cảnh báo thuốc sắp hết hàng và sắp hết hạn.
- Báo cáo thuốc tiêu thụ nhiều nhất ở mock.

Quy tắc nghiệp vụ:
- Xuất kho theo đơn thuốc dùng nguyên tắc FEFO: lô hết hạn trước xuất trước.
- Không hiển thị tồn kho âm.
- Thuốc dưới ngưỡng tối thiểu có status Sắp hết.
- Lô thuốc còn 30/60/90 ngày hết hạn cần cảnh báo.

Yêu cầu UI:
- Có filter theo trạng thái tồn kho, nhóm thuốc, hạn sử dụng.
- Cảnh báo phải có text rõ ràng cho Admin/Dược sĩ.
- Form nhập kho kiểm tra số lượng, hạn sử dụng và giá nhập.
```

## 07. Admin Quản lý Hóa đơn và Thanh toán Prompt

```text
Xây dựng module Quản lý Hóa đơn và Thanh toán cho HMS bằng [STACK].

Màn hình cần có:
- Danh sách hóa đơn với filter theo trạng thái, ngày, bệnh nhân, khoa, phương thức thanh toán.
- Chi tiết hóa đơn: phí khám, phí xét nghiệm/cận lâm sàng, phí thuốc, phí dịch vụ khác, BHYT/giảm giá nếu có, tổng tiền.
- Flow xác nhận thanh toán tại quầy: tiền mặt, chuyển khoản, thẻ.
- Flow thanh toán online mock: VNPay, Momo, ZaloPay.
- Trạng thái callback thành công, thất bại, timeout ở mock.
- Button in hóa đơn/xuất PDF.
- Báo cáo doanh thu theo ngày/tháng/khoa/bác sĩ.

Quy tắc nghiệp vụ:
- Hóa đơn tạo từ phiên khám và đơn thuốc.
- Thanh toán online thất bại phải giữ trạng thái Chưa thanh toán.
- Hủy hóa đơn phải nhập lý do và hiển thị confirm dialog.
- Mọi thao tác quan trọng hiển thị audit trail.

Yêu cầu UI:
- Số tiền định dạng VND.
- Status hóa đơn rõ ràng.
- Chi tiết hóa đơn in được, gọn, đúng nghiệp vụ.
```

## 08. Bác sĩ Workspace Prompt

```text
Xây dựng workspace cho vai trò Bác sĩ trong HMS bằng [STACK].

Màn hình cần có:
- Lịch khám hôm nay theo danh sách và calendar.
- Chi tiết lịch hẹn và thông tin bệnh nhân được phân công.
- Hồ sơ bệnh nhân: thông tin cơ bản, tiền sử bệnh, dị ứng, lịch sử khám, đơn thuốc cũ, hóa đơn liên quan ở mức cần thiết.
- Phiên khám bệnh: bắt đầu khám, nhập triệu chứng, chỉ số sinh tồn, chẩn đoán sơ bộ, chỉ định cận lâm sàng, kết quả, chẩn đoán cuối cùng, lời dặn.
- Kê đơn thuốc: tìm thuốc, thêm thuốc, liều dùng, cách dùng, số ngày, số lượng tự tính, cảnh báo dị ứng, cảnh báo tồn kho, cảnh báo tương tác thuốc ở mock.
- Hoàn tất khám: cập nhật lịch Hoàn thành và tạo hóa đơn nháp mock.
- Thống kê cá nhân: số lượt khám, đánh giá trung bình, biểu đồ hiệu suất.

Quy tắc nghiệp vụ:
- Bác sĩ chỉ xem bệnh nhân/lịch được phân công.
- Không cho sửa hồ sơ y tế ngoài phiên khám nếu không có quyền.
- Nếu dị ứng trùng thuốc, bắt buộc override có lý do hoặc chọn thuốc khác.
- Nếu thuốc hết hàng, đề xuất thuốc thay thế cùng hoạt chất ở mock.

Yêu cầu UI:
- Desktop-first, thao tác nhanh.
- Giao diện khám bệnh nên chia vùng: thông tin bệnh nhân, form khám, lịch sử liên quan.
- Có autosave draft mock cho phiếu khám.
- Có cảnh báo rõ nhưng không che mất luồng khám.
```

## 09. Bệnh nhân Portal Prompt

```text
Xây dựng portal Bệnh nhân cho HMS bằng [STACK].

Màn hình cần có:
- Trang chủ bệnh nhân với lịch sắp tới, thông báo, hóa đơn chưa thanh toán, đơn thuốc gần nhất.
- Đặt lịch khám: chọn khoa, dịch vụ, bác sĩ, ngày, khung giờ, lý do khám, xác nhận thông tin.
- Xem lịch khám: sắp tới, đã hoàn thành, đã hủy; cho phép hủy/dời lịch theo điều kiện mock.
- Xem đơn thuốc: danh sách đơn, chi tiết thuốc, liều dùng, lời dặn.
- Xem hóa đơn và thanh toán online mock.
- Hồ sơ sức khỏe cá nhân: thông tin cá nhân, BHYT, tiền sử, dị ứng, chỉ số sức khỏe theo thời gian.
- Thông báo và nhắc lịch.
- Đánh giá bác sĩ/dịch vụ sau khi hoàn tất khám.

Yêu cầu UX:
- Mobile-first cho bệnh nhân, vẫn đẹp trên desktop.
- Đặt lịch phải ít bước, dễ hiểu, có xác nhận cuối.
- Không hiển thị thuật ngữ kỹ thuật quá khó nếu không cần.
- Đơn thuốc và hóa đơn phải dễ đọc.
- Có trạng thái trống cho bệnh nhân mới.

Quy tắc nghiệp vụ:
- Bệnh nhân chỉ xem dữ liệu của chính mình.
- Hủy/dời lịch phải nhập lý do nếu lịch đã xác nhận.
- Thanh toán thất bại cho phép thử lại.
```

## 10. Reports, Settings và Audit Prompt

```text
Xây dựng các màn hình Báo cáo, Cấu hình hệ thống và Nhật ký hoạt động cho Admin HMS bằng [STACK].

Màn hình Báo cáo:
- Doanh thu theo ngày/tháng/khoa/bác sĩ.
- Lượt khám theo khoa và bác sĩ.
- Tồn kho thuốc, thuốc sắp hết hạn, thuốc tiêu thụ nhiều.
- Hóa đơn quá hạn/chưa thanh toán.
- Export PDF/Excel mock.

Màn hình Cấu hình:
- Giờ làm việc bệnh viện.
- Khung giờ khám, thời lượng khám trung bình, số slot tối đa.
- Ngưỡng tồn kho tối thiểu.
- Template thông báo.
- Cấu hình phương thức thanh toán.

Màn hình Nhật ký hoạt động:
- Ai thao tác, thời gian, module, hành động, đối tượng, kết quả.
- Filter theo người dùng, module, thời gian, mức độ.

Yêu cầu:
- Không hard-code các cấu hình nghiệp vụ vào UI khác.
- Audit log dễ tra cứu và không hiển thị dữ liệu nhạy cảm quá chi tiết.
- Cấu hình nguy hiểm cần confirm trước khi lưu.
```

## 11. Responsive và QA Prompt

```text
Rà soát và hoàn thiện UI HMS đã tạo bằng [STACK].

Kiểm tra:
- Không có overlap, clipping hoặc layout shift ở desktop, tablet, mobile.
- Bảng lớn trên mobile có giải pháp phù hợp: horizontal scroll, card view hoặc column priority.
- Các form có validation rõ ràng.
- Tất cả màn hình chính có loading, empty, error, permission denied.
- Action nguy hiểm có confirm dialog.
- Status không chỉ dựa vào màu, phải có text/icon.
- Navigation theo vai trò không lộ module sai quyền.
- Mock data nhất quán giữa các màn hình.
- Date/time hiển thị theo ngữ cảnh Việt Nam.
- Tiền tệ định dạng VND.

Hãy sửa các vấn đề UI/UX phát hiện được và liệt kê ngắn gọn thay đổi.
```

## Thứ tự dùng khuyến nghị

1. `00. Master Prompt`
2. `01. Design System Prompt`
3. `02. Auth và Role Routing Prompt`
4. `03. Admin Dashboard Prompt`
5. `04. Admin Quản lý Lịch khám Prompt`
6. `08. Bác sĩ Workspace Prompt`
7. `09. Bệnh nhân Portal Prompt`
8. `06. Admin Quản lý Thuốc và Kho Prompt`
9. `07. Admin Quản lý Hóa đơn và Thanh toán Prompt`
10. `05. Admin Quản lý Bác sĩ, Bệnh nhân, Khoa và Dịch vụ Prompt`
11. `10. Reports, Settings và Audit Prompt`
12. `11. Responsive và QA Prompt`

