using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThueVayDuTiec.Web.Data;
using ThueVayDuTiec.Web.Models;
using ThueVayDuTiec.Web.Services;
using ThueVayDuTiec.Web.ViewModels;
using System.Security.Claims;

namespace ThueVayDuTiec.Web.Areas.QuanTri.Controllers;

[Area("QuanTri")]
[Authorize(Roles = TenVaiTro.QuanTriVien)]
public class DonThueController : Controller
{
    private readonly UngDungDbContext _duLieu;
    private readonly IWebHostEnvironment _moiTruong;

    public DonThueController(UngDungDbContext duLieu, IWebHostEnvironment moiTruong)
    {
        _duLieu = duLieu;
        _moiTruong = moiTruong;
    }

    public async Task<IActionResult> Index(string? q, CancellationToken cancellationToken)
    {
        q = q?.Trim();
        var truyVan = _duLieu.DonThue.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var maDon = int.TryParse(q.Replace("#", "", StringComparison.OrdinalIgnoreCase)
                .Replace("VT-", "", StringComparison.OrdinalIgnoreCase), out var ma) ? ma : (int?)null;
            truyVan = truyVan.Where(d => d.HoTenKhach.Contains(q)
                || d.Email.Contains(q)
                || d.SoDienThoai.Contains(q)
                || d.TrangThai.Contains(q)
                || (maDon.HasValue && d.MaDonThue == maDon.Value));
        }
        ViewBag.TuKhoa = q;
        var donThue = await truyVan
            .Include(d => d.DanhSachChiTiet)
            .ThenInclude(c => c.Vay)
            .OrderByDescending(d => d.NgayTao)
            .ToListAsync(cancellationToken);
        return View(donThue);
    }

    public async Task<IActionResult> ChiTiet(int id, CancellationToken cancellationToken)
    {
        var donThue = await _duLieu.DonThue
            .Include(d => d.DanhSachChiTiet)
            .ThenInclude(c => c.Vay)
            .Include(d => d.TheoDoi)
            .FirstOrDefaultAsync(d => d.MaDonThue == id, cancellationToken);
        if (donThue is null) return NotFound();
        ViewBag.CoAnhChuyenKhoan = TimAnhChuyenKhoan(id) is not null;
        ViewBag.TongTienCoc = donThue.DanhSachChiTiet.Sum(c => c.Vay?.TienDatCoc ?? 0m);
        return View(donThue);
    }

    [HttpGet]
    public async Task<IActionResult> XemAnhChuyenKhoan(int id, CancellationToken cancellationToken)
    {
        var donThue = await _duLieu.DonThue
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.MaDonThue == id, cancellationToken);
        if (donThue is null) return NotFound();
        var tenTep = TimAnhChuyenKhoan(id);
        if (tenTep is null) return NotFound();

        var duoiTep = Path.GetExtension(tenTep).ToLowerInvariant();
        var loaiNoiDung = duoiTep switch
        {
            ".jpg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => null
        };
        if (loaiNoiDung is null) return NotFound();

        var thuMuc = Path.GetFullPath(Path.Combine(_moiTruong.ContentRootPath, "App_Data", "ChungTuThanhToan"));
        var duongDan = Path.GetFullPath(Path.Combine(thuMuc, tenTep));
        if (!duongDan.StartsWith(thuMuc + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || !System.IO.File.Exists(duongDan))
            return NotFound();

        return PhysicalFile(duongDan, loaiNoiDung);
    }

    private string? TimAnhChuyenKhoan(int maDon)
    {
        var thuMuc = Path.GetFullPath(Path.Combine(_moiTruong.ContentRootPath, "App_Data", "ChungTuThanhToan"));
        if (!Directory.Exists(thuMuc)) return null;
        var cacTep = Directory.GetFiles(thuMuc, $"{maDon}-*");
        if (cacTep.Length != 1) return null;
        var tenTep = Path.GetFileName(cacTep[0]);
        var duoiTep = Path.GetExtension(tenTep).ToLowerInvariant();
        return duoiTep is ".jpg" or ".png" or ".webp" ? tenTep : null;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XacNhan(int id, CancellationToken cancellationToken)
    {
        var donThue = await _duLieu.DonThue.FindAsync([id], cancellationToken);
        if (donThue is null)
            return NotFound();
        if (donThue.TrangThai != "ChoXacNhan")
        {
            TempData["ThongBaoLoi"] = "Chỉ có thể xác nhận đơn đang chờ xác nhận.";
            return RedirectToAction(nameof(ChiTiet), new { id });
        }

        donThue.TrangThai = "DaXacNhan";
        await _duLieu.SaveChangesAsync(cancellationToken);
        TempData["ThongBao"] = "Đơn thuê đã được xác nhận.";
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GhiNhanNhanVay(int id, bool daThuTienThue, CancellationToken cancellationToken)
    {
        var donThue = await _duLieu.DonThue
            .Include(d => d.DanhSachChiTiet).ThenInclude(c => c.Vay)
            .Include(d => d.TheoDoi)
            .FirstOrDefaultAsync(d => d.MaDonThue == id, cancellationToken);
        if (donThue is null) return NotFound();
        if (donThue.TrangThai != "DaXacNhan")
        {
            TempData["ThongBaoLoi"] = "Chỉ ghi nhận khách nhận váy sau khi đơn đã được xác nhận.";
            return RedirectToAction(nameof(ChiTiet), new { id });
        }
        if (!daThuTienThue)
        {
            TempData["ThongBaoLoi"] = "Vui lòng xác nhận đã thu tiền thuê khi giao váy cho khách.";
            return RedirectToAction(nameof(ChiTiet), new { id });
        }

        var ngayNhan = DateTime.Now;
        donThue.TrangThai = "DaNhanVay";
        donThue.TheoDoi ??= new TheoDoiDonThue { MaDonThue = id };
        donThue.TheoDoi.NgayNhanVay = ngayNhan;
        donThue.TheoDoi.TienCoc = donThue.DanhSachChiTiet.Sum(c => c.Vay?.TienDatCoc ?? 0m);
        donThue.TheoDoi.TienThueDaThu = donThue.TongTien;
        donThue.TheoDoi.NgayThuTienThue = ngayNhan;
        await _duLieu.SaveChangesAsync(cancellationToken);
        TempData["ThongBao"] = $"Đã ghi nhận khách nhận váy và thu {donThue.TongTien:N0}₫ tiền thuê.";
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HoanTra(int id, HoanTraViewModel model, CancellationToken cancellationToken)
    {
        var donThue = await _duLieu.DonThue
            .Include(d => d.DanhSachChiTiet).ThenInclude(c => c.Vay)
            .Include(d => d.TheoDoi)
            .FirstOrDefaultAsync(d => d.MaDonThue == id, cancellationToken);
        if (donThue is null) return NotFound();
        if (donThue.TrangThai != "DaNhanVay")
        {
            TempData["ThongBaoLoi"] = "Chỉ kiểm tra và hoàn cọc khi khách đã nhận rồi trả váy.";
            return RedirectToAction(nameof(ChiTiet), new { id });
        }

        var tongTienCoc = donThue.DanhSachChiTiet.Sum(c => c.Vay?.TienDatCoc ?? 0m);
        var tinhTrangHopLe = new[] { "Tot", "HaoMonNhe", "HuHong", "MatPhuKien" };
        var hinhThucHopLe = new[] { "TienMat", "ChuyenKhoan" };
        if (!tinhTrangHopLe.Contains(model.TinhTrangVay, StringComparer.Ordinal))
            ModelState.AddModelError(nameof(model.TinhTrangVay), "Vui lòng chọn tình trạng váy trong danh sách.");
        if (!hinhThucHopLe.Contains(model.HinhThucHoanCoc, StringComparer.Ordinal))
            ModelState.AddModelError(nameof(model.HinhThucHoanCoc), "Vui lòng chọn hình thức hoàn cọc.");
        if (model.TienKhauTru < 0)
            ModelState.AddModelError(nameof(model.TienKhauTru), "Khoản khấu trừ không được âm.");
        if (model.TienKhauTru > tongTienCoc)
            ModelState.AddModelError(nameof(model.TienKhauTru), "Khoản khấu trừ không được lớn hơn tiền cọc.");
        if (model.TienKhauTru > 0 && string.IsNullOrWhiteSpace(model.GhiChuKiemTra))
            ModelState.AddModelError(nameof(model.GhiChuKiemTra), "Hãy ghi rõ lý do và hạng mục khấu trừ.");
        if (!model.DaHoanCocNgay)
            ModelState.AddModelError(nameof(model.DaHoanCocNgay), "Xác nhận đã hoàn cọc cho khách ngay khi kiểm tra để hoàn thành đơn.");

        if (!ModelState.IsValid)
        {
            TempData["ThongBaoLoi"] = string.Join(" ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(e => !string.IsNullOrWhiteSpace(e)));
            return RedirectToAction(nameof(ChiTiet), new { id });
        }

        var ngayHoan = DateTime.Now;
        donThue.TheoDoi ??= new TheoDoiDonThue { MaDonThue = id, NgayNhanVay = ngayHoan };
        donThue.TheoDoi.NgayKiemTraTra = ngayHoan;
        donThue.TheoDoi.TinhTrangVay = model.TinhTrangVay;
        donThue.TheoDoi.GhiChuKiemTra = model.GhiChuKiemTra?.Trim();
        donThue.TheoDoi.TienCoc = tongTienCoc;
        donThue.TheoDoi.TienKhauTru = model.TienKhauTru;
        donThue.TheoDoi.TienHoanCoc = tongTienCoc - model.TienKhauTru;
        donThue.TheoDoi.HinhThucHoanCoc = model.HinhThucHoanCoc;
        donThue.TheoDoi.NgayHoanCoc = ngayHoan;
        donThue.TheoDoi.NguoiThucHienId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        donThue.TrangThai = "DaHoanTra";
        await _duLieu.SaveChangesAsync(cancellationToken);

        TempData["ThongBao"] = $"Đã hoàn tất đơn và ghi nhận hoàn cọc {donThue.TheoDoi.TienHoanCoc:N0}₫ ngay khi kiểm tra váy.";
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GhiNhanKhongDenNhan(int id, string? ghiChuKhongDenNhan, CancellationToken cancellationToken)
    {
        var donThue = await _duLieu.DonThue
            .Include(d => d.DanhSachChiTiet).ThenInclude(c => c.Vay)
            .Include(d => d.TheoDoi)
            .FirstOrDefaultAsync(d => d.MaDonThue == id, cancellationToken);
        if (donThue is null) return NotFound();
        if (donThue.TrangThai != "DaXacNhan")
        {
            TempData["ThongBaoLoi"] = "Chỉ ghi nhận khách không đến nhận với đơn đã xác nhận.";
            return RedirectToAction(nameof(ChiTiet), new { id });
        }
        if (string.IsNullOrWhiteSpace(ghiChuKhongDenNhan) || ghiChuKhongDenNhan.Trim().Length > 500)
        {
            TempData["ThongBaoLoi"] = "Vui lòng ghi lý do khách không đến nhận (tối đa 500 ký tự).";
            return RedirectToAction(nameof(ChiTiet), new { id });
        }

        var tienCoc = donThue.DanhSachChiTiet.Sum(c => c.Vay?.TienDatCoc ?? 0m);
        donThue.TheoDoi ??= new TheoDoiDonThue { MaDonThue = id };
        donThue.TheoDoi.NgayKhongDenNhan = DateTime.Now;
        donThue.TheoDoi.GhiChuKhongDenNhan = ghiChuKhongDenNhan.Trim();
        donThue.TheoDoi.TienCoc = tienCoc;
        donThue.TheoDoi.TienCocMat = tienCoc;
        donThue.TrangThai = "KhongDenNhan";
        await _duLieu.SaveChangesAsync(cancellationToken);

        TempData["ThongBao"] = $"Đã ghi nhận khách không đến nhận; tiền cọc {tienCoc:N0}₫ được giữ theo chính sách.";
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Huy(int id, string? lyDoHuy, CancellationToken cancellationToken)
    {
        var donThue = await _duLieu.DonThue.FindAsync([id], cancellationToken);
        if (donThue is null)
            return NotFound();
        if (donThue.TrangThai != "ChoXacNhan")
        {
            TempData["ThongBaoLoi"] = "Chỉ có thể hủy yêu cầu trước khi xác nhận đơn. Đơn đã xác nhận hãy ghi nhận khách nhận váy hoặc không đến nhận.";
            return RedirectToAction(nameof(ChiTiet), new { id });
        }
        if (string.IsNullOrWhiteSpace(lyDoHuy))
        {
            TempData["ThongBaoLoi"] = "Vui lòng ghi lý do hủy đơn, ví dụ khách không đến nhận váy.";
            return RedirectToAction(nameof(ChiTiet), new { id });
        }
        if (lyDoHuy.Trim().Length > 500)
        {
            TempData["ThongBaoLoi"] = "Lý do hủy đơn không được dài quá 500 ký tự.";
            return RedirectToAction(nameof(ChiTiet), new { id });
        }

        donThue.LyDoHuy = lyDoHuy.Trim();
        donThue.TrangThai = "DaHuy";
        await _duLieu.SaveChangesAsync(cancellationToken);
        TempData["ThongBao"] = "Đơn thuê đã được hủy.";
        return RedirectToAction(nameof(ChiTiet), new { id });
    }
}
