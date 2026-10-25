using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using ThueVayDuTiec.Web.Data;
using ThueVayDuTiec.Web.Models;
using ThueVayDuTiec.Web.Services;
using ThueVayDuTiec.Web.ViewModels;

namespace ThueVayDuTiec.Web.Areas.QuanTri.Controllers;

[Area("QuanTri")]
[Authorize(Roles = TenVaiTro.QuanTriVien)]
public class VayController : Controller
{
    private readonly UngDungDbContext _duLieu;
    private readonly IWebHostEnvironment _moiTruong;
    private const long DungLuongAnhToiDa = 8 * 1024 * 1024;

    public VayController(UngDungDbContext duLieu, IWebHostEnvironment moiTruong)
    {
        _duLieu = duLieu;
        _moiTruong = moiTruong;
    }

    public async Task<IActionResult> Index(string? q, CancellationToken cancellationToken)
    {
        q = q?.Trim();
        var truyVan = _duLieu.VayDuTiec.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            truyVan = truyVan.Where(v => v.TenVay.Contains(q)
                || v.KichCo.Contains(q)
                || v.MauSac.Contains(q)
                || v.KieuDang.Contains(q)
                || v.DanhMuc!.TenDanhMuc.Contains(q));
        ViewBag.TuKhoa = q;
        var vay = await truyVan
            .Include(v => v.DanhMuc)
            .Include(v => v.DanhSachHinhAnh)
            .OrderBy(v => v.TenVay).ToListAsync(cancellationToken);
        return View(vay);
    }

