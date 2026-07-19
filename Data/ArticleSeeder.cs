using System;
using System.Collections.Generic;
using System.Linq;
using QuanLyBenhVien.Models;

namespace QuanLyBenhVien.Data
{
    /// <summary>
    /// Seeds the public website's news and health notices. Kept separate from
    /// <see cref="DbSeeder"/> so the public content can evolve on its own.
    /// </summary>
    public static class ArticleSeeder
    {
        // Paragraph separator built without escape sequences so the article
        // bodies below stay readable as plain prose.
        private static readonly string Gap = new string((char)10, 2);

        private static string Body(params string[] paragraphs) => string.Join(Gap, paragraphs);

        /// <summary>Idempotent: only inserts titles that are not already stored.</summary>
        public static void Seed(ApplicationDbContext context)
        {
            var seed = new List<Article>
            {
                new Article
                {
                    TieuDe = "MediFlow triển khai đặt lịch khám trực tuyến 24/7",
                    TomTat = "Người bệnh có thể tự chọn chuyên khoa, bác sĩ và khung giờ khám ngay trên website, không cần gọi tổng đài.",
                    ChuyenMuc = "Thông báo",
                    TacGia = "Phòng Công tác xã hội",
                    NoiBat = true,
                    NgayDang = DateTime.Now.AddDays(-3),
                    NoiDung = Body(
                        "Bệnh viện MediFlow chính thức đưa vào vận hành hệ thống đặt lịch khám trực tuyến, phục vụ 24/7.",
                        "Người bệnh đăng ký tài khoản, chọn chuyên khoa và bác sĩ mong muốn, hệ thống sẽ hiển thị các khung giờ còn trống theo lịch làm việc thực tế của bác sĩ. Sau khi đặt lịch, người bệnh nhận thông báo xác nhận ngay trên hệ thống và có thể theo dõi trạng thái trong mục Lịch khám của tôi.",
                        "Người bệnh vui lòng có mặt trước giờ hẹn 15 phút để hoàn tất thủ tục tiếp đón và đo sinh hiệu.")
                },
                new Article
                {
                    TieuDe = "Khuyến cáo phòng bệnh hô hấp khi giao mùa",
                    TomTat = "Số ca viêm đường hô hấp trên tăng trong giai đoạn chuyển mùa. Bác sĩ khuyến cáo các biện pháp dự phòng đơn giản nhưng hiệu quả.",
                    ChuyenMuc = "Sức khỏe",
                    TacGia = "Khoa Nội tổng quát",
                    NoiBat = true,
                    NgayDang = DateTime.Now.AddDays(-7),
                    NoiDung = Body(
                        "Thời tiết chuyển mùa là điều kiện thuận lợi cho các bệnh lý đường hô hấp phát triển, đặc biệt ở trẻ nhỏ và người cao tuổi.",
                        "Để phòng bệnh, người dân nên giữ ấm vùng cổ ngực khi ra ngoài trời lạnh, rửa tay thường xuyên bằng xà phòng, đeo khẩu trang nơi đông người, uống đủ nước và bổ sung rau xanh, trái cây giàu vitamin C.",
                        "Khi có biểu hiện sốt cao liên tục trên hai ngày, khó thở, đau ngực hoặc ho ra máu, người bệnh cần đến cơ sở y tế để được thăm khám kịp thời, không tự ý dùng kháng sinh khi chưa có chỉ định của bác sĩ.")
                },
                new Article
                {
                    TieuDe = "Tầm soát tim mạch miễn phí cho người trên 40 tuổi",
                    TomTat = "Chương trình khám sàng lọc huyết áp, mỡ máu và điện tim dành cho người dân từ 40 tuổi, diễn ra trong tháng này.",
                    ChuyenMuc = "Tin tức",
                    TacGia = "Khoa Tim mạch",
                    NgayDang = DateTime.Now.AddDays(-12),
                    NoiDung = Body(
                        "Nhằm phát hiện sớm các yếu tố nguy cơ tim mạch, MediFlow tổ chức chương trình tầm soát miễn phí dành cho người dân từ 40 tuổi trở lên.",
                        "Nội dung tầm soát bao gồm đo huyết áp, đo chỉ số khối cơ thể, xét nghiệm mỡ máu cơ bản và đo điện tim. Kết quả được bác sĩ chuyên khoa Tim mạch đọc và tư vấn trực tiếp.",
                        "Người dân đăng ký tại quầy tiếp đón hoặc qua tổng đài 1900 6868. Số lượng có hạn theo từng ngày.")
                },
                new Article
                {
                    TieuDe = "Hướng dẫn chuẩn bị trước khi xét nghiệm máu",
                    TomTat = "Nhịn ăn bao lâu, có được uống nước không, thuốc đang dùng có ảnh hưởng kết quả? Những lưu ý giúp kết quả xét nghiệm chính xác.",
                    ChuyenMuc = "Sức khỏe",
                    TacGia = "Khoa Xét nghiệm",
                    NgayDang = DateTime.Now.AddDays(-18),
                    NoiDung = Body(
                        "Kết quả xét nghiệm máu có thể sai lệch nếu người bệnh không chuẩn bị đúng cách.",
                        "Với các xét nghiệm đường huyết và mỡ máu, người bệnh cần nhịn ăn từ 8 đến 12 giờ trước khi lấy máu. Trong thời gian nhịn ăn vẫn có thể uống nước lọc, nhưng không dùng cà phê, nước ngọt, sữa hay rượu bia.",
                        "Người bệnh đang dùng thuốc điều trị cần thông báo cho bác sĩ, đặc biệt là thuốc corticoid, thuốc tránh thai và vitamin liều cao, vì các thuốc này có thể làm thay đổi kết quả. Nên lấy máu vào buổi sáng và nghỉ ngơi 10 phút trước khi lấy mẫu.")
                },
                new Article
                {
                    TieuDe = "Điều chỉnh giờ làm việc phòng khám theo yêu cầu",
                    TomTat = "Phòng khám theo yêu cầu tiếp nhận người bệnh từ 08:00 đến 18:00 các ngày trong tuần, kể cả thứ Bảy.",
                    ChuyenMuc = "Thông báo",
                    TacGia = "Phòng Kế hoạch tổng hợp",
                    NgayDang = DateTime.Now.AddDays(-25),
                    NoiDung = Body(
                        "Để đáp ứng nhu cầu khám chữa bệnh ngày càng tăng, bệnh viện điều chỉnh khung giờ tiếp nhận của phòng khám theo yêu cầu.",
                        "Thời gian tiếp nhận mới là 08:00 đến 18:00, từ thứ Hai đến thứ Bảy. Khu vực cấp cứu vẫn hoạt động liên tục 24/7.",
                        "Người bệnh đã đặt lịch trước đó không bị ảnh hưởng. Bộ phận chăm sóc khách hàng sẽ liên hệ với các trường hợp cần dời giờ.")
                },
                new Article
                {
                    TieuDe = "MediFlow tuyển dụng điều dưỡng và kỹ thuật viên xét nghiệm",
                    TomTat = "Bệnh viện tuyển dụng nhiều vị trí điều dưỡng, kỹ thuật viên xét nghiệm và nhân viên tiếp đón trong quý này.",
                    ChuyenMuc = "Tuyển dụng",
                    TacGia = "Phòng Tổ chức cán bộ",
                    NgayDang = DateTime.Now.AddDays(-30),
                    NoiDung = Body(
                        "MediFlow cần tuyển các vị trí: Điều dưỡng đa khoa (10 người), Kỹ thuật viên xét nghiệm (4 người), Nhân viên tiếp đón (3 người).",
                        "Yêu cầu chung: tốt nghiệp đúng chuyên ngành, có chứng chỉ hành nghề còn hiệu lực đối với vị trí chuyên môn, giao tiếp tốt và có tinh thần phục vụ người bệnh.",
                        "Hồ sơ gửi về địa chỉ email support@mediflow.vn hoặc nộp trực tiếp tại Phòng Tổ chức cán bộ trong giờ hành chính.")
                }
            };

            var existing = context.Articles.Select(a => a.TieuDe).ToHashSet();
            var missing = seed.Where(a => !existing.Contains(a.TieuDe)).ToList();
            if (missing.Count == 0) return;

            context.Articles.AddRange(missing);
            context.SaveChanges();
        }
    }
}
