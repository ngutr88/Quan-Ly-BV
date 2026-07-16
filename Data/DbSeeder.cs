using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Models;
using QuanLyBenhVien.Helpers;

namespace QuanLyBenhVien.Data
{
    public static class DbSeeder
    {
        public static void Seed(ApplicationDbContext context)
        {
            context.Database.Migrate();

            // Force re-seeding if the database exists but doesn't have the new seed data
            if (context.Invoices.Any() && !context.Invoices.Any(i => i.MaGiaoDich == "MM987654321"))
            {
                context.InvoiceDetails.RemoveRange(context.InvoiceDetails);
                context.Invoices.RemoveRange(context.Invoices);
                context.PrescriptionDetails.RemoveRange(context.PrescriptionDetails);
                context.Prescriptions.RemoveRange(context.Prescriptions);
                context.ExaminationRecords.RemoveRange(context.ExaminationRecords);
                context.Appointments.RemoveRange(context.Appointments);
                context.Notifications.RemoveRange(context.Notifications);
                context.SaveChanges();
            }

            // 1. Seed Departments
            if (!context.Departments.Any())
            {
                context.Departments.AddRange(
                    new Department { TenKhoa = "Khoa Nội tổng quát", MoTa = "Chẩn đoán và điều trị bệnh nội khoa người lớn", ViTri = "Tầng 1 - Tòa nhà A" },
                    new Department { TenKhoa = "Khoa Tim mạch", MoTa = "Chẩn đoán và điều trị các bệnh lý tim mạch chuyên sâu", ViTri = "Tầng 2 - Tòa nhà B" },
                    new Department { TenKhoa = "Khoa Nhi", MoTa = "Chăm sóc sức khỏe toàn diện cho trẻ sơ sinh và trẻ nhỏ", ViTri = "Tầng 1 - Tòa nhà C" },
                    new Department { TenKhoa = "Khoa Tai Mũi Họng", MoTa = "Khám và điều trị các bệnh lý liên quan đến Tai, Mũi và Họng", ViTri = "Tầng 3 - Tòa nhà A" },
                    new Department { TenKhoa = "Khoa Ngoại tổng hợp", MoTa = "Phẫu thuật và điều trị ngoại khoa các bệnh lý cần can thiệp", ViTri = "Tầng 2 - Tòa nhà A" },
                    new Department { TenKhoa = "Khoa Sản phụ khoa", MoTa = "Chăm sóc sức khỏe sinh sản, thai sản và phụ khoa", ViTri = "Tầng 3 - Tòa nhà C" },
                    new Department { TenKhoa = "Khoa Da liễu", MoTa = "Chẩn đoán và điều trị các bệnh lý về da, tóc và móng", ViTri = "Tầng 4 - Tòa nhà A" },
                    new Department { TenKhoa = "Khoa Thần kinh", MoTa = "Khám và điều trị bệnh lý hệ thần kinh trung ương và ngoại vi", ViTri = "Tầng 4 - Tòa nhà B" }
                );
                context.SaveChanges();
            }

            // 2. Seed Services
            if (!context.Services.Any())
            {
                var general = context.Departments.First(d => d.TenKhoa == "Khoa Nội tổng quát");
                var cardio = context.Departments.First(d => d.TenKhoa == "Khoa Tim mạch");
                var pediatrics = context.Departments.First(d => d.TenKhoa == "Khoa Nhi");
                var ent = context.Departments.First(d => d.TenKhoa == "Khoa Tai Mũi Họng");

                context.Services.AddRange(
                    new Service { KhoaId = general.Id, TenDichVu = "Khám Nội tổng quát", Gia = 150000 },
                    new Service { KhoaId = general.Id, TenDichVu = "Khám sức khỏe tổng quát định kỳ", Gia = 300000 },
                    new Service { KhoaId = cardio.Id, TenDichVu = "Khám Tim mạch thường", Gia = 150000 },
                    new Service { KhoaId = cardio.Id, TenDichVu = "Điện tâm đồ (ECG)", Gia = 200000 },
                    new Service { KhoaId = cardio.Id, TenDichVu = "Siêu âm tim màu", Gia = 350000 },
                    new Service { KhoaId = pediatrics.Id, TenDichVu = "Khám Nhi tổng quát", Gia = 120000 },
                    new Service { KhoaId = ent.Id, TenDichVu = "Khám Tai Mũi Họng", Gia = 120000 },
                    new Service { KhoaId = ent.Id, TenDichVu = "Nội soi Tai Mũi Họng", Gia = 180000 }
                );
                context.SaveChanges();
            }

            // 3. Seed Users & Profiles (Admin, Doctor, Patient)
            if (!context.Users.Any())
            {
                // Admin User
                var adminUser = new User
                {
                    HoTen = "Quản trị viên Hệ thống",
                    Email = "admin@hms.com",
                    Sdt = "0987654321",
                    MatKhauHash = HashHelper.HashPassword("Admin@123"),
                    VaiTro = "Admin",
                    TrangThai = "Active"
                };
                context.Users.Add(adminUser);

                // Doctor 1 (Tim mach)
                var doctorCardio = new User
                {
                    HoTen = "BS. Nguyễn Văn Trung",
                    Email = "doctor@hms.com",
                    Sdt = "0912345678",
                    MatKhauHash = HashHelper.HashPassword("Doctor@123"),
                    VaiTro = "Doctor",
                    TrangThai = "Active"
                };
                context.Users.Add(doctorCardio);

                // Doctor 2 (Nhi)
                var doctorPediatrics = new User
                {
                    HoTen = "BS. Lê Thị Mai",
                    Email = "nhi@hms.com",
                    Sdt = "0977665544",
                    MatKhauHash = HashHelper.HashPassword("Doctor@123"),
                    VaiTro = "Doctor",
                    TrangThai = "Active"
                };
                context.Users.Add(doctorPediatrics);

                // Doctor 3 (Noi tong quat)
                var doctorGeneral = new User
                {
                    HoTen = "BS. Phạm Đức Hùng",
                    Email = "noitq@hms.com",
                    Sdt = "0965432100",
                    MatKhauHash = HashHelper.HashPassword("Doctor@123"),
                    VaiTro = "Doctor",
                    TrangThai = "Active"
                };
                context.Users.Add(doctorGeneral);

                // Doctor 4 (Tai Mui Hong)
                var doctorEnt = new User
                {
                    HoTen = "BS. Vũ Thị Hương",
                    Email = "tamh@hms.com",
                    Sdt = "0933111222",
                    MatKhauHash = HashHelper.HashPassword("Doctor@123"),
                    VaiTro = "Doctor",
                    TrangThai = "Active"
                };
                context.Users.Add(doctorEnt);

                // Doctor 5 (Ngoai tong hop)
                var doctorSurgery = new User
                {
                    HoTen = "BS. Trần Minh Khoa",
                    Email = "ngoai@hms.com",
                    Sdt = "0944333555",
                    MatKhauHash = HashHelper.HashPassword("Doctor@123"),
                    VaiTro = "Doctor",
                    TrangThai = "Active"
                };
                context.Users.Add(doctorSurgery);

                // Doctor 6 (Da lieu)
                var doctorDerm = new User
                {
                    HoTen = "BS. Nguyễn Thị Lan Anh",
                    Email = "dalieu@hms.com",
                    Sdt = "0911777888",
                    MatKhauHash = HashHelper.HashPassword("Doctor@123"),
                    VaiTro = "Doctor",
                    TrangThai = "Active"
                };
                context.Users.Add(doctorDerm);

                // Doctor 7 (San phu khoa)
                var doctorObGyn = new User
                {
                    HoTen = "BS. Hoàng Thị Thu Hà",
                    Email = "sanpk@hms.com",
                    Sdt = "0955111222",
                    MatKhauHash = HashHelper.HashPassword("Doctor@123"),
                    VaiTro = "Doctor",
                    TrangThai = "Active"
                };
                context.Users.Add(doctorObGyn);

                // Doctor 8 (Than kinh)
                var doctorNeuro = new User
                {
                    HoTen = "BS. Đỗ Quang Minh",
                    Email = "thankinh@hms.com",
                    Sdt = "0966222333",
                    MatKhauHash = HashHelper.HashPassword("Doctor@123"),
                    VaiTro = "Doctor",
                    TrangThai = "Active"
                };
                context.Users.Add(doctorNeuro);

                // Doctor 9 (Tim mach 2)
                var doctorCardio2 = new User
                {
                    HoTen = "BS. Trần Quốc Anh",
                    Email = "timbach2@hms.com",
                    Sdt = "0977333444",
                    MatKhauHash = HashHelper.HashPassword("Doctor@123"),
                    VaiTro = "Doctor",
                    TrangThai = "Active"
                };
                context.Users.Add(doctorCardio2);

                // Doctor 10 (Nhi 2)
                var doctorPediatrics2 = new User
                {
                    HoTen = "BS. Phan Thị Ngọc Diệp",
                    Email = "nhi2@hms.com",
                    Sdt = "0988444555",
                    MatKhauHash = HashHelper.HashPassword("Doctor@123"),
                    VaiTro = "Doctor",
                    TrangThai = "Active"
                };
                context.Users.Add(doctorPediatrics2);

                // Doctor 11 (Noi tong quat 2)
                var doctorGeneral2 = new User
                {
                    HoTen = "BS. Võ Hoàng Nam",
                    Email = "noitq2@hms.com",
                    Sdt = "0911555666",
                    MatKhauHash = HashHelper.HashPassword("Doctor@123"),
                    VaiTro = "Doctor",
                    TrangThai = "Active"
                };
                context.Users.Add(doctorGeneral2);

                // Doctor 12 (Mat)
                var doctorEye = new User
                {
                    HoTen = "BS. Lý Thị Bích Ngọc",
                    Email = "mat@hms.com",
                    Sdt = "0922666777",
                    MatKhauHash = HashHelper.HashPassword("Doctor@123"),
                    VaiTro = "Doctor",
                    TrangThai = "Active"
                };
                context.Users.Add(doctorEye);

                // Doctor 13 (Chan thuong Chinh hinh)
                var doctorOrtho = new User
                {
                    HoTen = "BS. Nguyễn Đình Toàn",
                    Email = "chinhhinh@hms.com",
                    Sdt = "0933777888",
                    MatKhauHash = HashHelper.HashPassword("Doctor@123"),
                    VaiTro = "Doctor",
                    TrangThai = "Active"
                };
                context.Users.Add(doctorOrtho);

                // Doctor 14 (Hoi suc cap cuu)
                var doctorER = new User
                {
                    HoTen = "BS. Huỳnh Văn Đạt",
                    Email = "hoitroc@hms.com",
                    Sdt = "0944888999",
                    MatKhauHash = HashHelper.HashPassword("Doctor@123"),
                    VaiTro = "Doctor",
                    TrangThai = "Active"
                };
                context.Users.Add(doctorER);

                // Doctor 15 (Ung buou)
                var doctorOncology = new User
                {
                    HoTen = "BS. Trần Thị Thanh Nhàn",
                    Email = "ungbuou@hms.com",
                    Sdt = "0955999000",
                    MatKhauHash = HashHelper.HashPassword("Doctor@123"),
                    VaiTro = "Doctor",
                    TrangThai = "Active"
                };
                context.Users.Add(doctorOncology);

                // Doctor 16 (Rang Ham Mat)
                var doctorDental = new User
                {
                    HoTen = "BS. Lê Đức Thịnh",
                    Email = "rhm@hms.com",
                    Sdt = "0966000111",
                    MatKhauHash = HashHelper.HashPassword("Doctor@123"),
                    VaiTro = "Doctor",
                    TrangThai = "Active"
                };
                context.Users.Add(doctorDental);

                // Doctor 17 (Tiet nieu)
                var doctorUrology = new User
                {
                    HoTen = "BS. Phạm Văn Khánh",
                    Email = "tietnieu@hms.com",
                    Sdt = "0977111222",
                    MatKhauHash = HashHelper.HashPassword("Doctor@123"),
                    VaiTro = "Doctor",
                    TrangThai = "Active"
                };
                context.Users.Add(doctorUrology);

                // Doctor 18 (Y hoc co truyen)
                var doctorTradMed = new User
                {
                    HoTen = "BS. Nguyễn Thị Phương Lan",
                    Email = "yhct@hms.com",
                    Sdt = "0988222333",
                    MatKhauHash = HashHelper.HashPassword("Doctor@123"),
                    VaiTro = "Doctor",
                    TrangThai = "Active"
                };
                context.Users.Add(doctorTradMed);

                // Patient User
                var patientUser = new User
                {
                    HoTen = "Trần Văn A",
                    Email = "patient@hms.com",
                    Sdt = "0901234567",
                    MatKhauHash = HashHelper.HashPassword("Patient@123"),
                    VaiTro = "Patient",
                    TrangThai = "Active"
                };
                context.Users.Add(patientUser);

                // Patient 2 (For scheduling history)
                var patientUser2 = new User
                {
                    HoTen = "Phạm Thị B",
                    Email = "patient2@hms.com",
                    Sdt = "0934567890",
                    MatKhauHash = HashHelper.HashPassword("Patient@123"),
                    VaiTro = "Patient",
                    TrangThai = "Active"
                };
                context.Users.Add(patientUser2);

                // Patient 3
                var patientUser3 = new User
                {
                    HoTen = "Lê Văn Cường",
                    Email = "patient3@hms.com",
                    Sdt = "0918765432",
                    MatKhauHash = HashHelper.HashPassword("Patient@123"),
                    VaiTro = "Patient",
                    TrangThai = "Active"
                };
                context.Users.Add(patientUser3);

                // Patient 4
                var patientUser4 = new User
                {
                    HoTen = "Nguyễn Thị Hồng",
                    Email = "patient4@hms.com",
                    Sdt = "0922334455",
                    MatKhauHash = HashHelper.HashPassword("Patient@123"),
                    VaiTro = "Patient",
                    TrangThai = "Active"
                };
                context.Users.Add(patientUser4);

                // Patient 5
                var patientUser5 = new User
                {
                    HoTen = "Đặng Văn Long",
                    Email = "patient5@hms.com",
                    Sdt = "0966778899",
                    MatKhauHash = HashHelper.HashPassword("Patient@123"),
                    VaiTro = "Patient",
                    TrangThai = "Active"
                };
                context.Users.Add(patientUser5);

                // Patient 6
                var patientUser6 = new User
                {
                    HoTen = "Trần Thị Minh Tú",
                    Email = "patient6@hms.com",
                    Sdt = "0911223344",
                    MatKhauHash = HashHelper.HashPassword("Patient@123"),
                    VaiTro = "Patient",
                    TrangThai = "Active"
                };
                context.Users.Add(patientUser6);

                // Patient 7
                var patientUser7 = new User
                {
                    HoTen = "Bùi Minh Đức",
                    Email = "patient7@hms.com",
                    Sdt = "0944556677",
                    MatKhauHash = HashHelper.HashPassword("Patient@123"),
                    VaiTro = "Patient",
                    TrangThai = "Active"
                };
                context.Users.Add(patientUser7);

                // Patient 8
                var patientUser8 = new User
                {
                    HoTen = "Võ Thị Thanh Tâm",
                    Email = "patient8@hms.com",
                    Sdt = "0900112233",
                    MatKhauHash = HashHelper.HashPassword("Patient@123"),
                    VaiTro = "Patient",
                    TrangThai = "Active"
                };
                context.Users.Add(patientUser8);

                // Patient 9
                var patientUser9 = new User
                {
                    HoTen = "Hồ Quang Hiếu",
                    Email = "patient9@hms.com",
                    Sdt = "0911334455",
                    MatKhauHash = HashHelper.HashPassword("Patient@123"),
                    VaiTro = "Patient",
                    TrangThai = "Active"
                };
                context.Users.Add(patientUser9);

                // Patient 10
                var patientUser10 = new User
                {
                    HoTen = "Lý Thị Mỹ Dung",
                    Email = "patient10@hms.com",
                    Sdt = "0922556677",
                    MatKhauHash = HashHelper.HashPassword("Patient@123"),
                    VaiTro = "Patient",
                    TrangThai = "Active"
                };
                context.Users.Add(patientUser10);

                // Patient 11
                var patientUser11 = new User
                {
                    HoTen = "Ngô Văn Thành",
                    Email = "patient11@hms.com",
                    Sdt = "0933778899",
                    MatKhauHash = HashHelper.HashPassword("Patient@123"),
                    VaiTro = "Patient",
                    TrangThai = "Active"
                };
                context.Users.Add(patientUser11);

                // Patient 12
                var patientUser12 = new User
                {
                    HoTen = "Đinh Thị Hạnh",
                    Email = "patient12@hms.com",
                    Sdt = "0944990011",
                    MatKhauHash = HashHelper.HashPassword("Patient@123"),
                    VaiTro = "Patient",
                    TrangThai = "Active"
                };
                context.Users.Add(patientUser12);

                // Patient 13
                var patientUser13 = new User
                {
                    HoTen = "Trương Minh Tuấn",
                    Email = "patient13@hms.com",
                    Sdt = "0955001122",
                    MatKhauHash = HashHelper.HashPassword("Patient@123"),
                    VaiTro = "Patient",
                    TrangThai = "Active"
                };
                context.Users.Add(patientUser13);

                // Patient 14
                var patientUser14 = new User
                {
                    HoTen = "Mai Thị Ngọc Ánh",
                    Email = "patient14@hms.com",
                    Sdt = "0966112233",
                    MatKhauHash = HashHelper.HashPassword("Patient@123"),
                    VaiTro = "Patient",
                    TrangThai = "Active"
                };
                context.Users.Add(patientUser14);

                // Patient 15
                var patientUser15 = new User
                {
                    HoTen = "Lâm Quốc Bảo",
                    Email = "patient15@hms.com",
                    Sdt = "0977223344",
                    MatKhauHash = HashHelper.HashPassword("Patient@123"),
                    VaiTro = "Patient",
                    TrangThai = "Active"
                };
                context.Users.Add(patientUser15);

                context.SaveChanges();

                // Create Profiles
                var cardioDept = context.Departments.First(d => d.TenKhoa == "Khoa Tim mạch");
                var pediatricsDept = context.Departments.First(d => d.TenKhoa == "Khoa Nhi");
                var generalDept = context.Departments.First(d => d.TenKhoa == "Khoa Nội tổng quát");
                var entDept = context.Departments.First(d => d.TenKhoa == "Khoa Tai Mũi Họng");
                var surgeryDept = context.Departments.First(d => d.TenKhoa == "Khoa Ngoại tổng hợp");
                var dermDept = context.Departments.First(d => d.TenKhoa == "Khoa Da liễu");
                var obgynDept = context.Departments.First(d => d.TenKhoa == "Khoa Sản phụ khoa");
                var neuroDept = context.Departments.First(d => d.TenKhoa == "Khoa Thần kinh");
                var eyeDept = context.Departments.First(d => d.TenKhoa == "Khoa Mắt");
                var orthoDept = context.Departments.First(d => d.TenKhoa == "Khoa Chấn thương Chỉnh hình");
                var erDept = context.Departments.First(d => d.TenKhoa == "Khoa Hồi sức cấp cứu");
                var oncologyDept = context.Departments.First(d => d.TenKhoa == "Khoa Ung bướu");
                var dentalDept = context.Departments.First(d => d.TenKhoa == "Khoa Răng Hàm Mặt");
                var urologyDept = context.Departments.First(d => d.TenKhoa == "Khoa Tiết niệu");
                var tradMedDept = context.Departments.First(d => d.TenKhoa == "Khoa Y học cổ truyền");

                var docProfile1 = new Doctor
                {
                    NguoiDungId = doctorCardio.Id,
                    KhoaId = cardioDept.Id,
                    ChuyenKhoa = "Tim mạch học can thiệp",
                    HocVi = "ThS.BS",
                    SoNamKinhNghiem = 10,
                    LichLamViec = "Ca sáng (08:00 - 12:00) các ngày trong tuần",
                    ChucVu = "Trưởng khoa"
                };
                var docProfile2 = new Doctor
                {
                    NguoiDungId = doctorPediatrics.Id,
                    KhoaId = pediatricsDept.Id,
                    ChuyenKhoa = "Dị ứng & Miễn dịch nhi khoa",
                    HocVi = "TS.BS",
                    SoNamKinhNghiem = 15,
                    LichLamViec = "Ca chiều (13:30 - 17:30) Thứ 2, 4, 6",
                    ChucVu = "Trưởng khoa"
                };
                var docProfile3 = new Doctor
                {
                    NguoiDungId = doctorGeneral.Id,
                    KhoaId = generalDept.Id,
                    ChuyenKhoa = "Nội tiết & Tiểu đường",
                    HocVi = "BS",
                    SoNamKinhNghiem = 7,
                    LichLamViec = "Ca sáng (08:00 - 12:00) Thứ 2, 3, 5, 6",
                    ChucVu = "Trưởng khoa"
                };
                var docProfile4 = new Doctor
                {
                    NguoiDungId = doctorEnt.Id,
                    KhoaId = entDept.Id,
                    ChuyenKhoa = "Nội soi & Phẫu thuật Tai Mũi Họng",
                    HocVi = "ThS.BS",
                    SoNamKinhNghiem = 12,
                    LichLamViec = "Ca sáng (08:00 - 12:00) & Ca chiều (13:30 - 17:00) Thứ 3, 5, 7",
                    ChucVu = "Trưởng khoa"
                };
                var docProfile5 = new Doctor
                {
                    NguoiDungId = doctorSurgery.Id,
                    KhoaId = surgeryDept.Id,
                    ChuyenKhoa = "Phẫu thuật nội soi ổ bụng",
                    HocVi = "PGS.TS.BS",
                    SoNamKinhNghiem = 20,
                    LichLamViec = "Ca sáng (07:30 - 11:30) các ngày Thứ 2 đến Thứ 6",
                    ChucVu = "Trưởng khoa"
                };
                var docProfile6 = new Doctor
                {
                    NguoiDungId = doctorDerm.Id,
                    KhoaId = dermDept.Id,
                    ChuyenKhoa = "Da liễu thẩm mỹ & Laser",
                    HocVi = "TS.BS",
                    SoNamKinhNghiem = 9,
                    LichLamViec = "Ca chiều (13:00 - 17:30) Thứ 2, 4, 6",
                    ChucVu = "Trưởng khoa"
                };
                var docProfile7 = new Doctor
                {
                    NguoiDungId = doctorObGyn.Id,
                    KhoaId = obgynDept.Id,
                    ChuyenKhoa = "Sản phụ khoa & Thai sản nguy cơ cao",
                    HocVi = "PGS.TS.BS",
                    SoNamKinhNghiem = 18,
                    LichLamViec = "Ca sáng (08:00 - 12:00) Thứ 2 đến Thứ 6",
                    ChucVu = "Trưởng khoa"
                };
                var docProfile8 = new Doctor
                {
                    NguoiDungId = doctorNeuro.Id,
                    KhoaId = neuroDept.Id,
                    ChuyenKhoa = "Thần kinh học & Đột quỵ",
                    HocVi = "TS.BS",
                    SoNamKinhNghiem = 14,
                    LichLamViec = "Ca sáng (08:00 - 12:00) & Ca chiều (13:30 - 17:00) Thứ 2, 4, 6",
                    ChucVu = "Trưởng khoa"
                };
                var docProfile9 = new Doctor
                {
                    NguoiDungId = doctorCardio2.Id,
                    KhoaId = cardioDept.Id,
                    ChuyenKhoa = "Siêu âm tim & Nhịp tim học",
                    HocVi = "ThS.BS",
                    SoNamKinhNghiem = 8,
                    LichLamViec = "Ca chiều (13:30 - 17:30) Thứ 2, 3, 5",
                    ChucVu = "Phó trưởng khoa"
                };
                var docProfile10 = new Doctor
                {
                    NguoiDungId = doctorPediatrics2.Id,
                    KhoaId = pediatricsDept.Id,
                    ChuyenKhoa = "Nhi khoa tổng quát & Dinh dưỡng nhi",
                    HocVi = "BS",
                    SoNamKinhNghiem = 5,
                    LichLamViec = "Ca sáng (08:00 - 12:00) Thứ 2, 3, 4, 5, 6",
                    ChucVu = "Phó trưởng khoa"
                };
                var docProfile11 = new Doctor
                {
                    NguoiDungId = doctorGeneral2.Id,
                    KhoaId = generalDept.Id,
                    ChuyenKhoa = "Hô hấp & Bệnh phổi mãn tính",
                    HocVi = "ThS.BS",
                    SoNamKinhNghiem = 11,
                    LichLamViec = "Ca chiều (13:30 - 17:30) Thứ 2, 4, 6",
                    ChucVu = "Phó trưởng khoa"
                };
                var docProfile12 = new Doctor
                {
                    NguoiDungId = doctorEye.Id,
                    KhoaId = eyeDept.Id,
                    ChuyenKhoa = "Phẫu thuật khúc xạ & Đục thủy tinh thể",
                    HocVi = "TS.BS",
                    SoNamKinhNghiem = 16,
                    LichLamViec = "Ca sáng (08:00 - 12:00) Thứ 2 đến Thứ 7",
                    ChucVu = "Trưởng khoa"
                };
                var docProfile13 = new Doctor
                {
                    NguoiDungId = doctorOrtho.Id,
                    KhoaId = orthoDept.Id,
                    ChuyenKhoa = "Phẫu thuật xương khớp & Thay khớp",
                    HocVi = "PGS.TS.BS",
                    SoNamKinhNghiem = 22,
                    LichLamViec = "Ca sáng (07:30 - 11:30) Thứ 2 đến Thứ 6",
                    ChucVu = "Trưởng khoa"
                };
                var docProfile14 = new Doctor
                {
                    NguoiDungId = doctorER.Id,
                    KhoaId = erDept.Id,
                    ChuyenKhoa = "Hồi sức tích cực & Cấp cứu đa chấn thương",
                    HocVi = "ThS.BS",
                    SoNamKinhNghiem = 13,
                    LichLamViec = "Trực 24h theo lịch phân ca",
                    ChucVu = "Trưởng khoa"
                };
                var docProfile15 = new Doctor
                {
                    NguoiDungId = doctorOncology.Id,
                    KhoaId = oncologyDept.Id,
                    ChuyenKhoa = "Ung thư nội khoa & Hóa trị liệu",
                    HocVi = "TS.BS",
                    SoNamKinhNghiem = 17,
                    LichLamViec = "Ca sáng (08:00 - 12:00) & Ca chiều (13:30 - 17:00) Thứ 2 đến Thứ 5",
                    ChucVu = "Trưởng khoa"
                };
                var docProfile16 = new Doctor
                {
                    NguoiDungId = doctorDental.Id,
                    KhoaId = dentalDept.Id,
                    ChuyenKhoa = "Phẫu thuật hàm mặt & Cấy ghép Implant",
                    HocVi = "ThS.BS",
                    SoNamKinhNghiem = 10,
                    LichLamViec = "Ca sáng (08:00 - 12:00) Thứ 2, 4, 6; Ca chiều (13:30 - 17:00) Thứ 3, 5",
                    ChucVu = "Trưởng khoa"
                };
                var docProfile17 = new Doctor
                {
                    NguoiDungId = doctorUrology.Id,
                    KhoaId = urologyDept.Id,
                    ChuyenKhoa = "Nội soi tiết niệu & Tán sỏi",
                    HocVi = "TS.BS",
                    SoNamKinhNghiem = 15,
                    LichLamViec = "Ca sáng (08:00 - 12:00) Thứ 2 đến Thứ 6",
                    ChucVu = "Trưởng khoa"
                };
                var docProfile18 = new Doctor
                {
                    NguoiDungId = doctorTradMed.Id,
                    KhoaId = tradMedDept.Id,
                    ChuyenKhoa = "Châm cứu & Vật lý trị liệu kết hợp",
                    HocVi = "ThS.BS",
                    SoNamKinhNghiem = 12,
                    LichLamViec = "Ca sáng (08:00 - 11:30) & Ca chiều (14:00 - 17:00) Thứ 2 đến Thứ 7",
                    ChucVu = "Trưởng khoa"
                };
                context.Doctors.AddRange(docProfile1, docProfile2, docProfile3, docProfile4, docProfile5, docProfile6,
                    docProfile7, docProfile8, docProfile9, docProfile10, docProfile11, docProfile12,
                    docProfile13, docProfile14, docProfile15, docProfile16, docProfile17, docProfile18);

                var patProfile1 = new Patient
                {
                    NguoiDungId = patientUser.Id,
                    NgaySinh = new DateTime(1990, 5, 12),
                    GioiTinh = "Nam",
                    NhomMau = "O+",
                    SoBHYT = "GD4797920102",
                    TienSuBenh = "Huyết áp thấp nhẹ",
                    DiUng = "Amoxicillin, Penicillin" // Allergy check target!
                };
                var patProfile2 = new Patient
                {
                    NguoiDungId = patientUser2.Id,
                    NgaySinh = new DateTime(1995, 10, 22),
                    GioiTinh = "Nữ",
                    NhomMau = "A+",
                    SoBHYT = "DN4791029102",
                    TienSuBenh = "Không",
                    DiUng = "Không"
                };
                var patProfile3 = new Patient
                {
                    NguoiDungId = patientUser3.Id,
                    NgaySinh = new DateTime(1985, 3, 8),
                    GioiTinh = "Nam",
                    NhomMau = "B+",
                    SoBHYT = "HN4810293847",
                    TienSuBenh = "Viêm gan B mãn tính",
                    DiUng = "Không"
                };
                var patProfile4 = new Patient
                {
                    NguoiDungId = patientUser4.Id,
                    NgaySinh = new DateTime(1978, 7, 15),
                    GioiTinh = "Nữ",
                    NhomMau = "AB+",
                    SoBHYT = "HN4810001122",
                    TienSuBenh = "Đái tháo đường type 2, tăng huyết áp",
                    DiUng = "Sulfa drugs"
                };
                var patProfile5 = new Patient
                {
                    NguoiDungId = patientUser5.Id,
                    NgaySinh = new DateTime(2003, 1, 20),
                    GioiTinh = "Nam",
                    NhomMau = "O-",
                    SoBHYT = "SG4820334455",
                    TienSuBenh = "Hen phế quản",
                    DiUng = "Aspirin"
                };
                var patProfile6 = new Patient
                {
                    NguoiDungId = patientUser6.Id,
                    NgaySinh = new DateTime(1968, 11, 5),
                    GioiTinh = "Nữ",
                    NhomMau = "B-",
                    SoBHYT = "DN4830556677",
                    TienSuBenh = "Viêm khớp dạng thấp, loãng xương",
                    DiUng = "Không"
                };
                var patProfile7 = new Patient
                {
                    NguoiDungId = patientUser7.Id,
                    NgaySinh = new DateTime(1992, 4, 30),
                    GioiTinh = "Nam",
                    NhomMau = "A-",
                    SoBHYT = "CT4840778899",
                    TienSuBenh = "Không có tiền sử bệnh đặc biệt",
                    DiUng = "Ibuprofen"
                };
                var patProfile8 = new Patient
                {
                    NguoiDungId = patientUser8.Id,
                    NgaySinh = new DateTime(1982, 8, 18),
                    GioiTinh = "Nữ",
                    NhomMau = "A+",
                    SoBHYT = "HN4850112233",
                    TienSuBenh = "Suy giáp, thiếu máu mãn tính",
                    DiUng = "Penicillin"
                };
                var patProfile9 = new Patient
                {
                    NguoiDungId = patientUser9.Id,
                    NgaySinh = new DateTime(1999, 2, 14),
                    GioiTinh = "Nam",
                    NhomMau = "O+",
                    SoBHYT = "SG4860334455",
                    TienSuBenh = "Viêm xoang mãn tính",
                    DiUng = "Không"
                };
                var patProfile10 = new Patient
                {
                    NguoiDungId = patientUser10.Id,
                    NgaySinh = new DateTime(1975, 12, 3),
                    GioiTinh = "Nữ",
                    NhomMau = "AB-",
                    SoBHYT = "DN4870556677",
                    TienSuBenh = "Đái tháo đường type 2, thoái hóa cột sống",
                    DiUng = "Metformin (buồn nôn)"
                };
                var patProfile11 = new Patient
                {
                    NguoiDungId = patientUser11.Id,
                    NgaySinh = new DateTime(2010, 6, 25),
                    GioiTinh = "Nam",
                    NhomMau = "B+",
                    SoBHYT = "HP4880778899",
                    TienSuBenh = "Viêm phế quản dị ứng tái phát",
                    DiUng = "Phấn hoa, lông thú"
                };
                var patProfile12 = new Patient
                {
                    NguoiDungId = patientUser12.Id,
                    NgaySinh = new DateTime(1988, 9, 7),
                    GioiTinh = "Nữ",
                    NhomMau = "O-",
                    SoBHYT = "CT4890990011",
                    TienSuBenh = "Loạn nhịp tim (đã đặt máy tạo nhịp)",
                    DiUng = "Không"
                };
                var patProfile13 = new Patient
                {
                    NguoiDungId = patientUser13.Id,
                    NgaySinh = new DateTime(1965, 4, 11),
                    GioiTinh = "Nam",
                    NhomMau = "A-",
                    SoBHYT = "BT4900001122",
                    TienSuBenh = "Tăng huyết áp, gout mãn tính, sỏi thận",
                    DiUng = "Allopurinol"
                };
                var patProfile14 = new Patient
                {
                    NguoiDungId = patientUser14.Id,
                    NgaySinh = new DateTime(2000, 11, 30),
                    GioiTinh = "Nữ",
                    NhomMau = "B-",
                    SoBHYT = "NA4910112233",
                    TienSuBenh = "Không có tiền sử đặc biệt",
                    DiUng = "Không"
                };
                var patProfile15 = new Patient
                {
                    NguoiDungId = patientUser15.Id,
                    NgaySinh = new DateTime(1955, 1, 2),
                    GioiTinh = "Nam",
                    NhomMau = "AB+",
                    SoBHYT = "AG4920223344",
                    TienSuBenh = "COPD giai đoạn II, suy tim độ II, đái tháo đường",
                    DiUng = "Aspirin, Codein"
                };
                context.Patients.AddRange(patProfile1, patProfile2, patProfile3, patProfile4, patProfile5, patProfile6, patProfile7,
                    patProfile8, patProfile9, patProfile10, patProfile11, patProfile12, patProfile13, patProfile14, patProfile15);

                context.SaveChanges();
            }
            // Auto-seed missing departments for existing DBs (non-destructive patch)
            var existingDeptNames = context.Departments.Select(d => d.TenKhoa).ToList();
            var newDepts = new List<(string TenKhoa, string MoTa, string ViTri)>
            {
                ("Khoa Nội tổng quát",  "Chẩn đoán và điều trị bệnh nội khoa người lớn",                    "Tầng 1 - Tòa nhà A"),
                ("Khoa Tim mạch",       "Chẩn đoán và điều trị các bệnh lý tim mạch chuyên sâu",            "Tầng 2 - Tòa nhà B"),
                ("Khoa Nhi",            "Chăm sóc sức khỏe toàn diện cho trẻ sơ sinh và trẻ nhỏ",          "Tầng 1 - Tòa nhà C"),
                ("Khoa Tai Mũi Họng",   "Khám và điều trị các bệnh lý liên quan đến Tai, Mũi và Họng",     "Tầng 3 - Tòa nhà A"),
                ("Khoa Ngoại tổng hợp", "Phẫu thuật và điều trị ngoại khoa các bệnh lý cần can thiệp",     "Tầng 2 - Tòa nhà A"),
                ("Khoa Sản phụ khoa",   "Chăm sóc sức khỏe sinh sản, thai sản và phụ khoa",                "Tầng 3 - Tòa nhà C"),
                ("Khoa Da liễu",        "Chẩn đoán và điều trị các bệnh lý về da, tóc và móng",            "Tầng 4 - Tòa nhà A"),
                ("Khoa Thần kinh",      "Khám và điều trị bệnh lý hệ thần kinh trung ương và ngoại vi",    "Tầng 4 - Tòa nhà B"),
            };
            bool deptAdded = false;
            foreach (var (TenKhoa, MoTa, ViTri) in newDepts)
            {
                if (!existingDeptNames.Contains(TenKhoa))
                {
                    context.Departments.Add(new Department { TenKhoa = TenKhoa, MoTa = MoTa, ViTri = ViTri });
                    deptAdded = true;
                }
            }
            if (deptAdded) context.SaveChanges();

            // Auto-seed missing medicines for existing DBs (non-destructive patch)
            var existingMedNames = context.Medicines.Select(m => m.TenThuoc).ToList();
            var newMeds = new List<(string TenThuoc, string HoatChat, string DonViTinh, decimal Gia, int TonKho, int NguongToiThieu, int HanSuDungNgay, string SoLo)>
            {
                ("Paracetamol 500mg",                  "Paracetamol",                    "Viên",    2000,  500, 100, 30,  "LOT-PARA-001"),
                ("Amoxicillin 500mg (Kháng sinh)",     "Amoxicillin",                    "Viên",    5000,  300,  50, 180, "LOT-AMOX-001"),
                ("Decolgen Forte",                     "Acetaminophen, Phenylephrine",   "Viên",    3000,  150,  80,   5, "LOT-DECO-001"),
                ("Amlodipine 5mg",                     "Amlodipine Besylate",            "Viên",    4500,  400,  80, 540, "LOT-AMLO-001"),
                ("Metformin 500mg",                    "Metformin Hydrochloride",        "Viên",    3500,  350,  70, 365, "LOT-METF-001"),
                ("Omeprazole 20mg",                    "Omeprazole",                     "Viên",    6000,  280,  60, 400, "LOT-OMEP-002"),
                ("Cetirizine 10mg (Chống dị ứng)",    "Cetirizine Hydrochloride",       "Viên",    3000,  200,  50, 450, "LOT-CETI-001"),
                ("Azithromycin 500mg",                 "Azithromycin",                   "Viên",   18000,  180,  40, 300, "LOT-AZIT-001"),
                ("Vitamin C 1000mg Effervescent",      "Ascorbic Acid",                  "Viên sủi", 8000, 220,  60, 270, "LOT-VITC-001"),
                ("Ibuprofen 400mg",                    "Ibuprofen",                      "Viên",    4000,   90, 100, 200, "LOT-IBUP-001"),
                ("Atorvastatin 20mg",                  "Atorvastatin Calcium",           "Viên",   12000,  160,  40, 600, "LOT-ATOR-001"),
                ("Salbutamol 2.5mg/2.5ml (Khí dung)", "Salbutamol Sulfate",             "Ống",    25000,  120,  30, 365, "LOT-SALB-001"),
            };
            bool medAdded = false;
            foreach (var m in newMeds)
            {
                if (!existingMedNames.Contains(m.TenThuoc))
                {
                    var newMed = new Medicine
                    {
                        TenThuoc = m.TenThuoc,
                        HoatChat = m.HoatChat,
                        DonViTinh = m.DonViTinh,
                        Gia = m.Gia,
                        TonKho = m.TonKho,
                        NguongToiThieu = m.NguongToiThieu
                    };
                    context.Medicines.Add(newMed);
                    context.SaveChanges();
                    context.MedicineBatches.Add(new MedicineBatch
                    {
                        ThuocId = newMed.Id,
                        SoLo = m.SoLo,
                        HanSuDung = DateTime.Now.AddDays(m.HanSuDungNgay),
                        SoLuongNhap = m.TonKho,
                        SoLuongTon = m.TonKho
                    });
                    medAdded = true;
                }
            }
            if (medAdded) context.SaveChanges();

            // Auto-seed missing patients for existing DBs (non-destructive patch)
            var existingPatientEmails = context.Users.Where(u => u.VaiTro == "Patient").Select(u => u.Email).ToList();
            var newPatientData = new List<(string HoTen, string Email, string Sdt, DateTime NgaySinh, string GioiTinh, string NhomMau, string SoBHYT, string TienSuBenh, string DiUng)>
            {
                ("Nguyễn Thị Hồng",   "patient4@hms.com", "0922334455", new DateTime(1978, 7, 15),  "Nữ",  "AB+", "HN4810001122", "Đái tháo đường type 2, tăng huyết áp",          "Sulfa drugs"),
                ("Đặng Văn Long",     "patient5@hms.com", "0966778899", new DateTime(2003, 1, 20),  "Nam", "O-",  "SG4820334455", "Hen phế quản",                                   "Aspirin"),
                ("Trần Thị Minh Tú",  "patient6@hms.com", "0911223344", new DateTime(1968, 11, 5),  "Nữ",  "B-",  "DN4830556677", "Viêm khớp dạng thấp, loãng xương",              "Không"),
                ("Bùi Minh Đức",      "patient7@hms.com", "0944556677", new DateTime(1992, 4, 30),  "Nam", "A-",  "CT4840778899", "Không có tiền sử bệnh đặc biệt",                "Ibuprofen"),
            };
            bool patAdded = false;
            foreach (var p in newPatientData)
            {
                if (!existingPatientEmails.Contains(p.Email))
                {
                    var newUser = new User
                    {
                        HoTen = p.HoTen,
                        Email = p.Email,
                        Sdt = p.Sdt,
                        MatKhauHash = HashHelper.HashPassword("Patient@123"),
                        VaiTro = "Patient",
                        TrangThai = "Active"
                    };
                    context.Users.Add(newUser);
                    context.SaveChanges();
                    context.Patients.Add(new Patient
                    {
                        NguoiDungId = newUser.Id,
                        NgaySinh = p.NgaySinh,
                        GioiTinh = p.GioiTinh,
                        NhomMau = p.NhomMau,
                        SoBHYT = p.SoBHYT,
                        TienSuBenh = p.TienSuBenh,
                        DiUng = p.DiUng
                    });
                    patAdded = true;
                }
            }
            if (patAdded) context.SaveChanges();

            // Auto-seed additional departments for existing DBs (non-destructive patch)
            var existingDeptNames2 = context.Departments.Select(d => d.TenKhoa).ToList();
            var extraDepts = new List<(string TenKhoa, string MoTa, string ViTri)>
            {
                ("Khoa Mắt",              "Khám và điều trị các bệnh lý về mắt, phẫu thuật nhãn khoa",                     "Tầng 5 - Tòa nhà A"),
                ("Khoa Chấn thương Chỉnh hình", "Điều trị gãy xương, trật khớp, phẫu thuật chỉnh hình",                   "Tầng 1 - Tòa nhà D"),
                ("Khoa Hồi sức cấp cứu",  "Cấp cứu và hồi sức tích cực cho bệnh nhân nặng",                               "Tầng trệt - Tòa nhà A"),
                ("Khoa Ung bướu",          "Chẩn đoán, điều trị và quản lý các bệnh lý ung thư",                           "Tầng 3 - Tòa nhà D"),
                ("Khoa Răng Hàm Mặt",      "Khám và điều trị các bệnh lý răng miệng, phẫu thuật hàm mặt",                 "Tầng 2 - Tòa nhà C"),
                ("Khoa Tiết niệu",         "Chẩn đoán và điều trị bệnh lý hệ tiết niệu và sinh dục nam",                   "Tầng 5 - Tòa nhà B"),
                ("Khoa Y học cổ truyền",   "Châm cứu, bấm huyệt, xoa bóp và điều trị bằng đông y kết hợp tây y",           "Tầng 2 - Tòa nhà D"),
            };
            bool extraDeptAdded = false;
            foreach (var (TenKhoa, MoTa, ViTri) in extraDepts)
            {
                if (!existingDeptNames2.Contains(TenKhoa))
                {
                    context.Departments.Add(new Department { TenKhoa = TenKhoa, MoTa = MoTa, ViTri = ViTri });
                    extraDeptAdded = true;
                }
            }
            if (extraDeptAdded) context.SaveChanges();

            // Auto-seed services for new departments (non-destructive patch)
            var existingServiceNames = context.Services.Select(s => s.TenDichVu).ToList();
            var servicesToAdd = new List<(string KhoaTen, string TenDichVu, decimal Gia)>
            {
                // Khoa Sản phụ khoa
                ("Khoa Sản phụ khoa",       "Khám thai định kỳ",                         200000),
                ("Khoa Sản phụ khoa",       "Siêu âm thai 4D",                           500000),
                ("Khoa Sản phụ khoa",       "Khám phụ khoa tổng quát",                   180000),
                // Khoa Da liễu
                ("Khoa Da liễu",            "Khám Da liễu thường",                       120000),
                ("Khoa Da liễu",            "Điều trị Laser da liễu",                    800000),
                // Khoa Thần kinh
                ("Khoa Thần kinh",          "Khám Thần kinh tổng quát",                  150000),
                ("Khoa Thần kinh",          "Điện não đồ (EEG)",                         250000),
                // Khoa Ngoại tổng hợp
                ("Khoa Ngoại tổng hợp",     "Khám Ngoại khoa tổng quát",                 150000),
                ("Khoa Ngoại tổng hợp",     "Siêu âm bụng tổng quát",                   200000),
                // Khoa Mắt
                ("Khoa Mắt",                "Khám Mắt tổng quát",                        120000),
                ("Khoa Mắt",                "Đo thị lực và khúc xạ",                     150000),
                ("Khoa Mắt",                "Phẫu thuật Phaco đục thủy tinh thể",       8000000),
                // Khoa Chấn thương Chỉnh hình
                ("Khoa Chấn thương Chỉnh hình", "Khám Chấn thương Chỉnh hình",           150000),
                ("Khoa Chấn thương Chỉnh hình", "Chụp X-quang xương khớp",               180000),
                ("Khoa Chấn thương Chỉnh hình", "Phẫu thuật nội soi khớp",              5000000),
                // Khoa Hồi sức cấp cứu
                ("Khoa Hồi sức cấp cứu",   "Khám cấp cứu",                               200000),
                // Khoa Ung bướu
                ("Khoa Ung bướu",           "Khám sàng lọc ung thư",                      300000),
                ("Khoa Ung bướu",           "Xét nghiệm marker ung thư",                  500000),
                // Khoa Răng Hàm Mặt
                ("Khoa Răng Hàm Mặt",       "Khám răng tổng quát",                        100000),
                ("Khoa Răng Hàm Mặt",       "Nhổ răng khôn",                               500000),
                ("Khoa Răng Hàm Mặt",       "Tẩy trắng răng",                            1500000),
                // Khoa Tiết niệu
                ("Khoa Tiết niệu",          "Khám Tiết niệu tổng quát",                   150000),
                ("Khoa Tiết niệu",          "Siêu âm hệ tiết niệu",                      250000),
                // Khoa Y học cổ truyền
                ("Khoa Y học cổ truyền",    "Khám Đông y",                                 100000),
                ("Khoa Y học cổ truyền",    "Châm cứu trị liệu (1 buổi)",                 200000),
                ("Khoa Y học cổ truyền",    "Xoa bóp bấm huyệt (1 buổi)",                 150000),
            };
            bool svcAdded = false;
            foreach (var (KhoaTen, TenDichVu, Gia) in servicesToAdd)
            {
                if (!existingServiceNames.Contains(TenDichVu))
                {
                    var dept = context.Departments.FirstOrDefault(d => d.TenKhoa == KhoaTen);
                    if (dept != null)
                    {
                        context.Services.Add(new Service { KhoaId = dept.Id, TenDichVu = TenDichVu, Gia = Gia });
                        svcAdded = true;
                    }
                }
            }
            if (svcAdded) context.SaveChanges();

            // Auto-seed additional doctors for existing DBs (non-destructive patch)
            var existingDoctorEmails = context.Users.Where(u => u.VaiTro == "Doctor").Select(u => u.Email).ToList();
            var extraDoctorData = new List<(string HoTen, string Email, string Sdt, string KhoaTen, string ChuyenKhoa, string HocVi, int SoNam, string LichLamViec)>
            {
                // Bác sĩ cho 2 khoa cũ chưa có bác sĩ
                ("BS. Hoàng Thị Thu Hà",    "sanpk@hms.com",     "0955111222", "Khoa Sản phụ khoa",     "Sản phụ khoa & Thai sản nguy cơ cao",       "PGS.TS.BS", 18, "Ca sáng (08:00 - 12:00) Thứ 2 đến Thứ 6"),
                ("BS. Đỗ Quang Minh",       "thankinh@hms.com",  "0966222333", "Khoa Thần kinh",        "Thần kinh học & Đột quỵ",                   "TS.BS",     14, "Ca sáng (08:00 - 12:00) & Ca chiều (13:30 - 17:00) Thứ 2, 4, 6"),
                // Bác sĩ thêm cho các khoa cũ đông bệnh nhân
                ("BS. Trần Quốc Anh",       "timbach2@hms.com",  "0977333444", "Khoa Tim mạch",         "Siêu âm tim & Nhịp tim học",                "ThS.BS",    8,  "Ca chiều (13:30 - 17:30) Thứ 2, 3, 5"),
                ("BS. Phan Thị Ngọc Diệp",  "nhi2@hms.com",      "0988444555", "Khoa Nhi",              "Nhi khoa tổng quát & Dinh dưỡng nhi",       "BS",        5,  "Ca sáng (08:00 - 12:00) Thứ 2, 3, 4, 5, 6"),
                ("BS. Võ Hoàng Nam",         "noitq2@hms.com",    "0911555666", "Khoa Nội tổng quát",    "Hô hấp & Bệnh phổi mãn tính",               "ThS.BS",    11, "Ca chiều (13:30 - 17:30) Thứ 2, 4, 6"),
                // Bác sĩ cho các khoa mới
                ("BS. Lý Thị Bích Ngọc",    "mat@hms.com",       "0922666777", "Khoa Mắt",              "Phẫu thuật khúc xạ & Đục thủy tinh thể",   "TS.BS",     16, "Ca sáng (08:00 - 12:00) Thứ 2 đến Thứ 7"),
                ("BS. Nguyễn Đình Toàn",     "chinhhinh@hms.com", "0933777888", "Khoa Chấn thương Chỉnh hình", "Phẫu thuật xương khớp & Thay khớp",   "PGS.TS.BS", 22, "Ca sáng (07:30 - 11:30) Thứ 2 đến Thứ 6"),
                ("BS. Huỳnh Văn Đạt",       "hoitroc@hms.com",   "0944888999", "Khoa Hồi sức cấp cứu", "Hồi sức tích cực & Cấp cứu đa chấn thương", "ThS.BS",    13, "Trực 24h theo lịch phân ca"),
                ("BS. Trần Thị Thanh Nhàn",  "ungbuou@hms.com",   "0955999000", "Khoa Ung bướu",         "Ung thư nội khoa & Hóa trị liệu",           "TS.BS",     17, "Ca sáng (08:00 - 12:00) & Ca chiều (13:30 - 17:00) Thứ 2 đến Thứ 5"),
                ("BS. Lê Đức Thịnh",         "rhm@hms.com",       "0966000111", "Khoa Răng Hàm Mặt",    "Phẫu thuật hàm mặt & Cấy ghép Implant",     "ThS.BS",    10, "Ca sáng (08:00 - 12:00) Thứ 2, 4, 6; Ca chiều (13:30 - 17:00) Thứ 3, 5"),
                ("BS. Phạm Văn Khánh",       "tietnieu@hms.com",  "0977111222", "Khoa Tiết niệu",       "Nội soi tiết niệu & Tán sỏi",               "TS.BS",     15, "Ca sáng (08:00 - 12:00) Thứ 2 đến Thứ 6"),
                ("BS. Nguyễn Thị Phương Lan", "yhct@hms.com",      "0988222333", "Khoa Y học cổ truyền", "Châm cứu & Vật lý trị liệu kết hợp",        "ThS.BS",    12, "Ca sáng (08:00 - 11:30) & Ca chiều (14:00 - 17:00) Thứ 2 đến Thứ 7"),
            };
            bool extraDocAdded = false;
            foreach (var d in extraDoctorData)
            {
                if (!existingDoctorEmails.Contains(d.Email))
                {
                    var newUser = new User
                    {
                        HoTen = d.HoTen,
                        Email = d.Email,
                        Sdt = d.Sdt,
                        MatKhauHash = HashHelper.HashPassword("Doctor@123"),
                        VaiTro = "Doctor",
                        TrangThai = "Active"
                    };
                    context.Users.Add(newUser);
                    context.SaveChanges();

                    var dept = context.Departments.FirstOrDefault(dp => dp.TenKhoa == d.KhoaTen);
                    if (dept != null)
                    {
                        var currentDocCount = context.Doctors.Count(dc => dc.KhoaId == dept.Id);
                        string chucVu = "Bác sĩ";
                        if (currentDocCount == 0) chucVu = "Trưởng khoa";
                        else if (currentDocCount == 1 || currentDocCount == 2) chucVu = "Phó trưởng khoa";

                        context.Doctors.Add(new Doctor
                        {
                            NguoiDungId = newUser.Id,
                            KhoaId = dept.Id,
                            ChuyenKhoa = d.ChuyenKhoa,
                            HocVi = d.HocVi,
                            SoNamKinhNghiem = d.SoNam,
                            LichLamViec = d.LichLamViec,
                            ChucVu = chucVu
                        });
                        extraDocAdded = true;
                    }
                }
            }
            if (extraDocAdded) context.SaveChanges();

            // Auto-seed additional patients for existing DBs (non-destructive patch)
            var existingPatEmails2 = context.Users.Where(u => u.VaiTro == "Patient").Select(u => u.Email).ToList();
            var extraPatientData = new List<(string HoTen, string Email, string Sdt, DateTime NgaySinh, string GioiTinh, string NhomMau, string SoBHYT, string TienSuBenh, string DiUng)>
            {
                ("Võ Thị Thanh Tâm",      "patient8@hms.com",  "0900112233", new DateTime(1982, 8, 18),  "Nữ",  "A+",  "HN4850112233", "Suy giáp, thiếu máu mãn tính",                    "Penicillin"),
                ("Hồ Quang Hiếu",         "patient9@hms.com",  "0911334455", new DateTime(1999, 2, 14),  "Nam", "O+",  "SG4860334455", "Viêm xoang mãn tính",                              "Không"),
                ("Lý Thị Mỹ Dung",        "patient10@hms.com", "0922556677", new DateTime(1975, 12, 3),  "Nữ",  "AB-", "DN4870556677", "Đái tháo đường type 2, thoái hóa cột sống",        "Metformin (buồn nôn)"),
                ("Ngô Văn Thành",         "patient11@hms.com", "0933778899", new DateTime(2010, 6, 25),  "Nam", "B+",  "HP4880778899", "Viêm phế quản dị ứng tái phát",                    "Phấn hoa, lông thú"),
                ("Đinh Thị Hạnh",         "patient12@hms.com", "0944990011", new DateTime(1988, 9, 7),   "Nữ",  "O-",  "CT4890990011", "Loạn nhịp tim (đã đặt máy tạo nhịp)",              "Không"),
                ("Trương Minh Tuấn",      "patient13@hms.com", "0955001122", new DateTime(1965, 4, 11),  "Nam", "A-",  "BT4900001122", "Tăng huyết áp, gout mãn tính, sỏi thận",           "Allopurinol"),
                ("Mai Thị Ngọc Ánh",      "patient14@hms.com", "0966112233", new DateTime(2000, 11, 30), "Nữ",  "B-",  "NA4910112233", "Không có tiền sử đặc biệt",                        "Không"),
                ("Lâm Quốc Bảo",          "patient15@hms.com", "0977223344", new DateTime(1955, 1, 2),   "Nam", "AB+", "AG4920223344", "COPD giai đoạn II, suy tim độ II, đái tháo đường", "Aspirin, Codein"),
            };
            bool extraPatAdded = false;
            foreach (var p in extraPatientData)
            {
                if (!existingPatEmails2.Contains(p.Email))
                {
                    var newUser = new User
                    {
                        HoTen = p.HoTen,
                        Email = p.Email,
                        Sdt = p.Sdt,
                        MatKhauHash = HashHelper.HashPassword("Patient@123"),
                        VaiTro = "Patient",
                        TrangThai = "Active"
                    };
                    context.Users.Add(newUser);
                    context.SaveChanges();
                    context.Patients.Add(new Patient
                    {
                        NguoiDungId = newUser.Id,
                        NgaySinh = p.NgaySinh,
                        GioiTinh = p.GioiTinh,
                        NhomMau = p.NhomMau,
                        SoBHYT = p.SoBHYT,
                        TienSuBenh = p.TienSuBenh,
                        DiUng = p.DiUng
                    });
                    extraPatAdded = true;
                }
            }
            if (extraPatAdded) context.SaveChanges();


            // Bulk-seed doctors: ensure each department has 15–20 doctors (non-destructive)
            {
                var allDepartments = context.Departments.ToList();
                if (allDepartments.Any())
                {
                    // Vietnamese name components
                    var hoArr = new[] { "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ", "Đặng", "Bùi", "Đỗ", "Hồ", "Ngô", "Dương", "Lý", "Tô", "Lương", "Mai", "Đinh" };
                    var demNam = new[] { "Văn", "Minh", "Đức", "Quang", "Hoàng", "Thanh", "Công", "Hữu", "Anh", "Quốc", "Đình", "Xuân", "Tuấn", "Trọng", "Bá" };
                    var demNu = new[] { "Thị", "Ngọc", "Bích", "Thu", "Phương", "Thanh", "Mỹ", "Kim", "Hồng", "Diệu", "Khánh" };
                    var tenNamArr = new[] { "Hùng", "Dũng", "Tuấn", "Khoa", "Long", "Nam", "Đạt", "Thắng", "Trung", "Phúc", "Tâm", "Huy", "Bảo", "Kiên", "Vinh", "Toàn", "Lâm", "An", "Thành", "Cường", "Phong", "Sơn", "Tài", "Nghĩa", "Trí" };
                    var tenNuArr = new[] { "Hà", "Mai", "Lan", "Hương", "Ngọc", "Thảo", "Linh", "Trang", "Yến", "Hạnh", "Vy", "Trinh", "Nhi", "Dung", "Uyên", "Oanh", "Thủy", "Chi", "Diệu", "Phượng", "Quyên", "Hiền", "Giang", "Châu", "Thy" };
                    var hocViArr = new[] { "BS", "BS", "BS", "ThS.BS", "ThS.BS", "ThS.BS", "TS.BS", "TS.BS", "PGS.TS.BS" };
                    var lichArr = new[] {
                        "Ca sáng (08:00 - 12:00) Thứ 2 đến Thứ 6",
                        "Ca chiều (13:30 - 17:30) Thứ 2 đến Thứ 6",
                        "Ca sáng (08:00 - 12:00) & Ca chiều (13:30 - 17:00) Thứ 2, 4, 6",
                        "Ca sáng (08:00 - 12:00) Thứ 2, 3, 5, 6",
                        "Ca chiều (13:30 - 17:30) Thứ 3, 5, 7",
                        "Ca sáng (07:30 - 11:30) các ngày trong tuần",
                        "Ca sáng (08:00 - 12:00) Thứ 2, 4, 6",
                        "Ca chiều (13:30 - 17:00) Thứ 2, 3, 4, 5",
                        "Ca sáng (08:00 - 12:00) & Ca chiều (14:00 - 17:00) Thứ 2 đến Thứ 7",
                    };

                    // Department-specific sub-specializations
                    var chuyenKhoaMap = new Dictionary<string, string[]>
                    {
                        { "Khoa Nội tổng quát", new[] { "Nội tiêu hóa", "Nội hô hấp", "Nội tiết & Đái tháo đường", "Nội thận", "Nội cơ xương khớp", "Nội tổng quát", "Dị ứng & Miễn dịch lâm sàng", "Nội tim mạch", "Bệnh nhiệt đới", "Nội gan mật", "Huyết học lâm sàng", "Lão khoa", "Nội thần kinh", "Nội phổi", "Y học gia đình", "Dinh dưỡng lâm sàng", "Nội khoa tổng hợp", "Chăm sóc giảm nhẹ", "Y học giấc ngủ", "Nội soi tiêu hóa" } },
                        { "Khoa Tim mạch", new[] { "Tim mạch can thiệp", "Siêu âm tim", "Nhịp tim học & Điện sinh lý", "Tăng huyết áp", "Suy tim mãn tính", "Tim bẩm sinh người lớn", "Phục hồi chức năng tim", "Bệnh mạch vành", "Tim mạch dự phòng", "Bệnh van tim", "Huyết khối & Thuyên tắc", "Siêu âm mạch máu", "Tim mạch lão khoa", "Bệnh cơ tim", "Bệnh màng ngoài tim", "Bệnh động mạch ngoại biên", "Tim mạch thể thao", "Đái tháo đường & Tim mạch", "Tim mạch cấp cứu", "Chẩn đoán hình ảnh tim" } },
                        { "Khoa Nhi", new[] { "Nhi tổng quát", "Nhi sơ sinh", "Dị ứng & Miễn dịch nhi", "Hô hấp nhi", "Tiêu hóa nhi", "Thần kinh nhi", "Dinh dưỡng nhi", "Tim mạch nhi", "Nhi nhiễm trùng", "Nội tiết nhi", "Huyết học nhi", "Thận nhi", "Nhi cấp cứu", "Tâm lý nhi khoa", "Da liễu nhi", "Ung bướu nhi", "Phục hồi chức năng nhi", "Chấn thương nhi", "Nhi khoa phát triển", "Nhi khoa cộng đồng" } },
                        { "Khoa Tai Mũi Họng", new[] { "Nội soi TMH", "Thính học lâm sàng", "Thanh quản & Giọng nói", "Mũi xoang nội soi", "Phẫu thuật TMH", "Tai thần kinh", "Dị ứng TMH", "Nhi TMH", "Ung bướu TMH", "Phẫu thuật tạo hình TMH", "Rối loạn nuốt", "Ngủ ngáy & Ngưng thở khi ngủ", "Viêm tai giữa mãn tính", "Vi phẫu thanh quản", "Cấy điện ốc tai", "Phẫu thuật xoang nội soi", "Chấn thương TMH", "TMH người già", "Chóng mặt & Thăng bằng", "U tuyến nước bọt" } },
                        { "Khoa Ngoại tổng hợp", new[] { "Ngoại tiêu hóa", "Ngoại gan mật tụy", "Nội soi ổ bụng", "Ngoại tổng quát", "Phẫu thuật lồng ngực", "Ngoại mạch máu", "Phẫu thuật tuyến giáp", "Ngoại tiết niệu", "Phẫu thuật trực tràng hậu môn", "Phẫu thuật tạo hình", "Ngoại nhi", "Ngoại chấn thương", "Phẫu thuật nội soi robot", "Ngoại ung bướu", "Phẫu thuật cấp cứu", "Phẫu thuật tuyến vú", "Phẫu thuật thoát vị", "Ngoại ghép tạng", "Phẫu thuật nội soi nâng cao", "Ngoại nhiễm trùng" } },
                        { "Khoa Sản phụ khoa", new[] { "Sản khoa tổng quát", "Phụ khoa tổng quát", "Thai sản nguy cơ cao", "Nội tiết sinh sản", "Ung bướu phụ khoa", "Sàn chậu & Tiểu không tự chủ", "Y học bào thai", "Vô sinh & Hiếm muộn", "Phẫu thuật nội soi phụ khoa", "Chẩn đoán trước sinh", "Sản khoa cấp cứu", "Siêu âm sản phụ khoa", "Mãn kinh & Nội tiết nữ", "Phẫu thuật tạo hình phụ khoa", "Sản khoa sau sinh", "Nuôi con bằng sữa mẹ", "Bệnh lý cổ tử cung", "Phụ khoa nhi & Vị thành niên", "Phụ khoa nhiễm trùng", "Kế hoạch hóa gia đình" } },
                        { "Khoa Da liễu", new[] { "Da liễu tổng quát", "Da liễu thẩm mỹ", "Laser da liễu", "Bệnh lây qua đường tình dục", "Dị ứng da & Chàm", "Da liễu nhi", "Bệnh tự miễn da", "Phẫu thuật da liễu", "Bệnh vẩy nến", "Bệnh nấm da", "Ung thư da", "Rụng tóc & Bệnh lý tóc", "Bệnh lý móng", "Da liễu laser & Ánh sáng", "Tiêm chất làm đầy", "Bệnh mô liên kết da", "Da liễu nhiệt đới", "Da liễu lão khoa", "Sẹo & Tái tạo da", "Bệnh sắc tố da" } },
                        { "Khoa Thần kinh", new[] { "Đột quỵ & Mạch máu não", "Parkinson & Rối loạn vận động", "Động kinh", "Đau đầu & Migraine", "Thần kinh cơ", "Sa sút trí tuệ & Alzheimer", "Rối loạn giấc ngủ", "Thần kinh tổng quát", "Đa xơ cứng", "Bệnh thần kinh ngoại biên", "Thần kinh nhi", "Thần kinh lão khoa", "Điện não đồ & Chẩn đoán", "Đau thần kinh", "Thần kinh ung bướu", "Thần kinh nhiễm trùng", "Phục hồi chức năng thần kinh", "Rối loạn thăng bằng", "Bệnh tủy sống", "Thần kinh hành vi" } },
                        { "Khoa Mắt", new[] { "Đục thủy tinh thể & Phaco", "Glaucoma (Cườm nước)", "Bệnh võng mạc & Dịch kính", "Khúc xạ & Kính áp tròng", "Nhãn khoa nhi", "Tạo hình mắt & Hốc mắt", "Giác mạc & Ghép giác mạc", "Mắt tổng quát", "Ung bướu mắt", "Lác mắt & Nhược thị", "Chấn thương mắt", "Nhãn áp & Thần kinh thị giác", "Phẫu thuật khúc xạ Lasik", "Bệnh mắt đái tháo đường", "Siêu âm mắt", "Viêm màng bồ đào", "Bệnh lệ đạo", "Kính áp tròng chuyên sâu", "Mắt lão khoa", "Thị lực thấp & Phục hồi" } },
                        { "Khoa Chấn thương Chỉnh hình", new[] { "Khớp gối", "Khớp háng & Thay khớp háng", "Cột sống", "Bàn tay & Vi phẫu", "Chấn thương thể thao", "Y học thể thao", "Xương khớp nhi", "Phẫu thuật bàn chân & Cổ chân", "Phẫu thuật vai & Khuỷu", "Chấn thương đa phức hợp", "Phẫu thuật chỉnh hình ung bướu", "Nội soi khớp", "Phục hồi chức năng chỉnh hình", "Loãng xương & Chuyển hóa xương", "Chấn thương chi trên", "Chấn thương chi dưới", "Nhiễm trùng xương khớp", "Phẫu thuật tạo hình xương", "Chấn thương cột sống cổ", "Vật lý trị liệu chỉnh hình" } },
                        { "Khoa Hồi sức cấp cứu", new[] { "Cấp cứu tổng quát", "Hồi sức tích cực", "Cấp cứu tim mạch", "Cấp cứu nhi", "Chống độc", "Đa chấn thương", "Hồi sức hô hấp", "Hồi sức thận nhân tạo", "Sốc & Nhiễm trùng huyết", "Cấp cứu thần kinh", "Cấp cứu hô hấp", "Hồi sức sau phẫu thuật", "Y học thảm họa", "Hồi sức sơ sinh", "Cấp cứu chấn thương", "Hồi sức bỏng", "Cấp cứu sản khoa", "Siêu âm cấp cứu (POCUS)", "Hồi sức nội khoa", "Cấp cứu ngoại khoa" } },
                        { "Khoa Ung bướu", new[] { "Ung thư nội khoa", "Hóa trị liệu", "Ung thư vú", "Ung thư phổi", "Ung thư tiêu hóa", "Xạ trị & Xạ phẫu", "Chăm sóc giảm nhẹ ung thư", "Ung thư huyết học", "Ung thư phụ khoa", "Ung thư đầu cổ", "Ung thư gan", "Ung thư tiết niệu", "Ung thư da", "Ung thư nhi", "Miễn dịch trị liệu ung thư", "Ung thư xương & Mô mềm", "Sinh học phân tử ung thư", "Dinh dưỡng ung thư", "Tầm soát ung thư", "Ung thư tuyến giáp" } },
                        { "Khoa Răng Hàm Mặt", new[] { "Nha tổng quát", "Chỉnh nha & Niềng răng", "Implant nha khoa", "Nha nhi", "Phẫu thuật hàm mặt", "Nội nha & Điều trị tủy", "Nha thẩm mỹ", "Phục hình răng", "Nha chu & Bệnh nướu", "Nhổ răng khôn & Tiểu phẫu", "Răng sứ & Veneer", "Tẩy trắng răng", "Phẫu thuật cắm Implant nâng cao", "Bệnh lý khớp thái dương hàm", "Nha khoa phục hồi", "Dị tật hàm mặt bẩm sinh", "Chấn thương hàm mặt", "Nha khoa người cao tuổi", "Nha khoa laser", "Nha khoa giấc ngủ" } },
                        { "Khoa Tiết niệu", new[] { "Sỏi tiết niệu & Tán sỏi", "Ung thư tiết niệu", "Nam học & Hiếm muộn nam", "Niệu nhi", "Nội soi tiết niệu", "Thận học lâm sàng", "Phẫu thuật tuyến tiền liệt", "Bệnh bàng quang", "Tiết niệu nữ", "Phẫu thuật nội soi robot", "Tiết niệu chức năng", "Nhiễm trùng tiết niệu", "Ghép thận", "Rối loạn cương dương", "Bệnh niệu đạo", "Tiết niệu lão khoa", "Siêu âm tiết niệu", "Phẫu thuật tạo hình tiết niệu", "Bệnh thận mãn", "Tiết niệu cấp cứu" } },
                        { "Khoa Y học cổ truyền", new[] { "Châm cứu trị liệu", "Bấm huyệt & Xoa bóp", "Thuốc đông y", "Dưỡng sinh & Khí công", "Cấy chỉ", "Vật lý trị liệu kết hợp", "Châm cứu giảm đau", "Đông y nội khoa", "Đông y phụ khoa", "Đông y nhi khoa", "Đông y cơ xương khớp", "Thủy châm", "Giác hơi & Cứu ngải", "Đông y da liễu", "Đông y thần kinh", "Phục hồi chức năng đông y", "Đông y tiêu hóa", "Đông y hô hấp", "Đông y nội tiết", "Yoga trị liệu kết hợp" } },
                    };

                    var rng = new Random(42); // Fixed seed for deterministic, reproducible data
                    int bulkEmailCounter = 100;

                    foreach (var dept in allDepartments)
                    {
                        var currentDocCount = context.Doctors.Count(d => d.KhoaId == dept.Id);
                        var targetCount = 15 + (rng.Next(6)); // 15–20
                        var toAdd = targetCount - currentDocCount;
                        if (toAdd <= 0) continue;

                        string[] specs;
                        if (!chuyenKhoaMap.TryGetValue(dept.TenKhoa, out specs!))
                            specs = new[] { dept.TenKhoa.Replace("Khoa ", "") + " tổng quát" };

                        for (int i = 0; i < toAdd; i++)
                        {
                            var email = $"bs{bulkEmailCounter:D3}@hms.com";
                            bulkEmailCounter++;

                            // Skip if email already exists (idempotent)
                            if (context.Users.Any(u => u.Email == email)) continue;

                            bool female = rng.Next(2) == 0;
                            var ho = hoArr[rng.Next(hoArr.Length)];
                            var dem = female ? demNu[rng.Next(demNu.Length)] : demNam[rng.Next(demNam.Length)];
                            var ten = female ? tenNuArr[rng.Next(tenNuArr.Length)] : tenNamArr[rng.Next(tenNamArr.Length)];
                            var fullName = $"BS. {ho} {dem} {ten}";
                            var phone = $"09{rng.Next(10000000, 100000000):D8}";

                            var user = new User
                            {
                                HoTen = fullName,
                                Email = email,
                                Sdt = phone,
                                MatKhauHash = HashHelper.HashPassword("Doctor@123"),
                                VaiTro = "Doctor",
                                TrangThai = "Active"
                            };
                            context.Users.Add(user);
                            context.SaveChanges();

                            var docIndex = currentDocCount + i;
                            string chucVu = "Bác sĩ";
                            if (docIndex == 0) chucVu = "Trưởng khoa";
                            else if (docIndex == 1 || docIndex == 2) chucVu = "Phó trưởng khoa";

                            context.Doctors.Add(new Doctor
                            {
                                NguoiDungId = user.Id,
                                KhoaId = dept.Id,
                                ChuyenKhoa = specs[docIndex % specs.Length],
                                HocVi = hocViArr[rng.Next(hocViArr.Length)],
                                SoNamKinhNghiem = rng.Next(3, 30),
                                LichLamViec = lichArr[rng.Next(lichArr.Length)],
                                ChucVu = chucVu
                            });
                        }
                        context.SaveChanges();
                    }
                }
            }

            // 4. Seed Medicines
            if (!context.Medicines.Any())
            {
                var med1 = new Medicine
                {
                    TenThuoc = "Paracetamol 500mg",
                    HoatChat = "Paracetamol",
                    DonViTinh = "Viên",
                    Gia = 2000,
                    TonKho = 500,
                    NguongToiThieu = 100
                };
                var med2 = new Medicine
                {
                    TenThuoc = "Amoxicillin 500mg (Kháng sinh)",
                    HoatChat = "Amoxicillin",
                    DonViTinh = "Viên",
                    Gia = 5000,
                    TonKho = 300,
                    NguongToiThieu = 50
                };
                var med3 = new Medicine
                {
                    TenThuoc = "Decolgen Forte",
                    HoatChat = "Acetaminophen, Phenylephrine",
                    DonViTinh = "Viên",
                    Gia = 3000,
                    TonKho = 150,
                    NguongToiThieu = 80
                };
                var med4 = new Medicine
                {
                    TenThuoc = "Amlodipine 5mg",
                    HoatChat = "Amlodipine Besylate",
                    DonViTinh = "Viên",
                    Gia = 4500,
                    TonKho = 400,
                    NguongToiThieu = 80
                };
                var med5 = new Medicine
                {
                    TenThuoc = "Metformin 500mg",
                    HoatChat = "Metformin Hydrochloride",
                    DonViTinh = "Viên",
                    Gia = 3500,
                    TonKho = 350,
                    NguongToiThieu = 70
                };
                var med6 = new Medicine
                {
                    TenThuoc = "Omeprazole 20mg",
                    HoatChat = "Omeprazole",
                    DonViTinh = "Viên",
                    Gia = 6000,
                    TonKho = 280,
                    NguongToiThieu = 60
                };
                var med7 = new Medicine
                {
                    TenThuoc = "Cetirizine 10mg (Chống dị ứng)",
                    HoatChat = "Cetirizine Hydrochloride",
                    DonViTinh = "Viên",
                    Gia = 3000,
                    TonKho = 200,
                    NguongToiThieu = 50
                };
                var med8 = new Medicine
                {
                    TenThuoc = "Azithromycin 500mg",
                    HoatChat = "Azithromycin",
                    DonViTinh = "Viên",
                    Gia = 18000,
                    TonKho = 180,
                    NguongToiThieu = 40
                };
                var med9 = new Medicine
                {
                    TenThuoc = "Vitamin C 1000mg Effervescent",
                    HoatChat = "Ascorbic Acid",
                    DonViTinh = "Viên sủi",
                    Gia = 8000,
                    TonKho = 220,
                    NguongToiThieu = 60
                };
                var med10 = new Medicine
                {
                    TenThuoc = "Ibuprofen 400mg",
                    HoatChat = "Ibuprofen",
                    DonViTinh = "Viên",
                    Gia = 4000,
                    TonKho = 90,   // Sắp dưới ngưỡng tối thiểu → kích hoạt cảnh báo
                    NguongToiThieu = 100
                };
                var med11 = new Medicine
                {
                    TenThuoc = "Atorvastatin 20mg",
                    HoatChat = "Atorvastatin Calcium",
                    DonViTinh = "Viên",
                    Gia = 12000,
                    TonKho = 160,
                    NguongToiThieu = 40
                };
                var med12 = new Medicine
                {
                    TenThuoc = "Salbutamol 2.5mg/2.5ml (Khí dung)",
                    HoatChat = "Salbutamol Sulfate",
                    DonViTinh = "Ống",
                    Gia = 25000,
                    TonKho = 120,
                    NguongToiThieu = 30
                };
                context.Medicines.AddRange(med1, med2, med3, med4, med5, med6, med7, med8, med9, med10, med11, med12);
                context.SaveChanges();

                // Seed Batches (LoThuoc) — FEFO demo scenarios
                context.MedicineBatches.AddRange(
                    // Paracetamol: Batch 1 (hết hạn 30 ngày) + Batch 2 (hết hạn 1 năm)
                    new MedicineBatch { ThuocId = med1.Id, SoLo = "LOT-PARA-001", HanSuDung = DateTime.Now.AddDays(30), SoLuongNhap = 200, SoLuongTon = 200 },
                    new MedicineBatch { ThuocId = med1.Id, SoLo = "LOT-PARA-002", HanSuDung = DateTime.Now.AddDays(365), SoLuongNhap = 300, SoLuongTon = 300 },
                    // Amoxicillin: Batch (hết hạn 6 tháng)
                    new MedicineBatch { ThuocId = med2.Id, SoLo = "LOT-AMOX-001", HanSuDung = DateTime.Now.AddDays(180), SoLuongNhap = 300, SoLuongTon = 300 },
                    // Decolgen: Batch sắp hết hạn (5 ngày) → cảnh báo
                    new MedicineBatch { ThuocId = med3.Id, SoLo = "LOT-DECO-001", HanSuDung = DateTime.Now.AddDays(5), SoLuongNhap = 150, SoLuongTon = 150 },
                    // Amlodipine
                    new MedicineBatch { ThuocId = med4.Id, SoLo = "LOT-AMLO-001", HanSuDung = DateTime.Now.AddDays(540), SoLuongNhap = 200, SoLuongTon = 200 },
                    new MedicineBatch { ThuocId = med4.Id, SoLo = "LOT-AMLO-002", HanSuDung = DateTime.Now.AddDays(720), SoLuongNhap = 200, SoLuongTon = 200 },
                    // Metformin
                    new MedicineBatch { ThuocId = med5.Id, SoLo = "LOT-METF-001", HanSuDung = DateTime.Now.AddDays(365), SoLuongNhap = 350, SoLuongTon = 350 },
                    // Omeprazole: Batch 1 sắp hết hạn (15 ngày)
                    new MedicineBatch { ThuocId = med6.Id, SoLo = "LOT-OMEP-001", HanSuDung = DateTime.Now.AddDays(15), SoLuongNhap = 80, SoLuongTon = 80 },
                    new MedicineBatch { ThuocId = med6.Id, SoLo = "LOT-OMEP-002", HanSuDung = DateTime.Now.AddDays(400), SoLuongNhap = 200, SoLuongTon = 200 },
                    // Cetirizine
                    new MedicineBatch { ThuocId = med7.Id, SoLo = "LOT-CETI-001", HanSuDung = DateTime.Now.AddDays(450), SoLuongNhap = 200, SoLuongTon = 200 },
                    // Azithromycin
                    new MedicineBatch { ThuocId = med8.Id, SoLo = "LOT-AZIT-001", HanSuDung = DateTime.Now.AddDays(300), SoLuongNhap = 180, SoLuongTon = 180 },
                    // Vitamin C
                    new MedicineBatch { ThuocId = med9.Id, SoLo = "LOT-VITC-001", HanSuDung = DateTime.Now.AddDays(270), SoLuongNhap = 220, SoLuongTon = 220 },
                    // Ibuprofen (tồn kho thấp hơn ngưỡng)
                    new MedicineBatch { ThuocId = med10.Id, SoLo = "LOT-IBUP-001", HanSuDung = DateTime.Now.AddDays(200), SoLuongNhap = 90, SoLuongTon = 90 },
                    // Atorvastatin
                    new MedicineBatch { ThuocId = med11.Id, SoLo = "LOT-ATOR-001", HanSuDung = DateTime.Now.AddDays(600), SoLuongNhap = 160, SoLuongTon = 160 },
                    // Salbutamol
                    new MedicineBatch { ThuocId = med12.Id, SoLo = "LOT-SALB-001", HanSuDung = DateTime.Now.AddDays(365), SoLuongNhap = 120, SoLuongTon = 120 }
                );
                context.SaveChanges();
            }


            // 5. Seed Historical Appointment & Examination & Invoice for Demo
            if (!context.Appointments.Any())
            {
                var patient = context.Patients.First();
                var doctor = context.Doctors.First();

                // 1st Appointment - Completed historical record
                var appOld = new Appointment
                {
                    BenhNhanId = patient.Id,
                    BacSiId = doctor.Id,
                    ThoiGian = DateTime.Now.AddDays(-2),
                    TrangThai = "HoanThanh",
                    LyDoKham = "Thường xuyên tức ngực trái và khó thở nhẹ khi gắng sức.",
                    NgayTao = DateTime.Now.AddDays(-3)
                };
                context.Appointments.Add(appOld);
                context.SaveChanges();

                // Examination
                var exam = new ExaminationRecord
                {
                    LichKhamId = appOld.Id,
                    TrieuChung = "Huyết áp 135/85 mmHg, nhịp tim hơi nhanh 85 l/ph. Ngực trái tức âm ỉ.",
                    HuyetAp = "135/85",
                    NhipTim = 85,
                    NhietDo = 36.8m,
                    CanNang = 70.0m,
                    ChieuCao = 172.0m,
                    BMI = 23.66m,
                    ChanDoan = "Cơn đau thắt ngực ổn định / Theo dõi tăng huyết áp nhẹ",
                    LoiDan = "Hạn chế đồ dầu mỡ, giảm ăn muối. Tái khám sau 2 tuần hoặc khi có dấu hiệu bất thường.",
                    ChiDinhCLS = "Điện tâm đồ (ECG)",
                    KetQuaCLS = "Nhịp xoang đều, có dấu hiệu thiếu máu cơ tim cục bộ thành sau.",
                    NgayKham = DateTime.Now.AddDays(-2)
                };
                context.ExaminationRecords.Add(exam);
                context.SaveChanges();

                // Prescription
                var pres = new Prescription
                {
                    PhieuKhamId = exam.Id,
                    NgayKe = DateTime.Now.AddDays(-2)
                };
                context.Prescriptions.Add(pres);
                context.SaveChanges();

                // Prescription Detail
                var medPara = context.Medicines.First(m => m.TenThuoc.StartsWith("Paracetamol"));
                context.PrescriptionDetails.Add(new PrescriptionDetail
                {
                    DonThuocId = pres.Id,
                    ThuocId = medPara.Id,
                    LieuDung = "Uống 1 viên khi nhức đầu, tối đa 3 lần/ngày.",
                    SoLuong = 10
                });

                // Deduct inventory from batch (FEFO)
                // Para LOT-PARA-001 has 200, expires first
                var batchPara = context.MedicineBatches.First(b => b.ThuocId == medPara.Id && b.SoLo == "LOT-PARA-001");
                batchPara.SoLuongTon -= 10;
                medPara.TonKho -= 10;

                context.SaveChanges();

                // Invoice 1 (DaThanhToan)
                var invoice = new Invoice
                {
                    PhieuKhamId = exam.Id,
                    TongTien = 150000 + 200000 + (10 * 2000), // Phí khám + Phí ECG + Tiền thuốc
                    TrangThaiThanhToan = "DaThanhToan",
                    PhuongThuc = "TienMat",
                    NgayTao = DateTime.Now.AddDays(-2),
                    NgayThanhToan = DateTime.Now.AddDays(-2)
                };
                context.Invoices.Add(invoice);
                context.SaveChanges();

                context.InvoiceDetails.AddRange(
                    new InvoiceDetail { HoaDonId = invoice.Id, LoaiPhi = "Phí Khám", SoTien = 150000 },
                    new InvoiceDetail { HoaDonId = invoice.Id, LoaiPhi = "Dịch vụ ECG", SoTien = 200000 },
                    new InvoiceDetail { HoaDonId = invoice.Id, LoaiPhi = "Thuốc (Paracetamol)", SoTien = 20000 }
                );
                context.SaveChanges();

                // Invoice 2: DaThanhToan (completed 5 days ago, Online MoMo)
                var app2 = new Appointment
                {
                    BenhNhanId = patient.Id,
                    BacSiId = doctor.Id,
                    ThoiGian = DateTime.Now.AddDays(-5).Date.AddHours(10),
                    TrangThai = "HoanThanh",
                    LyDoKham = "Kiểm tra sức khỏe tim mạch định kỳ.",
                    NgayTao = DateTime.Now.AddDays(-6)
                };
                context.Appointments.Add(app2);
                context.SaveChanges();

                var exam2 = new ExaminationRecord
                {
                    LichKhamId = app2.Id,
                    TrieuChung = "Nhịp tim bình thường, huyết áp ổn định.",
                    HuyetAp = "120/80",
                    NhipTim = 72,
                    NhietDo = 36.6m,
                    CanNang = 69.5m,
                    ChieuCao = 172.0m,
                    BMI = 23.49m,
                    ChanDoan = "Sức khỏe tim mạch tốt",
                    LoiDan = "Duy trì chế độ ăn uống lành mạnh và tập thể dục đều đặn.",
                    ChiDinhCLS = "Siêu âm tim màu",
                    KetQuaCLS = "Cấu trúc tim bình thường, không phát hiện bệnh lý.",
                    NgayKham = DateTime.Now.AddDays(-5)
                };
                context.ExaminationRecords.Add(exam2);
                context.SaveChanges();

                var invoice2 = new Invoice
                {
                    PhieuKhamId = exam2.Id,
                    TongTien = 150000 + 300000, // Phí khám + Siêu âm tim màu
                    TrangThaiThanhToan = "DaThanhToan",
                    PhuongThuc = "Online (MoMo)",
                    MaGiaoDich = "MM987654321",
                    NgayTao = DateTime.Now.AddDays(-5),
                    NgayThanhToan = DateTime.Now.AddDays(-5)
                };
                context.Invoices.Add(invoice2);
                context.SaveChanges();

                context.InvoiceDetails.AddRange(
                    new InvoiceDetail { HoaDonId = invoice2.Id, LoaiPhi = "Phí Khám", SoTien = 150000 },
                    new InvoiceDetail { HoaDonId = invoice2.Id, LoaiPhi = "Dịch vụ Siêu âm tim màu", SoTien = 300000 }
                );
                context.SaveChanges();

                // Invoice 3: ChuaThanhToan (completed 1 day ago)
                var app3 = new Appointment
                {
                    BenhNhanId = patient.Id,
                    BacSiId = doctor.Id,
                    ThoiGian = DateTime.Now.AddDays(-1).Date.AddHours(14),
                    TrangThai = "HoanThanh",
                    LyDoKham = "Tư vấn chế độ dinh dưỡng cho người cao huyết áp.",
                    NgayTao = DateTime.Now.AddDays(-2)
                };
                context.Appointments.Add(app3);
                context.SaveChanges();

                var exam3 = new ExaminationRecord
                {
                    LichKhamId = app3.Id,
                    TrieuChung = "Không có triệu chứng đau ngực, huyết áp 130/80 mmHg.",
                    HuyetAp = "130/80",
                    NhipTim = 75,
                    NhietDo = 36.5m,
                    CanNang = 70.2m,
                    ChieuCao = 172.0m,
                    BMI = 23.73m,
                    ChanDoan = "Tăng huyết áp độ 1 ổn định",
                    LoiDan = "Uống nhiều nước, ăn nhiều rau xanh.",
                    ChiDinhCLS = "Không",
                    KetQuaCLS = "Không",
                    NgayKham = DateTime.Now.AddDays(-1)
                };
                context.ExaminationRecords.Add(exam3);
                context.SaveChanges();

                var invoice3 = new Invoice
                {
                    PhieuKhamId = exam3.Id,
                    TongTien = 150000,
                    TrangThaiThanhToan = "ChuaThanhToan",
                    PhuongThuc = "ChuaThanhToan",
                    NgayTao = DateTime.Now.AddDays(-1)
                };
                context.Invoices.Add(invoice3);
                context.SaveChanges();

                context.InvoiceDetails.Add(new InvoiceDetail { HoaDonId = invoice3.Id, LoaiPhi = "Phí Khám", SoTien = 150000 });
                context.SaveChanges();

                // Invoice 4: QuaHan (completed 35 days ago)
                var app4 = new Appointment
                {
                    BenhNhanId = patient.Id,
                    BacSiId = doctor.Id,
                    ThoiGian = DateTime.Now.AddDays(-35).Date.AddHours(9),
                    TrangThai = "HoanThanh",
                    LyDoKham = "Khám sức khỏe tổng quát đầu năm.",
                    NgayTao = DateTime.Now.AddDays(-36)
                };
                context.Appointments.Add(app4);
                context.SaveChanges();

                var exam4 = new ExaminationRecord
                {
                    LichKhamId = app4.Id,
                    TrieuChung = "Mỏi mệt kéo dài.",
                    HuyetAp = "118/75",
                    NhipTim = 70,
                    NhietDo = 36.7m,
                    CanNang = 71.0m,
                    ChieuCao = 172.0m,
                    BMI = 24.0m,
                    ChanDoan = "Suy nhược cơ thể nhẹ",
                    LoiDan = "Nghỉ ngơi hợp lý, tránh thức khuya.",
                    ChiDinhCLS = "Điện tâm đồ (ECG)",
                    KetQuaCLS = "Kết quả ECG bình thường.",
                    NgayKham = DateTime.Now.AddDays(-35)
                };
                context.ExaminationRecords.Add(exam4);
                context.SaveChanges();

                var invoice4 = new Invoice
                {
                    PhieuKhamId = exam4.Id,
                    TongTien = 150000 + 200000 + 100000,
                    TrangThaiThanhToan = "QuaHan",
                    PhuongThuc = "ChuaThanhToan",
                    NgayTao = DateTime.Now.AddDays(-35)
                };
                context.Invoices.Add(invoice4);
                context.SaveChanges();

                context.InvoiceDetails.AddRange(
                    new InvoiceDetail { HoaDonId = invoice4.Id, LoaiPhi = "Phí Khám", SoTien = 150000 },
                    new InvoiceDetail { HoaDonId = invoice4.Id, LoaiPhi = "Dịch vụ ECG", SoTien = 200000 },
                    new InvoiceDetail { HoaDonId = invoice4.Id, LoaiPhi = "Thuốc bổ bổ sung vitamin", SoTien = 100000 }
                );
                context.SaveChanges();

                // Invoice 5: ThanhToanThatBai (completed 3 days ago, Online VNPay)
                var app5 = new Appointment
                {
                    BenhNhanId = patient.Id,
                    BacSiId = doctor.Id,
                    ThoiGian = DateTime.Now.AddDays(-3).Date.AddHours(11),
                    TrangThai = "HoanThanh",
                    LyDoKham = "Theo dõi đau thắt ngực trái.",
                    NgayTao = DateTime.Now.AddDays(-4)
                };
                context.Appointments.Add(app5);
                context.SaveChanges();

                var exam5 = new ExaminationRecord
                {
                    LichKhamId = app5.Id,
                    TrieuChung = "Thỉnh thoảng nhói ngực trái khi căng thẳng.",
                    HuyetAp = "125/82",
                    NhipTim = 78,
                    NhietDo = 36.6m,
                    CanNang = 69.8m,
                    ChieuCao = 172.0m,
                    BMI = 23.59m,
                    ChanDoan = "Thiếu máu cơ tim nhẹ",
                    LoiDan = "Tránh căng thẳng, tái khám định kỳ.",
                    ChiDinhCLS = "Siêu âm tim màu",
                    KetQuaCLS = "Nhịp xoang đều.",
                    NgayKham = DateTime.Now.AddDays(-3)
                };
                context.ExaminationRecords.Add(exam5);
                context.SaveChanges();

                var invoice5 = new Invoice
                {
                    PhieuKhamId = exam5.Id,
                    TongTien = 350000,
                    TrangThaiThanhToan = "ThanhToanThatBai",
                    PhuongThuc = "Online (VNPay)",
                    MaGiaoDich = "VP123456789",
                    NgayTao = DateTime.Now.AddDays(-3)
                };
                context.Invoices.Add(invoice5);
                context.SaveChanges();

                context.InvoiceDetails.Add(new InvoiceDetail { HoaDonId = invoice5.Id, LoaiPhi = "Dịch vụ Siêu âm tim màu", SoTien = 350000 });
                context.SaveChanges();

                // Invoice 6: DangXuLy (completed 2 days ago, ChuyenKhoan)
                var app6 = new Appointment
                {
                    BenhNhanId = patient.Id,
                    BacSiId = doctor.Id,
                    ThoiGian = DateTime.Now.AddDays(-2).Date.AddHours(15),
                    TrangThai = "HoanThanh",
                    LyDoKham = "Khám và kê đơn thuốc huyết áp định kỳ.",
                    NgayTao = DateTime.Now.AddDays(-3)
                };
                context.Appointments.Add(app6);
                context.SaveChanges();

                var exam6 = new ExaminationRecord
                {
                    LichKhamId = app6.Id,
                    TrieuChung = "Huyết áp ổn định dưới tác dụng của thuốc.",
                    HuyetAp = "120/80",
                    NhipTim = 70,
                    NhietDo = 36.5m,
                    CanNang = 70.0m,
                    ChieuCao = 172.0m,
                    BMI = 23.66m,
                    ChanDoan = "Tăng huyết áp vô căn",
                    LoiDan = "Tiếp tục dùng thuốc theo đơn cũ.",
                    ChiDinhCLS = "Không",
                    KetQuaCLS = "Không",
                    NgayKham = DateTime.Now.AddDays(-2)
                };
                context.ExaminationRecords.Add(exam6);
                context.SaveChanges();

                var invoice6 = new Invoice
                {
                    PhieuKhamId = exam6.Id,
                    TongTien = 120000,
                    TrangThaiThanhToan = "DangXuLy",
                    PhuongThuc = "ChuyenKhoan",
                    MaGiaoDich = "CK_TRF_88291",
                    NgayTao = DateTime.Now.AddDays(-2)
                };
                context.Invoices.Add(invoice6);
                context.SaveChanges();

                context.InvoiceDetails.Add(new InvoiceDetail { HoaDonId = invoice6.Id, LoaiPhi = "Phí Khám", SoTien = 120000 });
                context.SaveChanges();

                // Invoice 7: DaHuy (completed 6 days ago)
                var app7 = new Appointment
                {
                    BenhNhanId = patient.Id,
                    BacSiId = doctor.Id,
                    ThoiGian = DateTime.Now.AddDays(-6).Date.AddHours(10),
                    TrangThai = "HoanThanh",
                    LyDoKham = "Khám Tai Mũi Họng khẩn cấp.",
                    NgayTao = DateTime.Now.AddDays(-6)
                };
                context.Appointments.Add(app7);
                context.SaveChanges();

                var exam7 = new ExaminationRecord
                {
                    LichKhamId = app7.Id,
                    TrieuChung = "Đau họng, nuốt vướng.",
                    HuyetAp = "115/75",
                    NhipTim = 74,
                    NhietDo = 37.2m,
                    CanNang = 70.1m,
                    ChieuCao = 172.0m,
                    BMI = 23.7m,
                    ChanDoan = "Viêm họng cấp",
                    LoiDan = "Súc họng nước muối, uống nhiều nước ấm.",
                    ChiDinhCLS = "Không",
                    KetQuaCLS = "Không",
                    NgayKham = DateTime.Now.AddDays(-6)
                };
                context.ExaminationRecords.Add(exam7);
                context.SaveChanges();

                var invoice7 = new Invoice
                {
                    PhieuKhamId = exam7.Id,
                    TongTien = 120000,
                    TrangThaiThanhToan = "DaHuy",
                    PhuongThuc = "ChuaThanhToan",
                    NgayTao = DateTime.Now.AddDays(-6)
                };
                context.Invoices.Add(invoice7);
                context.SaveChanges();

                context.InvoiceDetails.Add(new InvoiceDetail { HoaDonId = invoice7.Id, LoaiPhi = "Phí Khám", SoTien = 120000 });
                context.SaveChanges();

                // Audit Log
                context.AuditLogs.Add(new AuditLog
                {
                    NguoiDungId = doctor.NguoiDungId,
                    HanhDong = "Lưu bệnh án & kê đơn",
                    ChiTiet = $"BS. Nguyễn Văn Trung hoàn tất khám cho bệnh nhân Trần Văn A, chẩn đoán: {exam.ChanDoan}.",
                    ThoiGian = DateTime.Now.AddDays(-2)
                });
                context.SaveChanges();

                // Seed some upcoming appointments
                context.Appointments.AddRange(
                    new Appointment
                    {
                        BenhNhanId = patient.Id,
                        BacSiId = doctor.Id,
                        ThoiGian = DateTime.Now.AddDays(1).Date.AddHours(9), // 9:00 AM tomorrow
                        TrangThai = "DaXacNhan",
                        LyDoKham = "Tái khám tim mạch và kiểm tra huyết áp thường kỳ.",
                        NgayTao = DateTime.Now.AddDays(-1)
                    },
                    new Appointment
                    {
                        BenhNhanId = context.Patients.Skip(1).First().Id, // Patient 2
                        BacSiId = context.Doctors.Skip(1).First().Id, // Doctor 2
                        ThoiGian = DateTime.Now.AddHours(2), // Today soon!
                        TrangThai = "ChoXacNhan",
                        LyDoKham = "Bé bị sốt cao kèm ho khan từ tối qua.",
                        NgayTao = DateTime.Now
                    }
                );
                context.SaveChanges();
            }

            // 6. Seed Notifications for Patient Trần Văn A (NguoiDungId = 3)
            if (!context.Notifications.Any())
            {
                var patientUser = context.Users.FirstOrDefault(u => u.Email == "patient@hms.com");
                if (patientUser != null)
                {
                    context.Notifications.AddRange(
                        new Notification
                        {
                            NguoiDungId = patientUser.Id,
                            NoiDung = "[LichKham] Lịch khám đã xác nhận|Cuộc hẹn với BS. Nguyễn Văn Trung vào lúc 09:00 ngày mai đã được xác nhận.",
                            NgayGui = DateTime.Now.AddMinutes(-10),
                            DaDoc = false
                        },
                        new Notification
                        {
                            NguoiDungId = patientUser.Id,
                            NoiDung = "[LichKham] Nhắc lịch khám|Đừng quên bạn có lịch khám chuyên khoa Tim mạch vào ngày mai lúc 09:00.",
                            NgayGui = DateTime.Now.AddHours(-2),
                            DaDoc = false
                        },
                        new Notification
                        {
                            NguoiDungId = patientUser.Id,
                            NoiDung = "[ThanhToan] Hóa đơn thanh toán|Hóa đơn số #INV-0001 đã được thanh toán thành công.",
                            NgayGui = DateTime.Now.AddHours(-5),
                            DaDoc = false
                        },
                        new Notification
                        {
                            NguoiDungId = patientUser.Id,
                            NoiDung = "[ThanhToan] Thanh toán thất bại|Giao dịch thanh toán phí khám của bạn không thành công. Vui lòng kiểm tra lại hoặc thử lại.",
                            NgayGui = DateTime.Now.AddDays(-1),
                            DaDoc = true
                        },
                        new Notification
                        {
                            NguoiDungId = patientUser.Id,
                            NoiDung = "[DonThuoc] Đơn thuốc mới|BS. Nguyễn Văn Trung vừa cập nhật đơn thuốc mới cho hồ sơ của bạn.",
                            NgayGui = DateTime.Now.AddDays(-2),
                            DaDoc = true
                        }
                    );
                    context.SaveChanges();
                }
            }

            // 7. Seed Dependents for Trần Văn A
            if (!context.Dependents.Any())
            {
                var patient = context.Patients.FirstOrDefault();
                if (patient != null)
                {
                    context.Dependents.AddRange(
                        new Dependent
                        {
                            BenhNhanId = patient.Id,
                            HoTen = "Trần Minh Hải",
                            QuanHe = "Con trai",
                            GioiTinh = "Nam",
                            NamSinh = 2021,
                            NhomMau = "O+",
                            SoBHYT = "TE1011242352",
                            TienSuBenhLy = "Viêm phế quản co thắt"
                        },
                        new Dependent
                        {
                            BenhNhanId = patient.Id,
                            HoTen = "Nguyễn Thị Hoa",
                            QuanHe = "Mẹ ruột",
                            GioiTinh = "Nữ",
                            NamSinh = 1964,
                            NhomMau = "B+",
                            SoBHYT = "GD4797920102",
                            TienSuBenhLy = "Rối loạn tuần hoàn não"
                        }
                    );
                    context.SaveChanges();
                }
            }

            SynchronizeDemoAccountCredentials(context);
            SeedDefaultDoctorWorkSchedules(context);
            SeedAdditionalPatientFamilies(context);
        }