    [HttpGet]
    public async Task<IActionResult> Them(CancellationToken cancellationToken)
    {
        await NapDanhMuc(cancellationToken);
        return View("BieuMau", new VayQuanTriViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Them(VayQuanTriViewModel model, CancellationToken cancellationToken)
    {
        var tepAnh = model.AnhTaiLen.Where(tep => tep.Length > 0).ToList();
        if (tepAnh.Count > 3)
            ModelState.AddModelError(nameof(model.AnhTaiLen), "Mỗi lần chỉ chọn tối đa 3 ảnh.");
        await KiemTraTepAnhAsync(tepAnh, cancellationToken);
        if (!ModelState.IsValid)
        {
            await NapDanhMuc(cancellationToken);
            return View("BieuMau", model);
        }

        var vay = new VayDuTiec();
        GanDuLieu(vay, model);
        var tepDaLuu = new List<string>();
        await using var giaoDich = await _duLieu.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            _duLieu.VayDuTiec.Add(vay);
            await _duLieu.SaveChangesAsync(cancellationToken);
            foreach (var tep in tepAnh)
            {
                var duongDan = await LuuTepAnhAsync(tep, cancellationToken);
                tepDaLuu.Add(duongDan);
                var anh = new HinhAnhVay
                {
                    MaVay = vay.MaVay,
                    DuongDan = duongDan,
                    AnhChinh = tepDaLuu.Count == 1,
                    ThuTu = tepDaLuu.Count - 1
                };
                vay.DanhSachHinhAnh.Add(anh);
                if (anh.AnhChinh)
                    vay.HinhAnh = duongDan;
            }

            await _duLieu.SaveChangesAsync(cancellationToken);
            await giaoDich.CommitAsync(cancellationToken);
        }
        catch
        {
            await giaoDich.RollbackAsync(CancellationToken.None);
            XoaTepAnhDaLuu(tepDaLuu);
            throw;
        }

        TempData["ThongBao"] = tepAnh.Count == 0
            ? "Đã thêm váy. Bạn có thể thêm ảnh vào lúc sửa thông tin."
            : $"Đã thêm váy cùng {tepAnh.Count} ảnh.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Sua(int id, CancellationToken cancellationToken)
    {
        var vay = await _duLieu.VayDuTiec
            .Include(v => v.DanhSachHinhAnh.OrderBy(a => a.ThuTu))
            .FirstOrDefaultAsync(v => v.MaVay == id, cancellationToken);
        if (vay is null)
            return NotFound();

        await NapDanhMuc(cancellationToken);
        var model = ChuyenDoi(vay);
        model.DanhSachHinhAnh = vay.DanhSachHinhAnh.OrderBy(a => a.ThuTu).ToList();
        return View("BieuMau", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sua(int id, VayQuanTriViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.MaVay)
            return BadRequest();

        var vay = await _duLieu.VayDuTiec.FindAsync([id], cancellationToken);
        if (vay is null)
            return NotFound();

        if (!ModelState.IsValid)
        {
            model.DanhSachHinhAnh = await _duLieu.HinhAnhVay
                .Where(a => a.MaVay == id)
                .OrderBy(a => a.ThuTu)
                .ToListAsync(cancellationToken);
            await NapDanhMuc(cancellationToken);
            return View("BieuMau", model);
        }

        GanDuLieu(vay, model);
        await _duLieu.SaveChangesAsync(cancellationToken);
        TempData["ThongBao"] = "Đã cập nhật thông tin váy.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ThemAnh(int id, List<IFormFile> tepAnh, CancellationToken cancellationToken)
    {
        var vay = await _duLieu.VayDuTiec
            .Include(v => v.DanhSachHinhAnh)
            .FirstOrDefaultAsync(v => v.MaVay == id, cancellationToken);
        if (vay is null)
            return NotFound();

        var tepHopLe = tepAnh.Where(tep => tep.Length > 0).ToList();
        if (tepHopLe.Count == 0)
        {
            TempData["ThongBaoLoi"] = "Vui lòng chọn ít nhất một ảnh.";
            return RedirectToAction(nameof(Sua), new { id });
        }
        if (tepHopLe.Count > 3)
        {
            TempData["ThongBaoLoi"] = "Mỗi lần chỉ chọn tối đa 3 ảnh.";
            return RedirectToAction(nameof(Sua), new { id });
        }

        var loiAnh = new List<string>();
        foreach (var tep in tepHopLe)
        {
            var loi = await KiemTraTepAnhAsync(tep, cancellationToken);
            if (loi is not null)
                loiAnh.Add($"{tep.FileName}: {loi}");
        }
        if (loiAnh.Count > 0)
        {
            TempData["ThongBaoLoi"] = string.Join(" ", loiAnh);
            return RedirectToAction(nameof(Sua), new { id });
        }

        var tepDaLuu = new List<string>();
        try
        {
            foreach (var tep in tepHopLe)
            {
                var duongDan = await LuuTepAnhAsync(tep, cancellationToken);
                tepDaLuu.Add(duongDan);
                var laAnhChinh = vay.DanhSachHinhAnh.Count == 0;
                vay.DanhSachHinhAnh.Add(new HinhAnhVay
                {
                    MaVay = id,
                    DuongDan = duongDan,
                    AnhChinh = laAnhChinh,
                    ThuTu = vay.DanhSachHinhAnh.Count == 0 ? 0 : vay.DanhSachHinhAnh.Max(a => a.ThuTu) + 1
                });
                if (laAnhChinh)
                    vay.HinhAnh = duongDan;
            }

            await _duLieu.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            XoaTepAnhDaLuu(tepDaLuu);
            throw;
        }

        TempData["ThongBao"] = $"Đã thêm {tepHopLe.Count} ảnh cho mẫu váy.";
        return RedirectToAction(nameof(Sua), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ThayAnh(int id, int maAnh, IFormFile tepAnh, CancellationToken cancellationToken)
    {
        var anh = await _duLieu.HinhAnhVay
            .FirstOrDefaultAsync(a => a.MaAnh == maAnh && a.MaVay == id, cancellationToken);
        if (anh is null)
            return NotFound();

        var loiAnh = await KiemTraTepAnhAsync(tepAnh, cancellationToken);
        if (loiAnh is not null)
        {
            TempData["ThongBaoLoi"] = loiAnh;
            return RedirectToAction(nameof(Sua), new { id });
        }

        var duongDanCu = anh.DuongDan;
        var duongDanMoi = await LuuTepAnhAsync(tepAnh, cancellationToken);
        try
        {
            anh.DuongDan = duongDanMoi;
            if (anh.AnhChinh)
            {
                var vay = await _duLieu.VayDuTiec.FindAsync([id], cancellationToken);
                if (vay is not null)
                    vay.HinhAnh = duongDanMoi;
            }

            await _duLieu.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            XoaTepAnhDaLuu([duongDanMoi]);
            throw;
        }

        XoaTepAnh(duongDanCu);
        TempData["ThongBao"] = "Đã thay ảnh của mẫu váy.";
        return RedirectToAction(nameof(Sua), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XoaAnh(int id, int maAnh, CancellationToken cancellationToken)
    {
        var anh = await _duLieu.HinhAnhVay
            .FirstOrDefaultAsync(a => a.MaAnh == maAnh && a.MaVay == id, cancellationToken);
        if (anh is null)
            return NotFound();

        if (anh.AnhChinh)
        {
            TempData["ThongBaoLoi"] = "Không thể xóa ảnh đại diện. Hãy chọn ảnh khác làm ảnh đại diện trước.";
            return RedirectToAction(nameof(Sua), new { id });
        }

        var duongDanCu = anh.DuongDan;
        _duLieu.HinhAnhVay.Remove(anh);
        await _duLieu.SaveChangesAsync(cancellationToken);
        XoaTepAnh(duongDanCu);
        TempData["ThongBao"] = "Đã xóa ảnh khỏi mẫu váy.";
        return RedirectToAction(nameof(Sua), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChonAnhChinh(int id, int maAnh, CancellationToken cancellationToken)
    {
        var vay = await _duLieu.VayDuTiec
            .AsTracking()
            .Include(v => v.DanhSachHinhAnh)
            .FirstOrDefaultAsync(v => v.MaVay == id, cancellationToken);
        if (vay is null)
            return NotFound();

        var anhChon = vay.DanhSachHinhAnh.FirstOrDefault(a => a.MaAnh == maAnh);
        if (anhChon is null)
            return NotFound();

        foreach (var anh in vay.DanhSachHinhAnh)
            anh.AnhChinh = anh.MaAnh == maAnh;

        vay.HinhAnh = anhChon.DuongDan;
        await _duLieu.SaveChangesAsync(cancellationToken);
        TempData["ThongBao"] = "Đã chọn ảnh đại diện cho mẫu váy.";
        return RedirectToAction(nameof(Sua), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id, CancellationToken cancellationToken)
    {
        var vay = await _duLieu.VayDuTiec
            .Include(v => v.DanhSachHinhAnh)
            .FirstOrDefaultAsync(v => v.MaVay == id, cancellationToken);
        if (vay is null)
            return NotFound();

        if (await _duLieu.ChiTietDonThue.AnyAsync(c => c.MaVay == id, cancellationToken))
        {
            TempData["ThongBaoLoi"] = "Không thể xóa váy vì đã có đơn thuê liên quan. Hãy dùng chức năng Ẩn váy để giữ lịch sử.";
            return RedirectToAction(nameof(Index));
        }

        var duongDanAnh = vay.DanhSachHinhAnh.Select(a => a.DuongDan).ToList();
        _duLieu.VayDuTiec.Remove(vay);
        await _duLieu.SaveChangesAsync(cancellationToken);
        XoaTepAnhDaLuu(duongDanAnh);
        TempData["ThongBao"] = "Đã xóa váy và các ảnh đã tải lên.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiTrangThai(int id, CancellationToken cancellationToken)
    {
        var vay = await _duLieu.VayDuTiec.FindAsync([id], cancellationToken);
        if (vay is null)
            return NotFound();

        vay.DangKinhDoanh = !vay.DangKinhDoanh;
        await _duLieu.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Lich(int id, CancellationToken cancellationToken)
    {
        var vay = await _duLieu.VayDuTiec
            .Include(v => v.DanhMuc)
            .FirstOrDefaultAsync(v => v.MaVay == id, cancellationToken);
        if (vay is null)
            return NotFound();

        ViewBag.Vay = vay;
        var donThue = await _duLieu.ChiTietDonThue
            .Include(c => c.DonThue)
            .Where(c => c.MaVay == id && c.DonThue!.TrangThai != "DaHuy")
            .OrderBy(c => c.DonThue!.NgayBatDauThue)
            .Select(c => c.DonThue!)
            .ToListAsync(cancellationToken);
        return View(donThue);
    }

    private async Task NapDanhMuc(CancellationToken cancellationToken)
    {
        ViewBag.DanhMuc = new SelectList(
            await _duLieu.DanhMucVay.OrderBy(d => d.TenDanhMuc).ToListAsync(cancellationToken),
            nameof(DanhMucVay.MaDanhMuc), nameof(DanhMucVay.TenDanhMuc));
    }

    private static VayQuanTriViewModel ChuyenDoi(VayDuTiec vay) => new()
    {
        MaVay = vay.MaVay,
        MaDanhMuc = vay.MaDanhMuc,
        TenVay = vay.TenVay,
        KichCo = vay.KichCo,
        MauSac = vay.MauSac,
        KieuDang = vay.KieuDang,
        GiaThueMoiNgay = vay.GiaThueMoiNgay,
        TienDatCoc = vay.TienDatCoc,
        HinhAnh = vay.HinhAnh,
        DanhSachHinhAnh = vay.DanhSachHinhAnh.OrderBy(a => a.ThuTu).ToList()
    };

    private static void GanDuLieu(VayDuTiec vay, VayQuanTriViewModel model)
    {
        vay.MaDanhMuc = model.MaDanhMuc;
        vay.TenVay = model.TenVay.Trim();
        vay.KichCo = model.KichCo.Trim();
        vay.MauSac = model.MauSac.Trim();
        vay.KieuDang = model.KieuDang.Trim();
        vay.GiaThueMoiNgay = model.GiaThueMoiNgay;
        vay.TienDatCoc = model.TienDatCoc;
    }

    private async Task<string?> KiemTraTepAnhAsync(IFormFile tepAnh, CancellationToken cancellationToken)
    {
        if (tepAnh is null || tepAnh.Length == 0)
            return "Vui lòng chọn một tệp ảnh.";
        if (tepAnh.Length > DungLuongAnhToiDa)
            return "Mỗi ảnh không được vượt quá 8 MB.";

        var dinhDang = await NhanDienDinhDangAnhAsync(tepAnh, cancellationToken);
        return dinhDang is null
            ? "Không nhận diện được ảnh. Hãy chọn tệp JPG, PNG, WEBP hoặc GIF."
            : null;
    }

    private async Task KiemTraTepAnhAsync(IReadOnlyList<IFormFile> tepAnh, CancellationToken cancellationToken)
    {
        foreach (var tep in tepAnh)
        {
            var loi = await KiemTraTepAnhAsync(tep, cancellationToken);
            if (loi is not null)
                ModelState.AddModelError(nameof(VayQuanTriViewModel.AnhTaiLen), $"{tep.FileName}: {loi}");
        }
    }

    private static async Task<string?> NhanDienDinhDangAnhAsync(IFormFile tepAnh, CancellationToken cancellationToken)
    {
        var dauTep = new byte[12];
        await using var luong = tepAnh.OpenReadStream();
        var soByte = await luong.ReadAsync(dauTep.AsMemory(), cancellationToken);
        var duLieu = dauTep.AsSpan(0, soByte);

        if (duLieu.Length >= 3 && duLieu[0] == 0xFF && duLieu[1] == 0xD8 && duLieu[2] == 0xFF)
            return ".jpg";
        if (duLieu.Length >= 8 && duLieu[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            return ".png";
        if (duLieu.Length >= 6 && (duLieu[..6].SequenceEqual("GIF87a"u8) || duLieu[..6].SequenceEqual("GIF89a"u8)))
            return ".gif";
        if (duLieu.Length >= 12 && duLieu[..4].SequenceEqual("RIFF"u8) && duLieu[8..12].SequenceEqual("WEBP"u8))
            return ".webp";

        return null;
    }

    private async Task<string> LuuTepAnhAsync(IFormFile tepAnh, CancellationToken cancellationToken)
    {
        var dinhDang = await NhanDienDinhDangAnhAsync(tepAnh, cancellationToken)
            ?? throw new InvalidDataException("Không nhận diện được định dạng của tệp ảnh.");
        var tenTep = $"{Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant()}{dinhDang}";
        var thuMucAnh = Path.Combine(_moiTruong.WebRootPath ?? Path.Combine(_moiTruong.ContentRootPath, "wwwroot"), "images", "vay");
        Directory.CreateDirectory(thuMucAnh);
        var duongDanTep = Path.Combine(thuMucAnh, tenTep);
        try
        {
            await using var tepDich = new FileStream(duongDanTep, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            await tepAnh.CopyToAsync(tepDich, cancellationToken);
        }
        catch
        {
            if (System.IO.File.Exists(duongDanTep))
                System.IO.File.Delete(duongDanTep);
            throw;
        }

        return $"/images/vay/{tenTep}";
    }

    private void XoaTepAnhDaLuu(IEnumerable<string> duongDanAnh)
    {
        foreach (var duongDan in duongDanAnh)
            XoaTepAnh(duongDan);
    }

    private void XoaTepAnh(string? duongDan)
    {
        const string tienTo = "/images/vay/";
        if (string.IsNullOrWhiteSpace(duongDan) || !duongDan.StartsWith(tienTo, StringComparison.Ordinal))
            return;

        var tenTep = duongDan[tienTo.Length..];
        if (string.IsNullOrWhiteSpace(tenTep) || Path.GetFileName(tenTep) != tenTep)
            return;

        var thuMucGoc = Path.GetFullPath(Path.Combine(
            _moiTruong.WebRootPath ?? Path.Combine(_moiTruong.ContentRootPath, "wwwroot"), "images", "vay"));
        var duongDanTep = Path.GetFullPath(Path.Combine(thuMucGoc, tenTep));
        if (duongDanTep.StartsWith(thuMucGoc + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && System.IO.File.Exists(duongDanTep))
            System.IO.File.Delete(duongDanTep);
    }
}
