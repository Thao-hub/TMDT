using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThueVayDuTiec.Web.Data;
using ThueVayDuTiec.Web.Services;

namespace ThueVayDuTiec.Web.Areas.QuanTri.Controllers;

[Area("QuanTri")]
[Authorize(Roles = TenVaiTro.QuanTriVien)]
public class DonThueController : Controller
{
    private readonly UngDungDbContext _duLieu;

    public DonThueController(UngDungDbContext duLieu) => _duLieu = duLieu;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var donThue = await _duLieu.DonThue
            .OrderByDescending(d => d.NgayTao)
            .ToListAsync(cancellationToken);
        return View(donThue);
    }

    public async Task<IActionResult> ChiTiet(int id, CancellationToken cancellationToken)
    {
        var donThue = await _duLieu.DonThue
            .Include(d => d.DanhSachChiTiet)
            .ThenInclude(c => c.Vay)
            .FirstOrDefaultAsync(d => d.MaDonThue == id, cancellationToken);
        return donThue is null ? NotFound() : View(donThue);
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
    public async Task<IActionResult> Huy(int id, CancellationToken cancellationToken)
    {
        var donThue = await _duLieu.DonThue.FindAsync([id], cancellationToken);
        if (donThue is null)
            return NotFound();
        if (donThue.TrangThai == "DaHuy")
        {
            TempData["ThongBaoLoi"] = "Đơn thuê đã được hủy trước đó.";
            return RedirectToAction(nameof(ChiTiet), new { id });
        }

        donThue.TrangThai = "DaHuy";
        await _duLieu.SaveChangesAsync(cancellationToken);
        TempData["ThongBao"] = "Đơn thuê đã được hủy.";
        return RedirectToAction(nameof(ChiTiet), new { id });
    }
}