        private static void SynchronizeDemoAccountCredentials(ApplicationDbContext context)
        {
            var demoAccounts = new[]
            {
                new { Email = "admin@hms.com", Password = "Admin@123", Role = "Admin" },
                new { Email = "doctor@hms.com", Password = "Doctor@123", Role = "Doctor" },
                new { Email = "patient@hms.com", Password = "Patient@123", Role = "Patient" }
            };

            var changed = false;
            foreach (var demo in demoAccounts)
            {
                var user = context.Users.FirstOrDefault(u => u.Email == demo.Email);
                if (user == null)
                {
                    continue;
                }

                var expectedHash = HashHelper.HashPassword(demo.Password);
                if (user.MatKhauHash != expectedHash || user.VaiTro != demo.Role || user.TrangThai != "Active")
                {
                    user.MatKhauHash = expectedHash;
                    user.VaiTro = demo.Role;
                    user.TrangThai = "Active";
                    changed = true;
                }
            }

            if (changed)
            {
                context.SaveChanges();
            }
        }

        private static void SeedDefaultDoctorWorkSchedules(ApplicationDbContext context)
        {
            var doctorsWithoutSchedules = context.Doctors
                .Where(d => !context.DoctorWorkSchedules.Any(s => s.BacSiId == d.Id))
                .ToList();

            foreach (var doctor in doctorsWithoutSchedules)
            {
                var schedules = DoctorScheduleHelper.BuildSchedulesFromDescription(doctor.Id, doctor.LichLamViec);
                context.DoctorWorkSchedules.AddRange(schedules);
            }

            if (doctorsWithoutSchedules.Any())
            {
                context.SaveChanges();
            }
        }

