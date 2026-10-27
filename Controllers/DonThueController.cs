using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThueVayDuTiec.Web.Models;
using ThueVayDuTiec.Web.Data;
using System.Security.Claims;
using ThueVayDuTiec.Web.ViewModels;

namespace ThueVayDuTiec.Web.Controllers;

[Authorize]
public class DonThueController : Controller
{
    private readonly UngDungDbContext _duLieu;
    private readonly IConfiguration _cauHinh;
    private readonly IWebHostEnvironment _moiTruong;

    public DonThueController(UngDungDbContext duLieu, IConfiguration cauHinh, IWebHostEnvironment moiTruong)
    {
        _duLieu = duLieu;
        _cauHinh = cauHinh;
        _moiTruong = moiTruong;
    }

    [HttpGet]
    public async Task<IActionResult> DatThue(int id, CancellationToken cancellationToken)
    {
        var nguoiDung = await _duLieu.Users.FirstOrDefaultAsync(
            u => u.Id == User.FindFirstValue(ClaimTypes.NameIdentifier), cancellationToken);
        if (nguoiDung is null)
            return Challenge();

        var vay = await _duLieu.VayDuTiec.FirstOrDefaultAsync(v => v.MaVay == id && v.DangKinhDoanh);
        if (vay == null) return NotFound();

        GanThongTinThanhVien(nguoiDung);
        GanThongTinGiaVay(vay);
        await GanLichThueVayAsync(vay.MaVay, cancellationToken);
        var yeuCau = new YeuCauDatThue
        {
            MaVay = vay.MaVay,
            NgayBatDauThue = DateTime.Today.AddDays(1),
            NgayKetThucThue = DateTime.Today.AddDays(2)
        };
        GanThongTinQRDatThue(vay, yeuCau);
        return View(yeuCau);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(9_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 9_000_000)]
    public async Task<IActionResult> DatThue(YeuCauDatThue yeuCau, CancellationToken cancellationToken)
    {
        var nguoiDung = await _duLieu.Users.FirstOrDefaultAsync(
            u => u.Id == User.FindFirstValue(ClaimTypes.NameIdentifier), cancellationToken);
        if (nguoiDung is null)
            return Challenge();
        GanThongTinThanhVien(nguoiDung);

        if (string.IsNullOrWhiteSpace(nguoiDung.HoTen) || string.IsNullOrWhiteSpace(nguoiDung.Email)
            || string.IsNullOrWhiteSpace(nguoiDung.PhoneNumber))
        {
            TempData["ThongBaoLoi"] = "Vui lòng bổ sung họ tên và số điện thoại trong hồ sơ trước khi đặt thuê.";
            return RedirectToAction("HoSo", "TaiKhoan");
        }

        var vay = await _duLieu.VayDuTiec.FirstOrDefaultAsync(v => v.MaVay == yeuCau.MaVay && v.DangKinhDoanh);
        if (vay == null) return NotFound();
        GanThongTinGiaVay(vay);
        GanThongTinQRDatThue(vay, yeuCau);
        var duoiAnhChuyenKhoan = await KiemTraAnhChuyenKhoanAsync(yeuCau.AnhChuyenKhoan, cancellationToken);
        var lichDaDat = await LayLichThueVayAsync(vay.MaVay, cancellationToken);
        ViewBag.LichDaDat = lichDaDat;

        if (yeuCau.NgayBatDauThue.Date < DateTime.Today)
            ModelState.AddModelError(nameof(yeuCau.NgayBatDauThue), "Ngày bắt đầu thuê không được ở trong quá khứ.");

        if (yeuCau.NgayKetThucThue.Date < yeuCau.NgayBatDauThue.Date)
            ModelState.AddModelError(nameof(yeuCau.NgayKetThucThue), "Ngày trả phải bằng hoặc sau ngày bắt đầu thuê.");

        if (ModelState.IsValid)
        {
            // Khóa ngày thuê, ngày trả và thêm một ngày để tiệm xử lý váy.
            bool biTrungLich = lichDaDat.Any(khoang =>
                yeuCau.NgayBatDauThue.Date <= khoang.NgayXuLy.Date
                && yeuCau.NgayKetThucThue.Date >= khoang.NgayBatDau.Date);

            if (biTrungLich)
            {
                ViewBag.ThongBaoDatTrungLich = "Váy đã có lịch thuê hoặc đang trong ngày tiệm xử lý. Lịch mới nhất đã được hiển thị; vui lòng chọn ngày khác.";
                ModelState.AddModelError("", "Váy đã có lịch thuê hoặc đang trong ngày tiệm xử lý. Vui lòng xem lịch bận và chọn ngày khác.");
            }
        }

        if (!ModelState.IsValid)
        {
            GanThongTinThanhVien(nguoiDung);
            return View(yeuCau);
        }

        await using var giaoDich = await _duLieu.Database.BeginTransactionAsync(cancellationToken);
        string? tenAnhTam = null;
        string? tenAnhCuoi = null;
        var maDonThueMoi = 0;
        try
        {
            // Khóa theo váy trong phạm vi giao dịch. Các yêu cầu đặt cùng váy
            // phải tuần tự kiểm tra lịch trước khi được phép tạo đơn.
            var ketQuaKhoa = (await _duLieu.Database.SqlQuery<int>($"""
                DECLARE @ketQua INT;
                EXEC @ketQua = sys.sp_getapplock
                    @Resource = {"ThueVayDuTiec:DatThue:Vay:" + vay.MaVay},
                    @LockMode = 'Exclusive',
                    @LockOwner = 'Transaction',
                    @LockTimeout = 10000;
                SELECT @ketQua AS [Value];
                """).ToListAsync(cancellationToken))[0];

            if (ketQuaKhoa < 0)
            {
                await giaoDich.RollbackAsync(cancellationToken);
                ViewBag.ThongBaoDatTrungLich = "Hệ thống đang xử lý nhiều yêu cầu thuê váy này. Vui lòng thử lại.";
                ViewBag.LichDaDat = await LayLichThueVayAsync(vay.MaVay, cancellationToken);
                GanThongTinThanhVien(nguoiDung);
                return View(yeuCau);
            }

            // Đọc lịch mới sau khi đã lấy khóa, tránh dùng kết quả kiểm tra cũ
            // ở đầu request khi một khách khác vừa tạo đơn.
            lichDaDat = await LayLichThueVayAsync(vay.MaVay, cancellationToken);
            bool biTrungLichLucGhi = lichDaDat.Any(khoang =>
                yeuCau.NgayBatDauThue.Date <= khoang.NgayXuLy.Date
                && yeuCau.NgayKetThucThue.Date >= khoang.NgayBatDau.Date);

            if (biTrungLichLucGhi)
            {
                await giaoDich.RollbackAsync(cancellationToken);
                ViewBag.LichDaDat = lichDaDat;
                ViewBag.ThongBaoDatTrungLich = "Váy vừa được khách khác đặt vào khoảng ngày này. Lịch đã được cập nhật; vui lòng chọn ngày khác.";
                GanThongTinThanhVien(nguoiDung);
                return View(yeuCau);
            }

            int soNgay = (yeuCau.NgayKetThucThue.Date - yeuCau.NgayBatDauThue.Date).Days + 1;
            tenAnhTam = await LuuAnhChuyenKhoanAsync(
                yeuCau.AnhChuyenKhoan!, duoiAnhChuyenKhoan!, cancellationToken);
            var donThue = new DonThue
            {
                NguoiDungId = nguoiDung.Id,
                HoTenKhach = nguoiDung.HoTen,
                Email = nguoiDung.Email,
                SoDienThoai = nguoiDung.PhoneNumber,
                NgayTao = DateTime.Now,
                NgayBatDauThue = yeuCau.NgayBatDauThue.Date,
                NgayKetThucThue = yeuCau.NgayKetThucThue.Date,
                TongTien = vay.GiaThueMoiNgay * soNgay,
                TrangThai = "ChoXacNhan",
                DanhSachChiTiet = new List<ChiTietDonThue>
                {
                    new()
                    {
                        MaVay = vay.MaVay,
                        SoNgayThue = soNgay,
                        DonGia = vay.GiaThueMoiNgay
                    }
                }
            };

            _duLieu.DonThue.Add(donThue);
            await _duLieu.SaveChangesAsync(cancellationToken);
            maDonThueMoi = donThue.MaDonThue;
            tenAnhCuoi = DoiTenAnhTheoMaDon(tenAnhTam, donThue.MaDonThue);
            await giaoDich.CommitAsync(cancellationToken);
        }
        catch
        {
            await giaoDich.RollbackAsync(CancellationToken.None);
            if (tenAnhTam is not null) XoaAnhChuyenKhoan(tenAnhTam);
            if (tenAnhCuoi is not null) XoaAnhChuyenKhoan(tenAnhCuoi);
            throw;
        }
        return RedirectToAction(nameof(XacNhan), new { id = maDonThueMoi });
    }

