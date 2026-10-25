using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ThueVayDuTiec.Web.Models;

namespace ThueVayDuTiec.Web.Data;

public class UngDungDbContext : IdentityDbContext<NguoiDung>
{
    public UngDungDbContext(DbContextOptions<UngDungDbContext> tuyChon) : base(tuyChon) { }

    public DbSet<DanhMucVay> DanhMucVay => Set<DanhMucVay>();
    public DbSet<VayDuTiec> VayDuTiec => Set<VayDuTiec>();
    public DbSet<HinhAnhVay> HinhAnhVay => Set<HinhAnhVay>();
    public DbSet<DonThue> DonThue => Set<DonThue>();
    public DbSet<TheoDoiDonThue> TheoDoiDonThue => Set<TheoDoiDonThue>();
    public DbSet<ChiTietDonThue> ChiTietDonThue => Set<ChiTietDonThue>();

    protected override void OnModelCreating(ModelBuilder boMau)
    {
        base.OnModelCreating(boMau);
        boMau.Entity<IdentityRole>(vaiTro =>
        {
            vaiTro.ToTable("VaiTro");
            vaiTro.Property(v => v.Id).HasColumnName("MaVT").HasMaxLength(50);
            vaiTro.Property(v => v.Name).HasColumnName("TenVT").HasMaxLength(50);
            vaiTro.Property(v => v.NormalizedName).HasColumnName("TenVTCh").HasMaxLength(50);
            vaiTro.Property(v => v.ConcurrencyStamp).HasColumnName("MaDB").HasMaxLength(36);
            vaiTro.HasIndex(v => v.NormalizedName)
                .HasDatabaseName("CM_VT_TenCh")
                .IsUnique()
                .HasFilter("[TenVTCh] IS NOT NULL");
        });
        boMau.Entity<NguoiDung>(nguoiDung =>
        {
            nguoiDung.ToTable("NguoiDung");
            nguoiDung.Property(n => n.Id).HasColumnName("MaND").HasMaxLength(50);
            nguoiDung.Property(n => n.HoTen).HasColumnName("HoTen");
            nguoiDung.Property(n => n.UserName).HasColumnName("TenDN").HasMaxLength(150);
            nguoiDung.Property(n => n.NormalizedUserName).HasColumnName("TenDNCh").HasMaxLength(150);
            nguoiDung.Property(n => n.Email).HasColumnName("ThuDT").HasMaxLength(150);
            nguoiDung.Property(n => n.NormalizedEmail).HasColumnName("ThuDTCh").HasMaxLength(150);
            nguoiDung.Property(n => n.EmailConfirmed).HasColumnName("ThuDTXN");
            nguoiDung.Property(n => n.PasswordHash).HasColumnName("MatKhauBam").HasMaxLength(100);
            nguoiDung.Property(n => n.SecurityStamp).HasColumnName("MaBM").HasMaxLength(36);
            nguoiDung.Property(n => n.ConcurrencyStamp).HasColumnName("MaDB").HasMaxLength(36);
            nguoiDung.Property(n => n.PhoneNumber).HasColumnName("SDT").HasMaxLength(30);
            nguoiDung.Property(n => n.PhoneNumberConfirmed).HasColumnName("SDTXN");
            nguoiDung.Property(n => n.TwoFactorEnabled).HasColumnName("XacThuc2Y");
            nguoiDung.Property(n => n.LockoutEnd).HasColumnName("KhoaDen");
            nguoiDung.Property(n => n.LockoutEnabled).HasColumnName("ChoKhoa");
            nguoiDung.Property(n => n.AccessFailedCount).HasColumnName("LanSai");
            nguoiDung.HasIndex(n => n.NormalizedUserName)
                .HasDatabaseName("CM_ND_TenDNCh")
                .IsUnique()
                .HasFilter("[TenDNCh] IS NOT NULL");
        });
        boMau.Entity<IdentityRoleClaim<string>>(claim =>
        {
            claim.ToTable("TuyenBoVaiTro");
            claim.Property(c => c.Id).HasColumnName("MaTB");
            claim.Property(c => c.RoleId).HasColumnName("MaVT").HasMaxLength(50);
            claim.Property(c => c.ClaimType).HasColumnName("LoaiTB").HasMaxLength(256);
            claim.Property(c => c.ClaimValue).HasColumnName("GTTB").HasMaxLength(1000);
        });
        boMau.Entity<IdentityUserClaim<string>>(claim =>
        {
            claim.ToTable("TuyenBoNguoiDung");
            claim.Property(c => c.Id).HasColumnName("MaTB");
            claim.Property(c => c.UserId).HasColumnName("MaND").HasMaxLength(50);
            claim.Property(c => c.ClaimType).HasColumnName("LoaiTB").HasMaxLength(256);
            claim.Property(c => c.ClaimValue).HasColumnName("GTTB").HasMaxLength(1000);
        });
        boMau.Entity<IdentityUserLogin<string>>(dangNhap =>
        {
            dangNhap.ToTable("DangNhapNgoai");
            dangNhap.Property(d => d.LoginProvider).HasColumnName("NhaCC").HasMaxLength(128);
            dangNhap.Property(d => d.ProviderKey).HasColumnName("KhoaNCC").HasMaxLength(128);
            dangNhap.Property(d => d.ProviderDisplayName).HasColumnName("TenHienThiNCC").HasMaxLength(100);
            dangNhap.Property(d => d.UserId).HasColumnName("MaND").HasMaxLength(50);
        });
        boMau.Entity<IdentityUserRole<string>>(vaiTroNguoiDung =>
        {
            vaiTroNguoiDung.ToTable("VaiTroNguoiDung");
            vaiTroNguoiDung.Property(v => v.UserId).HasColumnName("MaND").HasMaxLength(50);
            vaiTroNguoiDung.Property(v => v.RoleId).HasColumnName("MaVT").HasMaxLength(50);
        });
        boMau.Entity<IdentityUserToken<string>>(token =>
        {
            token.ToTable("MaTruyCapNguoiDung");
            token.Property(t => t.UserId).HasColumnName("MaND").HasMaxLength(50);
            token.Property(t => t.LoginProvider).HasColumnName("NhaCC").HasMaxLength(128);
            token.Property(t => t.Name).HasColumnName("TenMa").HasMaxLength(128);
            token.Property(t => t.Value).HasColumnName("GTMa").HasMaxLength(1000);
        });

        boMau.Entity<DanhMucVay>().ToTable("DanhMucVay");
        boMau.Entity<VayDuTiec>().ToTable("VayDuTiec");
        boMau.Entity<HinhAnhVay>(anh =>
        {
            anh.ToTable("HinhAnhVay");
            anh.HasKey(a => a.MaAnh).HasName("KC_HinhAnhVay");
            anh.Property(a => a.MaAnh).HasColumnName("MaAnh");
            anh.Property(a => a.MaVay).HasColumnName("MaVay");
            anh.Property(a => a.DuongDan).HasColumnName("DuongDan").HasMaxLength(500).IsRequired();
            anh.Property(a => a.AnhChinh).HasColumnName("AnhChinh").HasDefaultValue(false);
            anh.Property(a => a.ThuTu).HasColumnName("ThuTu").HasDefaultValue(0);
            anh.HasIndex(a => new { a.MaVay, a.ThuTu }).HasDatabaseName("CM_HinhAnhVay_ThuTu");
        });
        boMau.Entity<DonThue>().ToTable("DonThue");
        boMau.Entity<TheoDoiDonThue>(theoDoi =>
        {
            theoDoi.ToTable("TheoDoiDonThue");
            theoDoi.HasKey(x => x.MaDonThue).HasName("KC_TheoDoiDonThue");
            theoDoi.Property(x => x.MaDonThue).HasColumnName("MaDon");
            theoDoi.Property(x => x.NgayNhanVay).HasColumnName("NgayNhan");
            theoDoi.Property(x => x.NgayKhongDenNhan).HasColumnName("NgayKhongDenNhan");
            theoDoi.Property(x => x.GhiChuKhongDenNhan).HasColumnName("GhiChuKhongDenNhan").HasMaxLength(500);
            theoDoi.Property(x => x.TienCocMat).HasColumnName("TienCocMat").HasPrecision(18, 2);
            theoDoi.Property(x => x.TienThueDaThu).HasColumnName("TienThueDaThu").HasPrecision(18, 2);
            theoDoi.Property(x => x.NgayThuTienThue).HasColumnName("NgayThuTienThue");
            theoDoi.Property(x => x.NgayKiemTraTra).HasColumnName("NgayKiemTra");
            theoDoi.Property(x => x.TinhTrangVay).HasColumnName("TinhTrang").HasMaxLength(30);
            theoDoi.Property(x => x.GhiChuKiemTra).HasColumnName("GhiChu").HasMaxLength(1000);
            theoDoi.Property(x => x.TienCoc).HasColumnName("TienCoc").HasPrecision(18, 2);
            theoDoi.Property(x => x.TienKhauTru).HasColumnName("TienKhauTru").HasPrecision(18, 2);
            theoDoi.Property(x => x.TienHoanCoc).HasColumnName("TienHoanCoc").HasPrecision(18, 2);
            theoDoi.Property(x => x.HinhThucHoanCoc).HasColumnName("HinhThucHoanCoc").HasMaxLength(30);
            theoDoi.Property(x => x.NgayHoanCoc).HasColumnName("NgayHoanCoc");
            theoDoi.Property(x => x.NguoiThucHienId).HasColumnName("MaNDThucHien").HasMaxLength(50);
        });
        boMau.Entity<ChiTietDonThue>().ToTable("ChiTietDonThue");
        boMau.Entity<DanhMucVay>().Property(d => d.MaDanhMuc).HasColumnName("MaDM");
        boMau.Entity<DanhMucVay>().Property(d => d.TenDanhMuc).HasColumnName("TenDM");
        boMau.Entity<DanhMucVay>().Property(d => d.MoTa).HasMaxLength(500);
        boMau.Entity<VayDuTiec>().Property(v => v.MaDanhMuc).HasColumnName("MaDM");
        boMau.Entity<VayDuTiec>().Property(v => v.GiaThueMoiNgay).HasColumnName("GiaNgay");
        boMau.Entity<VayDuTiec>().Property(v => v.DangKinhDoanh).HasColumnName("DangKD");
        boMau.Entity<DonThue>().Property(d => d.MaDonThue).HasColumnName("MaDon");
        boMau.Entity<DonThue>().Property(d => d.NguoiDungId).HasColumnName("MaND").HasMaxLength(50);
        boMau.Entity<DonThue>().Property(d => d.HoTenKhach).HasColumnName("HoTen");
        boMau.Entity<DonThue>().Property(d => d.Email).HasColumnName("ThuDT");
        boMau.Entity<DonThue>().Property(d => d.SoDienThoai).HasColumnName("SDT");
        boMau.Entity<DonThue>().Property(d => d.NgayBatDauThue).HasColumnName("NgayBD").HasColumnType("date");
        boMau.Entity<DonThue>().Property(d => d.NgayKetThucThue).HasColumnName("NgayKT").HasColumnType("date");
        boMau.Entity<DonThue>().Property(d => d.LyDoHuy).HasMaxLength(500);
        boMau.Entity<ChiTietDonThue>().Property(c => c.MaChiTiet).HasColumnName("MaCT");
        boMau.Entity<ChiTietDonThue>().Property(c => c.MaDonThue).HasColumnName("MaDon");
        boMau.Entity<ChiTietDonThue>().Property(c => c.SoNgayThue).HasColumnName("SoNgay");
        boMau.Entity<NguoiDung>().Property(n => n.HoTen).HasMaxLength(100).IsRequired();
        boMau.Entity<NguoiDung>()
            .HasIndex(n => n.NormalizedEmail)
            .HasDatabaseName("CM_ND_ThuDTCh")
            .IsUnique()
            .HasFilter("[ThuDTCh] IS NOT NULL");

        boMau.Entity<VayDuTiec>()
            .HasOne(v => v.DanhMuc)
            .WithMany(d => d.DanhSachVay)
            .HasForeignKey(v => v.MaDanhMuc);

        boMau.Entity<HinhAnhVay>()
            .HasOne(a => a.Vay)
            .WithMany(v => v.DanhSachHinhAnh)
            .HasForeignKey(a => a.MaVay)
            .OnDelete(DeleteBehavior.Cascade);

        boMau.Entity<ChiTietDonThue>()
            .HasOne(c => c.Vay)
            .WithMany()
            .HasForeignKey(c => c.MaVay);

        boMau.Entity<ChiTietDonThue>()
            .HasOne(c => c.DonThue)
            .WithMany(d => d.DanhSachChiTiet)
            .HasForeignKey(c => c.MaDonThue);

        boMau.Entity<DonThue>()
            .HasOne(d => d.NguoiDung)
            .WithMany()
            .HasForeignKey(d => d.NguoiDungId)
            .OnDelete(DeleteBehavior.Restrict);

        boMau.Entity<TheoDoiDonThue>()
            .HasOne(t => t.DonThue)
            .WithOne(d => d.TheoDoi)
            .HasForeignKey<TheoDoiDonThue>(t => t.MaDonThue)
            .OnDelete(DeleteBehavior.Cascade);

        boMau.Entity<DonThue>().HasIndex(d => d.NguoiDungId).HasDatabaseName("CM_Don_ND");
        boMau.Entity<VayDuTiec>().Property(v => v.GiaThueMoiNgay).HasPrecision(18, 2);
        boMau.Entity<VayDuTiec>().Property(v => v.TienDatCoc).HasPrecision(18, 2);
        boMau.Entity<DonThue>().Property(d => d.TongTien).HasPrecision(18, 2);
        boMau.Entity<ChiTietDonThue>().Property(c => c.DonGia).HasPrecision(18, 2);
    }
}
