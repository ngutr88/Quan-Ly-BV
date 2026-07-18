using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Models;

namespace QuanLyBenhVien.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Doctor> Doctors { get; set; } = null!;
        public DbSet<Patient> Patients { get; set; } = null!;
        public DbSet<Department> Departments { get; set; } = null!;
        public DbSet<Service> Services { get; set; } = null!;
        public DbSet<Appointment> Appointments { get; set; } = null!;
        public DbSet<ExaminationRecord> ExaminationRecords { get; set; } = null!;
        public DbSet<Prescription> Prescriptions { get; set; } = null!;
        public DbSet<PrescriptionDetail> PrescriptionDetails { get; set; } = null!;
        public DbSet<Medicine> Medicines { get; set; } = null!;
        public DbSet<MedicineBatch> MedicineBatches { get; set; } = null!;
        public DbSet<Invoice> Invoices { get; set; } = null!;
        public DbSet<InvoiceDetail> InvoiceDetails { get; set; } = null!;
        public DbSet<Review> Reviews { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<Dependent> Dependents { get; set; } = null!;
        public DbSet<DoctorWorkSchedule> DoctorWorkSchedules { get; set; } = null!;
        public DbSet<PatientDocument> PatientDocuments { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User - Doctor (One-to-One)
            modelBuilder.Entity<User>()
                .HasOne(u => u.DoctorProfile)
                .WithOne(d => d.User)
                .HasForeignKey<Doctor>(d => d.NguoiDungId)
                .OnDelete(DeleteBehavior.Cascade);

            // User - Patient (One-to-One)
            modelBuilder.Entity<User>()
                .HasOne(u => u.PatientProfile)
                .WithOne(p => p.User)
                .HasForeignKey<Patient>(p => p.NguoiDungId)
                .OnDelete(DeleteBehavior.Cascade);

            // Doctor - Department (Many-to-One)
            modelBuilder.Entity<Doctor>()
                .HasOne(d => d.Department)
                .WithMany()
                .HasForeignKey(d => d.KhoaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Doctor - WorkSchedule (One-to-Many)
            modelBuilder.Entity<DoctorWorkSchedule>()
                .HasOne(s => s.Doctor)
                .WithMany(d => d.WorkSchedules)
                .HasForeignKey(s => s.BacSiId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DoctorWorkSchedule>()
                .HasIndex(s => new { s.BacSiId, s.ThuTrongTuan, s.GioBatDau, s.GioKetThuc })
                .IsUnique();

            // Service - Department (Many-to-One)
            modelBuilder.Entity<Service>()
                .HasOne(s => s.Department)
                .WithMany()
                .HasForeignKey(s => s.KhoaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Appointment - Patient (Many-to-One)
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany()
                .HasForeignKey(a => a.BenhNhanId)
                .OnDelete(DeleteBehavior.Restrict);

            // Appointment - Doctor (Many-to-One)
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany()
                .HasForeignKey(a => a.BacSiId)
                .OnDelete(DeleteBehavior.SetNull);

            // ExaminationRecord - Appointment (One-to-One)
            modelBuilder.Entity<ExaminationRecord>()
                .HasOne(e => e.Appointment)
                .WithMany()
                .HasForeignKey(e => e.LichKhamId)
                .OnDelete(DeleteBehavior.Cascade);

            // Prescription - ExaminationRecord (One-to-One)
            modelBuilder.Entity<Prescription>()
                .HasOne(p => p.ExaminationRecord)
                .WithMany()
                .HasForeignKey(p => p.PhieuKhamId)
                .OnDelete(DeleteBehavior.Cascade);

            // PrescriptionDetail - Prescription (Many-to-One)
            modelBuilder.Entity<PrescriptionDetail>()
                .HasOne(pd => pd.Prescription)
                .WithMany(p => p.PrescriptionDetails)
                .HasForeignKey(pd => pd.DonThuocId)
                .OnDelete(DeleteBehavior.Cascade);

            // PrescriptionDetail - Medicine (Many-to-One)
            modelBuilder.Entity<PrescriptionDetail>()
                .HasOne(pd => pd.Medicine)
                .WithMany()
                .HasForeignKey(pd => pd.ThuocId)
                .OnDelete(DeleteBehavior.Restrict);

            // MedicineBatch - Medicine (Many-to-One)
            modelBuilder.Entity<MedicineBatch>()
                .HasOne(mb => mb.Medicine)
                .WithMany(m => m.LoThuocs)
                .HasForeignKey(mb => mb.ThuocId)
                .OnDelete(DeleteBehavior.Cascade);

            // Invoice - ExaminationRecord (One-to-One)
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.ExaminationRecord)
                .WithMany()
                .HasForeignKey(i => i.PhieuKhamId)
                .OnDelete(DeleteBehavior.Cascade);

            // InvoiceDetail - Invoice (Many-to-One)
            modelBuilder.Entity<InvoiceDetail>()
                .HasOne(id => id.Invoice)
                .WithMany(i => i.InvoiceDetails)
                .HasForeignKey(id => id.HoaDonId)
                .OnDelete(DeleteBehavior.Cascade);

            // Review - Patient (Many-to-One)
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Patient)
                .WithMany()
                .HasForeignKey(r => r.BenhNhanId)
                .OnDelete(DeleteBehavior.Restrict);

            // Dependent - Patient (Many-to-One)
            modelBuilder.Entity<Dependent>()
                .HasOne(d => d.Patient)
                .WithMany(p => p.Dependents)
                .HasForeignKey(d => d.BenhNhanId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PatientDocument>()
                .HasOne(d => d.Patient)
                .WithMany()
                .HasForeignKey(d => d.BenhNhanId)
                .OnDelete(DeleteBehavior.Cascade);

            // Review - Doctor (Many-to-One)
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Doctor)
                .WithMany()
                .HasForeignKey(r => r.BacSiId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
