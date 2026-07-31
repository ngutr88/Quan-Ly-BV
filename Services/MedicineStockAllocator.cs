using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Models;

namespace QuanLyBenhVien.Services
{
    // Trừ kho thuốc theo FEFO (First Expired, First Out) - tách ra từ
    // ExamController.CompleteSession để dùng chung được cho các luồng kê đơn
    // khác (kê lại, áp mẫu đơn...). Ghi lại chính xác đã trừ từ lô nào vào
    // PrescriptionBatchAllocation để sau này có thể hoàn trả đúng lô (sửa/hủy
    // đơn "Chờ cấp phát").
    public class MedicineStockAllocator
    {
        private readonly ApplicationDbContext _context;

        public MedicineStockAllocator(ApplicationDbContext context)
        {
            _context = context;
        }

        // Trả về false nếu không đủ tồn kho hợp lệ (đơn hàng gọi sẽ tự rollback
        // transaction) - không tự ném exception vì đây là một lỗi nghiệp vụ dự
        // kiến trước (đã kiểm tra sơ bộ ở bước xem trước), không phải lỗi hệ thống.
        public async Task<bool> AllocateFefoAsync(int medicineId, int chiTietDonThuocId, int quantity)
        {
            var medicine = await _context.Medicines.FirstOrDefaultAsync(m => m.Id == medicineId);
            if (medicine == null) return false;

            var activeBatches = await _context.MedicineBatches
                .Where(b => b.ThuocId == medicineId && b.HanSuDung > DateTime.Today && b.SoLuongTon > 0)
                .OrderBy(b => b.HanSuDung) // Lô sắp hết hạn sớm nhất được lấy trước
                .ToListAsync();

            var remaining = quantity;
            foreach (var batch in activeBatches)
            {
                if (remaining <= 0) break;

                var take = Math.Min(batch.SoLuongTon, remaining);
                batch.SoLuongTon -= take;
                remaining -= take;

                _context.PrescriptionBatchAllocations.Add(new PrescriptionBatchAllocation
                {
                    ChiTietDonThuocId = chiTietDonThuocId,
                    LoThuocId = batch.Id,
                    SoLuongLay = take
                });
            }

            if (remaining > 0) return false;

            medicine.TonKho = Math.Max(0, medicine.TonKho - quantity);
            return true;
        }
    }
}
