using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Helpers;
using QuanLyBenhVien.Models.ViewModels.PrescriptionSafety;

namespace QuanLyBenhVien.Services
{
    // Toàn bộ dữ liệu tương tác thuốc/nhóm dị ứng chéo dùng ở đây là DỮ LIỆU
    // MINH HỌA (xem DbSeeder, cờ LaDuLieuMinhHoa), chưa qua thẩm định dược lý
    // lâm sàng - engine này chỉ minh họa cơ chế cảnh báo 3 mức, không phải
    // nguồn tra cứu y khoa chính thức. Interface không biết "ai đang gọi" (kê
    // đơn ngoại trú, mẫu đơn, hay sau này y lệnh nội trú - chưa xây), để dùng
    // lại nguyên vẹn cho các luồng kê thuốc khác.
    public class PrescriptionSafetyChecker : IPrescriptionSafetyChecker
    {
        private const int PediatricAgeThreshold = 15;

        private readonly ApplicationDbContext _context;

        public PrescriptionSafetyChecker(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PrescriptionSafetyResult> CheckAsync(PrescriptionSafetyContext context)
        {
            var result = new PrescriptionSafetyResult();
            if (context.CandidateLines.Count == 0) return result;

            var patient = await _context.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == context.PatientId);
            if (patient == null) return result;

            var medicineIds = context.CandidateLines.Select(l => l.MedicineId).Distinct().ToList();
            var medicines = await _context.Medicines.AsNoTracking()
                .Where(m => medicineIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id);

            var activeIngredients = await _context.PrescriptionDetails.AsNoTracking()
                .Where(pd => (pd.Prescription.TrangThai == "ChoCapPhat" || pd.Prescription.TrangThai == "DaCapPhat")
                    && pd.Prescription.ExaminationRecord.Appointment.BenhNhanId == context.PatientId
                    && (context.ExcludePrescriptionId == null || pd.DonThuocId != context.ExcludePrescriptionId))
                .Select(pd => pd.Medicine.HoatChat)
                .ToListAsync();

            var allergyGroups = await _context.DrugAllergyGroups.AsNoTracking()
                .Include(g => g.ThanhVien)
                .ToListAsync();

            var interactions = await _context.DrugInteractions.AsNoTracking().ToListAsync();

            // Khóa = hoạt chất đã chuẩn hóa (bỏ dấu, chữ thường); giá trị = tên
            // hiển thị gốc + danh sách thuốc trong đơn đang cân nhắc mang hoạt chất đó.
            var candidateIngredients = new Dictionary<string, (string Display, List<int> MedicineIds)>();
            foreach (var line in context.CandidateLines)
            {
                if (!medicines.TryGetValue(line.MedicineId, out var medicine)) continue;
                foreach (var ing in SplitIngredients(medicine.HoatChat))
                {
                    var key = NormalizeIngredient(ing);
                    if (!candidateIngredients.TryGetValue(key, out var entry))
                    {
                        entry = (ing, new List<int>());
                        candidateIngredients[key] = entry;
                    }
                    if (!entry.MedicineIds.Contains(line.MedicineId)) entry.MedicineIds.Add(line.MedicineId);
                }
            }

            // 1) Dị ứng - khớp trực tiếp hoạt chất + khớp theo nhóm chéo phản ứng
            if (!string.IsNullOrWhiteSpace(patient.DiUng))
            {
                foreach (var kv in candidateIngredients)
                {
                    var key = kv.Key;
                    var (display, medicineIdsForIngredient) = kv.Value;
                    var medicineId = medicineIdsForIngredient.First();

                    if (VietnameseTextHelper.ContainsIgnoreCase(patient.DiUng, display))
                    {
                        result.Warnings.Add(new PrescriptionSafetyWarning
                        {
                            Tier = SafetyTier.HardBlock,
                            Category = "DiUngTrucTiep",
                            Message = $"Bệnh nhân có khai báo dị ứng khớp trực tiếp với hoạt chất \"{display}\".",
                            RelatedMedicineIdA = medicineId,
                            RequiresAcknowledgement = true
                        });
                        continue;
                    }

                    foreach (var group in allergyGroups)
                    {
                        var isMember = group.ThanhVien.Any(m => NormalizeIngredient(m.HoatChat) == key);
                        if (!isMember) continue;

                        var crossMatch = group.ThanhVien.FirstOrDefault(m =>
                            NormalizeIngredient(m.HoatChat) != key &&
                            VietnameseTextHelper.ContainsIgnoreCase(patient.DiUng, m.HoatChat));
                        if (crossMatch != null)
                        {
                            result.Warnings.Add(new PrescriptionSafetyWarning
                            {
                                Tier = SafetyTier.HardBlock,
                                Category = "DiUngCheo",
                                Message = $"Bệnh nhân dị ứng \"{crossMatch.HoatChat}\" - cùng nhóm \"{group.TenNhom}\" với hoạt chất \"{display}\" đang kê (nguy cơ dị ứng chéo).",
                                RelatedMedicineIdA = medicineId,
                                RequiresAcknowledgement = true
                            });
                        }
                    }
                }
            }

            // 2) Tương tác thuốc - trong đơn hiện tại VÀ với các đơn đang hiệu lực khác.
            // Bỏ qua cặp mà cả 2 vế đều là thuốc đã có từ trước (không cần báo lại).
            var everyIngredientKey = candidateIngredients.Keys
                .Concat(activeIngredients.SelectMany(SplitIngredients).Select(NormalizeIngredient))
                .Distinct()
                .ToList();

            for (var i = 0; i < everyIngredientKey.Count; i++)
            {
                for (var j = i + 1; j < everyIngredientKey.Count; j++)
                {
                    var a = everyIngredientKey[i];
                    var b = everyIngredientKey[j];
                    if (!candidateIngredients.ContainsKey(a) && !candidateIngredients.ContainsKey(b)) continue;

                    var match = interactions.FirstOrDefault(x =>
                        (NormalizeIngredient(x.HoatChatA) == a && NormalizeIngredient(x.HoatChatB) == b) ||
                        (NormalizeIngredient(x.HoatChatA) == b && NormalizeIngredient(x.HoatChatB) == a));
                    if (match == null) continue;

                    var tier = match.MucDoTuongTac switch
                    {
                        "ChongChiDinh" => SafetyTier.HardBlock,
                        "NghiemTrong" => SafetyTier.MustConfirm,
                        _ => SafetyTier.SoftNudge
                    };

                    result.Warnings.Add(new PrescriptionSafetyWarning
                    {
                        Tier = tier,
                        Category = $"TuongTac{match.MucDoTuongTac}",
                        Message = $"Tương tác {DescribeMucDo(match.MucDoTuongTac)} giữa \"{match.HoatChatA}\" và \"{match.HoatChatB}\": {match.MoTa} (dữ liệu minh họa, chưa thẩm định dược lý lâm sàng).",
                        RelatedMedicineIdA = candidateIngredients.TryGetValue(a, out var entryA) ? entryA.MedicineIds.First() : (int?)null,
                        RelatedMedicineIdB = candidateIngredients.TryGetValue(b, out var entryB) ? entryB.MedicineIds.First() : (int?)null,
                        RequiresAcknowledgement = tier != SafetyTier.SoftNudge
                    });
                }
            }

            // 3) Trùng hoạt chất giữa 2 biệt dược khác nhau trong cùng đơn
            foreach (var kv in candidateIngredients)
            {
                var (display, medicineIdsForIngredient) = kv.Value;
                if (medicineIdsForIngredient.Count > 1)
                {
                    result.Warnings.Add(new PrescriptionSafetyWarning
                    {
                        Tier = SafetyTier.MustConfirm,
                        Category = "TrungHoatChat",
                        Message = $"Có {medicineIdsForIngredient.Count} thuốc khác nhau trong đơn cùng chứa hoạt chất \"{display}\" - nguy cơ quá liều do trùng lặp.",
                        RelatedMedicineIdA = medicineIdsForIngredient[0],
                        RelatedMedicineIdB = medicineIdsForIngredient[1],
                        RequiresAcknowledgement = true
                    });
                }
            }

            // 4) Vượt liều tối đa/ngày + ngưỡng ước lượng theo cân nặng bệnh nhi
            var ageYears = (DateTime.Today - patient.NgaySinh).TotalDays / 365.25;
            foreach (var line in context.CandidateLines)
            {
                if (!medicines.TryGetValue(line.MedicineId, out var medicine)) continue;
                if (!line.LieuMoiLan.HasValue || !line.SoLanMoiNgay.HasValue) continue;

                var dailyUnits = line.LieuMoiLan.Value * line.SoLanMoiNgay.Value;

                if (medicine.LieuToiDaMoiNgay.HasValue && dailyUnits > medicine.LieuToiDaMoiNgay.Value)
                {
                    result.Warnings.Add(new PrescriptionSafetyWarning
                    {
                        Tier = SafetyTier.MustConfirm,
                        Category = "VuotLieuToiDa",
                        Message = $"\"{medicine.TenThuoc}\": liều {dailyUnits} {medicine.DonViTinh}/ngày vượt ngưỡng tối đa khuyến nghị ({medicine.LieuToiDaMoiNgay} {medicine.DonViTinh}/ngày).",
                        RelatedMedicineIdA = medicine.Id,
                        RequiresAcknowledgement = true
                    });
                }

                if (ageYears < PediatricAgeThreshold && context.CanNangKg.HasValue && medicine.LieuToiDaMoiNgayTheoKg.HasValue)
                {
                    var maxForWeight = medicine.LieuToiDaMoiNgayTheoKg.Value * context.CanNangKg.Value;
                    if (dailyUnits > maxForWeight)
                    {
                        result.Warnings.Add(new PrescriptionSafetyWarning
                        {
                            Tier = SafetyTier.MustConfirm,
                            Category = "VuotLieuTheoCan",
                            Message = $"\"{medicine.TenThuoc}\": liều {dailyUnits} {medicine.DonViTinh}/ngày vượt ngưỡng ước lượng theo cân nặng bệnh nhi ({medicine.LieuToiDaMoiNgayTheoKg} x {context.CanNangKg} = {maxForWeight:0.##} {medicine.DonViTinh}/ngày - ước lượng theo số đơn vị, không phải tính mg/kg thật).",
                            RelatedMedicineIdA = medicine.Id,
                            RequiresAcknowledgement = true
                        });
                    }
                }
            }

            return result;
        }

        private static string DescribeMucDo(string mucDo) => mucDo switch
        {
            "ChongChiDinh" => "mức chống chỉ định",
            "NghiemTrong" => "mức nghiêm trọng",
            "TrungBinh" => "mức trung bình",
            "Nhe" => "mức nhẹ",
            _ => mucDo
        };

        private static IEnumerable<string> SplitIngredients(string? hoatChat)
        {
            if (string.IsNullOrWhiteSpace(hoatChat)) yield break;
            foreach (var part in hoatChat.Split(new[] { ',', '/', '+' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = part.Trim();
                if (trimmed.Length > 0) yield return trimmed;
            }
        }

        private static string NormalizeIngredient(string ingredient) =>
            VietnameseTextHelper.RemoveDiacritics(ingredient).Trim().ToLowerInvariant();
    }
}