    public async Task<IActionResult> XacNhan(int id, CancellationToken cancellationToken)
    {
        IQueryable<DonThue> truyVan = _duLieu.DonThue
            .Include(d => d.DanhSachChiTiet)
            .ThenInclude(c => c.Vay)
            .Include(d => d.TheoDoi)
            .AsQueryable();
        if (!User.IsInRole(Services.TenVaiTro.QuanTriVien))
        {
            var nguoiDungId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            truyVan = truyVan.Where(d => d.NguoiDungId == nguoiDungId);
        }

        var donThue = await truyVan.FirstOrDefaultAsync(d => d.MaDonThue == id, cancellationToken);
        if (donThue == null) return NotFound();
        return View(donThue);
    }

    [HttpGet]
    public async Task<IActionResult> CuaToi(CancellationToken cancellationToken)
    {
        var nguoiDungId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var donThue = await _duLieu.DonThue
            .Where(d => d.NguoiDungId == nguoiDungId)
            .Include(d => d.DanhSachChiTiet)
            .ThenInclude(c => c.Vay)
            .Include(d => d.TheoDoi)
            .OrderByDescending(d => d.NgayTao)
            .ToListAsync(cancellationToken);
        return View(donThue);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Huy(int id, CancellationToken cancellationToken)
    {
        var nguoiDungId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var donThue = await _duLieu.DonThue.FirstOrDefaultAsync(
            d => d.MaDonThue == id && d.NguoiDungId == nguoiDungId, cancellationToken);
        if (donThue is null)
            return NotFound();

        if (donThue.TrangThai != "ChoXacNhan")
        {
            TempData["ThongBaoLoi"] = "Chỉ có thể hủy đơn đang chờ xác nhận.";
            return RedirectToAction(nameof(CuaToi));
        }

        donThue.TrangThai = "DaHuy";
        await _duLieu.SaveChangesAsync(cancellationToken);
        TempData["ThongBao"] = "Đơn thuê đã được hủy.";
        return RedirectToAction(nameof(CuaToi));
    }

    private void GanThongTinThanhVien(NguoiDung nguoiDung)
    {
        ViewBag.HoTenKhach = nguoiDung.HoTen;
        ViewBag.EmailKhach = nguoiDung.Email;
        ViewBag.SoDienThoaiKhach = nguoiDung.PhoneNumber;
    }

    private void GanThongTinGiaVay(VayDuTiec vay)
    {
        ViewBag.GiaThueMoiNgay = vay.GiaThueMoiNgay;
        ViewBag.TienDatCoc = vay.TienDatCoc;
        ViewBag.TenVay = vay.TenVay;
    }

    private async Task GanLichThueVayAsync(int maVay, CancellationToken cancellationToken)
    {
        ViewBag.LichDaDat = await LayLichThueVayAsync(maVay, cancellationToken);
    }

    private async Task<List<KhoangLichThueViewModel>> LayLichThueVayAsync(
        int maVay,
        CancellationToken cancellationToken)
    {
        var khoangThue = await _duLieu.ChiTietDonThue
            .Where(c => c.MaVay == maVay && c.DonThue != null && c.DonThue.TrangThai != "DaHuy" && c.DonThue.TrangThai != "KhongDenNhan")
            .Select(c => new
            {
                c.DonThue!.NgayBatDauThue,
                c.DonThue.NgayKetThucThue
            })
            .OrderBy(c => c.NgayBatDauThue)
            .ToListAsync(cancellationToken);

        return khoangThue.Select(khoang => new KhoangLichThueViewModel
        {
            NgayBatDau = khoang.NgayBatDauThue.Date,
            NgayKetThuc = khoang.NgayKetThucThue.Date,
            NgayXuLy = khoang.NgayKetThucThue.Date.AddDays(1)
        }).ToList();
    }

    private void GanThongTinQRDatThue(VayDuTiec vay, YeuCauDatThue yeuCau)
    {
        var maNganHang = _cauHinh["ThanhToan:MaNganHang"]?.Trim();
        var soTaiKhoan = _cauHinh["ThanhToan:SoTaiKhoan"]?.Trim();
        var tenChuTaiKhoan = _cauHinh["ThanhToan:TenChuTaiKhoan"]?.Trim();
        if (string.IsNullOrWhiteSpace(maNganHang)
            || string.IsNullOrWhiteSpace(soTaiKhoan)
            || string.IsNullOrWhiteSpace(tenChuTaiKhoan))
        {
            return;
        }

        var soTien = vay.TienDatCoc;
        var noiDung = $"COC VAY M{vay.MaVay} {yeuCau.NgayBatDauThue:yyyyMMdd} {yeuCau.NgayKetThucThue:yyyyMMdd}";
        ViewBag.MaNganHangQR = maNganHang;
        ViewBag.SoTaiKhoanQR = soTaiKhoan;
        ViewBag.TenChuTaiKhoanQR = tenChuTaiKhoan;
        ViewBag.SoTienQR = soTien;
        ViewBag.NoiDungQR = noiDung;
        ViewBag.MaQRThanhToan = TaoDuongDanQR(maNganHang, soTaiKhoan, tenChuTaiKhoan, soTien, noiDung);
    }

    private static string TaoDuongDanQR(string maNganHang, string soTaiKhoan, string tenChuTaiKhoan, decimal soTien, string noiDung)
    {
        return $"https://img.vietqr.io/image/{Uri.EscapeDataString(maNganHang)}-{Uri.EscapeDataString(soTaiKhoan)}-compact2.svg?amount={soTien:0}&addInfo={Uri.EscapeDataString(noiDung)}&accountName={Uri.EscapeDataString(tenChuTaiKhoan)}";
    }

    private async Task<string?> KiemTraAnhChuyenKhoanAsync(IFormFile? tepAnh, CancellationToken cancellationToken)
    {
        if (tepAnh is null || tepAnh.Length == 0)
            return null; // ModelState.Required cung cấp thông báo thiếu ảnh.
        if (tepAnh.Length > 8 * 1024 * 1024)
        {
            ModelState.AddModelError(nameof(YeuCauDatThue.AnhChuyenKhoan), "Ảnh chuyển khoản không được vượt quá 8 MB.");
            return null;
        }

        var dauTep = new byte[12];
        await using var luong = tepAnh.OpenReadStream();
        var soByte = await luong.ReadAsync(dauTep.AsMemory(), cancellationToken);
        var duLieu = dauTep.AsSpan(0, soByte);
        if (duLieu.Length >= 3 && duLieu[0] == 0xFF && duLieu[1] == 0xD8 && duLieu[2] == 0xFF)
            return ".jpg";
        if (duLieu.Length >= 8 && duLieu[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            return ".png";
        if (duLieu.Length >= 12 && duLieu[..4].SequenceEqual("RIFF"u8) && duLieu[8..12].SequenceEqual("WEBP"u8))
            return ".webp";

        ModelState.AddModelError(nameof(YeuCauDatThue.AnhChuyenKhoan), "Tệp không phải ảnh JPG, PNG hoặc WEBP hợp lệ.");
        return null;
    }

    private async Task<string> LuuAnhChuyenKhoanAsync(IFormFile tepAnh, string duoiTep, CancellationToken cancellationToken)
    {
        var tenTep = $"{Guid.NewGuid():N}{duoiTep}";
        var thuMuc = Path.Combine(_moiTruong.ContentRootPath, "App_Data", "ChungTuThanhToan");
        Directory.CreateDirectory(thuMuc);
        var duongDan = Path.Combine(thuMuc, tenTep);
        try
        {
            await using var tepDich = new FileStream(duongDan, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            await tepAnh.CopyToAsync(tepDich, cancellationToken);
        }
        catch
        {
            XoaAnhChuyenKhoan(tenTep);
            throw;
        }
        return tenTep;
    }

    private string DoiTenAnhTheoMaDon(string tenTepTam, int maDon)
    {
        var thuMuc = Path.Combine(_moiTruong.ContentRootPath, "App_Data", "ChungTuThanhToan");
        var tenTepCuoi = $"{maDon}-{tenTepTam}";
        System.IO.File.Move(Path.Combine(thuMuc, tenTepTam), Path.Combine(thuMuc, tenTepCuoi));
        return tenTepCuoi;
    }

    private void XoaAnhChuyenKhoan(string tenTep)
    {
        if (Path.GetFileName(tenTep) != tenTep) return;
        var duongDan = Path.Combine(_moiTruong.ContentRootPath, "App_Data", "ChungTuThanhToan", tenTep);
        if (System.IO.File.Exists(duongDan)) System.IO.File.Delete(duongDan);
    }

}
