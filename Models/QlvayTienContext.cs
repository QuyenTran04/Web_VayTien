using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VAYTIEN.Models;

namespace VAYTIEN.Models
{
    public class QlvayTienContext : IdentityDbContext<ApplicationUser>
    {
        public QlvayTienContext(DbContextOptions<QlvayTienContext> options)
            : base(options)
        {
        }

        public virtual DbSet<ChiNhanhNganHang> ChiNhanhNganHangs { get; set; }
        public virtual DbSet<DoiTuongVay> DoiTuongVays { get; set; }
        public virtual DbSet<GiaoDich> GiaoDiches { get; set; }
        public virtual DbSet<HopDongVay> HopDongVays { get; set; }
        public virtual DbSet<KhachHang> KhachHangs { get; set; }
        public virtual DbSet<LichTraNo> LichTraNos { get; set; }
        public virtual DbSet<LoaiTienTe> LoaiTienTes { get; set; }
        public virtual DbSet<LoaiVay> LoaiVays { get; set; }
        public virtual DbSet<NhanVien> NhanViens { get; set; }
        public virtual DbSet<TaiKhoanNganHang> TaiKhoanNganHangs { get; set; }
        public virtual DbSet<TaiSanTheChap> TaiSanTheChaps { get; set; }
        public virtual DbSet<ThanhToanLichTra> ThanhToanLichTras { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Bắt buộc phải gọi base.OnModelCreating để cấu hình các bảng của Identity
            base.OnModelCreating(modelBuilder);

            // Cấu hình chi tiết cho từng bảng bằng Fluent API

            modelBuilder.Entity<KhachHang>(entity =>
            {
                entity.Property(e => e.HoTen).HasMaxLength(100);
                entity.Property(e => e.CmndCccd).HasMaxLength(12);
                entity.Property(e => e.DiaChi).HasMaxLength(255);
                entity.Property(e => e.Sdt).HasMaxLength(15);
                entity.Property(e => e.Email).HasMaxLength(100);
            });

            modelBuilder.Entity<HopDongVay>(entity =>
            {
                entity.Property(e => e.SoTienVay).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.SoTienConLai).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.LaiSuat).HasColumnType("decimal(5, 2)");

                entity.HasOne(d => d.MaKhNavigation)
                    .WithMany(p => p.HopDongVays)
                    .HasForeignKey(d => d.MaKh)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<LichTraNo>(entity =>
            {
                entity.Property(e => e.SoTienGoc).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.SoTienLai).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.SoTienPhaiTra).HasColumnType("decimal(18, 2)");

                entity.HasOne(d => d.MaHopDongNavigation)
                    .WithMany(p => p.LichTraNos)
                    .HasForeignKey(d => d.MaHopDong)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TaiKhoanNganHang>(entity =>
            {
                entity.HasKey(e => e.MaTaiKhoan);
                entity.Property(e => e.SoDu).HasColumnType("decimal(18, 2)");
            });

            modelBuilder.Entity<GiaoDich>(entity =>
            {
                entity.HasKey(e => e.MaGiaoDich);
                entity.Property(e => e.SoTienGd).HasColumnType("decimal(18, 2)");
            });

            modelBuilder.Entity<TaiSanTheChap>(entity =>
            {
                entity.HasKey(e => e.MaTs);
                entity.Property(e => e.GiaTri).HasColumnType("decimal(18, 2)");
            });

            modelBuilder.Entity<DoiTuongVay>(entity =>
            {
                entity.HasKey(e => e.MaDoiTuongVay);
                entity.Property(e => e.LaiSuat).HasColumnType("decimal(5, 2)");
            });

            modelBuilder.Entity<LoaiVay>(entity =>
            {
                entity.HasKey(e => e.MaLoaiVay);
                entity.Property(e => e.LaiSuat).HasColumnType("decimal(5, 2)");
            });

            modelBuilder.Entity<NhanVien>()
                .HasOne(n => n.ApplicationUser)
                .WithOne()
                .HasForeignKey<NhanVien>(n => n.ApplicationUserId);
        }
    }
}
