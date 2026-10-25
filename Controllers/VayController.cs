using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThueVayDuTiec.Web.Data;
using ThueVayDuTiec.Web.ViewModels;

namespace ThueVayDuTiec.Web.Controllers;

public class VayController : Controller
{
    private readonly UngDungDbContext _duLieu;

    public VayController(UngDungDbContext duLieu) => _duLieu = duLieu;

    public async Task<IActionResult> ChiTiet(int id, CancellationToken cancellationToken)
    {
        var vay = await _duLieu.VayDuTiec
            .Include(v => v.DanhMuc)
            .Include(v => v.DanhSachHinhAnh)
            .FirstOrDefaultAsync(v => v.MaVay == id && v.DangKinhDoanh);

        if (vay == null) return NotFound();

        var khoangDaDat = await _duLieu.ChiTietDonThue
            .Where(c => c.MaVay == id && c.DonThue != null && c.DonThue.TrangThai != "DaHuy" && c.DonThue.TrangThai != "KhongDenNhan")
            .Select(c => new
            {
                c.DonThue!.NgayBatDauThue,
                c.DonThue.NgayKetThucThue
            })
            .OrderBy(c => c.NgayBatDauThue)
            .ToListAsync(cancellationToken);
        ViewBag.LichDaDat = khoangDaDat.Select(khoang => new KhoangLichThueViewModel
        {
            NgayBatDau = khoang.NgayBatDauThue.Date,
            NgayKetThuc = khoang.NgayKetThucThue.Date,
            NgayXuLy = khoang.NgayKetThucThue.Date.AddDays(1)
        }).ToList();

        return View(vay);
    }
}