        private static void SeedAdditionalPatientFamilies(ApplicationDbContext context)
        {
            var families = new List<(
                string HoTen,
                string Email,
                string Sdt,
                DateTime NgaySinh,
                string GioiTinh,
                string NhomMau,
                string SoBHYT,
                string TienSuBenh,
                string DiUng,
                List<(string HoTen, string QuanHe, string GioiTinh, int NamSinh, string NhomMau, string SoBHYT, string TienSuBenhLy)> Dependents)>
            {
                (
                    "Lê Thị Mai Anh",
                    "patient.family01@hms.com",
                    "0908123456",
                    new DateTime(1986, 3, 12),
                    "Nữ",
                    "A+",
                    "HN4860312456",
                    "Viêm dạ dày mạn tính, thiếu máu nhẹ sau sinh, theo dõi tuyến giáp định kỳ.",
                    "Dị ứng Penicillin, hải sản.",
                    new List<(string, string, string, int, string, string, string)>
                    {
                        ("Lê Gia Bảo", "Con trai", "Nam", 2017, "O+", "TE1170312456", "Viêm mũi dị ứng theo mùa, từng sốt co giật lúc 2 tuổi."),
                        ("Nguyễn Thị Lan", "Mẹ ruột", "Nữ", 1958, "B+", "GD4580312456", "Tăng huyết áp độ 2, thoái hóa khớp gối, cần theo dõi đường huyết.")
                    }
                ),
                (
                    "Phạm Quốc Huy",
                    "patient.family02@hms.com",
                    "0919234567",
                    new DateTime(1979, 10, 4),
                    "Nam",
                    "O+",
                    "SG4791045678",
                    "Tăng huyết áp, rối loạn lipid máu, từng phẫu thuật ruột thừa năm 2011.",
                    "Không ghi nhận dị ứng thuốc.",
                    new List<(string, string, string, int, string, string, string)>
                    {
                        ("Trần Mỹ Duyên", "Vợ", "Nữ", 1982, "AB+", "GD4821045678", "Đau nửa đầu tái phát, dị ứng thời tiết."),
                        ("Phạm Minh Khang", "Con trai", "Nam", 2012, "O+", "TE1121045678", "Hen phế quản nhẹ, cần tránh khói bụi và lông thú.")
                    }
                ),
                (
                    "Võ Thị Thanh Trúc",
                    "patient.family03@hms.com",
                    "0930345678",
                    new DateTime(1994, 8, 21),
                    "Nữ",
                    "B+",
                    "DN4940821678",
                    "Đang theo dõi thai kỳ nguy cơ thấp, tiền sử viêm xoang mạn.",
                    "Dị ứng Ibuprofen.",
                    new List<(string, string, string, int, string, string, string)>
                    {
                        ("Võ Hoàng Nam", "Cha ruột", "Nam", 1962, "A-", "GD4620821678", "Đái tháo đường type 2, bệnh thận mạn giai đoạn 2."),
                        ("Ngô Bảo Ngọc", "Con gái", "Nữ", 2020, "B+", "TE1200821678", "Chàm cơ địa, dị ứng đạm sữa bò lúc nhỏ.")
                    }
                ),
                (
                    "Đặng Minh Tuấn",
                    "patient.family04@hms.com",
                    "0971456789",
                    new DateTime(1967, 12, 2),
                    "Nam",
                    "AB-",
                    "CT4671220789",
                    "Bệnh mạch vành đã đặt stent, gout, gan nhiễm mỡ độ 1.",
                    "Dị ứng thuốc cản quang iod.",
                    new List<(string, string, string, int, string, string, string)>
                    {
                        ("Đặng Thị Kim Oanh", "Vợ", "Nữ", 1970, "O-", "GD4701220789", "Loãng xương, thiếu vitamin D, tiền sử viêm loét dạ dày."),
                        ("Đặng Minh Khôi", "Con trai", "Nam", 1999, "AB-", "SV4991220789", "Không có bệnh nền đáng chú ý, dị ứng phấn hoa nhẹ.")
                    }
                )
            };

            var changed = false;

            foreach (var family in families)
            {
                var user = context.Users.FirstOrDefault(u => u.Email == family.Email);
                if (user == null)
                {
                    user = new User
                    {
                        HoTen = family.HoTen,
                        Email = family.Email,
                        Sdt = family.Sdt,
                        MatKhauHash = HashHelper.HashPassword("Patient@123"),
                        VaiTro = "Patient",
                        TrangThai = "Active"
                    };
                    context.Users.Add(user);
                    context.SaveChanges();
                    changed = true;
                }

                var patient = context.Patients.FirstOrDefault(p => p.NguoiDungId == user.Id);
                if (patient == null)
                {
                    patient = new Patient
                    {
                        NguoiDungId = user.Id,
                        NgaySinh = family.NgaySinh,
                        GioiTinh = family.GioiTinh,
                        NhomMau = family.NhomMau,
                        SoBHYT = family.SoBHYT,
                        TienSuBenh = family.TienSuBenh,
                        DiUng = family.DiUng
                    };
                    context.Patients.Add(patient);
                    context.SaveChanges();
                    changed = true;
                }

                foreach (var dependentSeed in family.Dependents)
                {
                    var dependentExists = context.Dependents.Any(d =>
                        d.BenhNhanId == patient.Id &&
                        d.HoTen == dependentSeed.HoTen);

                    if (dependentExists) continue;

                    context.Dependents.Add(new Dependent
                    {
                        BenhNhanId = patient.Id,
                        HoTen = dependentSeed.HoTen,
                        QuanHe = dependentSeed.QuanHe,
                        GioiTinh = dependentSeed.GioiTinh,
                        NamSinh = dependentSeed.NamSinh,
                        NhomMau = dependentSeed.NhomMau,
                        SoBHYT = dependentSeed.SoBHYT,
                        TienSuBenhLy = dependentSeed.TienSuBenhLy
                    });
                    changed = true;
                }
            }

            if (changed)
            {
                context.SaveChanges();
            }
        }
    }
}
