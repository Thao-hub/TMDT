using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThueVayDuTiec.Web.Data;
using ThueVayDuTiec.Web.Models;
using ThueVayDuTiec.Web.Services;
using ThueVayDuTiec.Web.ViewModels;

namespace ThueVayDuTiec.Web.Areas.QuanTri.Controllers;

[Area("QuanTri")]
[Authorize(Roles = TenVaiTro.QuanTriVien)]
public class DanhMucController : Controller
{
    private readonly UngDungDbContext _duLieu;

    public DanhMucController(UngDungDbContext duLieu) => _duLieu = duLieu;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await _duLieu.DanhMucVay
            .Include(d => d.DanhSachVay)
            .OrderBy(d => d.TenDanhMuc)
            .ToListAsync(cancellationToken));
    }

    [HttpGet]
    public IActionResult Them() => View("BieuMau", new DanhMucQuanTriViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Them(DanhMucQuanTriViewModel model, CancellationToken cancellationToken)
    {
        await KiemTraTenDanhMucAsync(model, null, cancellationToken);
        if (!ModelState.IsValid)
            return View("BieuMau", model);

        _duLieu.DanhMucVay.Add(new DanhMucVay { TenDanhMuc = model.TenDanhMuc.Trim(), MoTa = model.MoTa?.Trim() });
        await _duLieu.SaveChangesAsync(cancellationToken);
        TempData["ThongBao"] = "Đã thêm danh mục.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Sua(int id, CancellationToken cancellationToken)
    {
        var danhMuc = await _duLieu.DanhMucVay.FindAsync([id], cancellationToken);
        if (danhMuc is null)
            return NotFound();
        return View("BieuMau", new DanhMucQuanTriViewModel
        {
            MaDanhMuc = danhMuc.MaDanhMuc,
            TenDanhMuc = danhMuc.TenDanhMuc,
            MoTa = danhMuc.MoTa
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sua(int id, DanhMucQuanTriViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.MaDanhMuc)
            return BadRequest();
        var danhMuc = await _duLieu.DanhMucVay.FindAsync([id], cancellationToken);
        if (danhMuc is null)
            return NotFound();
        await KiemTraTenDanhMucAsync(model, id, cancellationToken);
        if (!ModelState.IsValid)
            return View("BieuMau", model);

        danhMuc.TenDanhMuc = model.TenDanhMuc.Trim();
        danhMuc.MoTa = model.MoTa?.Trim();
        await _duLieu.SaveChangesAsync(cancellationToken);
        TempData["ThongBao"] = "Đã cập nhật danh mục.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id, CancellationToken cancellationToken)
    {
        var danhMuc = await _duLieu.DanhMucVay
            .Include(d => d.DanhSachVay)
            .FirstOrDefaultAsync(d => d.MaDanhMuc == id, cancellationToken);
        if (danhMuc is null)
            return NotFound();

        if (danhMuc.DanhSachVay.Count > 0)
        {
            TempData["ThongBaoLoi"] =
                $"Không thể xóa danh mục “{danhMuc.TenDanhMuc}” vì đang có {danhMuc.DanhSachVay.Count} váy sử dụng. Hãy chuyển các váy sang danh mục khác trước.";
            return RedirectToAction(nameof(Index));
        }

        _duLieu.DanhMucVay.Remove(danhMuc);
        await _duLieu.SaveChangesAsync(cancellationToken);
        TempData["ThongBao"] = "Đã xóa danh mục.";
        return RedirectToAction(nameof(Index));
    }

    private async Task KiemTraTenDanhMucAsync(
        DanhMucQuanTriViewModel model,
        int? maDanhMucHienTai,
        CancellationToken cancellationToken)
    {
        var tenDanhMuc = model.TenDanhMuc?.Trim();
        if (string.IsNullOrWhiteSpace(tenDanhMuc))
            return;

        var tenDaTonTai = await _duLieu.DanhMucVay.AnyAsync(
            danhMuc => danhMuc.TenDanhMuc == tenDanhMuc
                && (!maDanhMucHienTai.HasValue || danhMuc.MaDanhMuc != maDanhMucHienTai.Value),
            cancellationToken);
        if (tenDaTonTai)
            ModelState.AddModelError(nameof(model.TenDanhMuc), "Tên danh mục này đã được sử dụng.");
    }
}
