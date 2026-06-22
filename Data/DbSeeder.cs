using System;
using System.Linq;
using QuanLyBenhVien.Models;
using QuanLyBenhVien.Helpers;

namespace QuanLyBenhVien.Data
{
    public static class DbSeeder
    {
        public static void Seed(ApplicationDbContext context)
        {
            context.Database.EnsureCreated();

            // 1. Seed Departments
            if (!context.Departments.Any())
            {
                context.Departments.AddRange(
                    new Department { TenKhoa = "Khoa Nội tổng quát", MoTa = "Chẩn đoán và điều trị bệnh nội khoa người lớn", ViTri = "Tầng 1 - Tòa nhà A" },
                    new Department { TenKhoa = "Khoa Tim mạch", MoTa = "Chẩn đoán và điều trị các bệnh lý tim mạch chuyên sâu", ViTri = "Tầng 2 - Tòa nhà B" },
                    new Department { TenKhoa = "Khoa Nhi", MoTa = "Chăm sóc sức khỏe toàn diện cho trẻ sơ sinh và trẻ nhỏ", ViTri = "Tầng 1 - Tòa nhà C" },
                    new Department { TenKhoa = "Khoa Tai Mũi Họng", MoTa = "Khám và điều trị các bệnh lý liên quan đến Tai, Mũi và Họng", ViTri = "Tầng 3 - Tòa nhà A" }
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

                context.SaveChanges();

                // Create Profiles
                var cardioDept = context.Departments.First(d => d.TenKhoa == "Khoa Tim mạch");
                var pediatricsDept = context.Departments.First(d => d.TenKhoa == "Khoa Nhi");

                var docProfile1 = new Doctor
                {
                    NguoiDungId = doctorCardio.Id,
                    KhoaId = cardioDept.Id,
                    ChuyenKhoa = "Tim mạch học can thiệp",
                    HocVi = "ThS.BS",
                    SoNamKinhNghiem = 10,
                    LichLamViec = "Ca sáng (08:00 - 12:00) các ngày trong tuần"
                };
                var docProfile2 = new Doctor
                {
                    NguoiDungId = doctorPediatrics.Id,
                    KhoaId = pediatricsDept.Id,
                    ChuyenKhoa = "Dị ứng & Miễn dịch nhi khoa",
                    HocVi = "TS.BS",
                    SoNamKinhNghiem = 15,
                    LichLamViec = "Ca chiều (13:30 - 17:30) Thứ 2, 4, 6"
                };
                context.Doctors.AddRange(docProfile1, docProfile2);

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
                context.Patients.AddRange(patProfile1, patProfile2);

                context.SaveChanges();
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
                context.Medicines.AddRange(med1, med2, med3);
                context.SaveChanges();

                // Seed Batches (LoThuoc)
                context.MedicineBatches.AddRange(
                    // Paracetamol: Batch 1 (expires in 1 month) and Batch 2 (expires in 1 year)
                    new MedicineBatch { ThuocId = med1.Id, SoLo = "LOT-PARA-001", HanSuDung = DateTime.Now.AddDays(30), SoLuongNhap = 200, SoLuongTon = 200 },
                    new MedicineBatch { ThuocId = med1.Id, SoLo = "LOT-PARA-002", HanSuDung = DateTime.Now.AddDays(365), SoLuongNhap = 300, SoLuongTon = 300 },
                    // Amoxicillin: Batch 3 (expires in 6 months)
                    new MedicineBatch { ThuocId = med2.Id, SoLo = "LOT-AMOX-001", HanSuDung = DateTime.Now.AddDays(180), SoLuongNhap = 300, SoLuongTon = 300 },
                    // Decolgen: Batch 4 (expires in 5 days - warning!)
                    new MedicineBatch { ThuocId = med3.Id, SoLo = "LOT-DECO-001", HanSuDung = DateTime.Now.AddDays(5), SoLuongNhap = 150, SoLuongTon = 150 }
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

                // Invoice
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
        }
    }
}
