using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThueVayDuTiec.Web.Data;

namespace ThueVayDuTiec.Web.Controllers;

public class TrangChuController : Controller
{
    private readonly UngDungDbContext _duLieu;

    public TrangChuController(UngDungDbContext duLieu) => _duLieu = duLieu;

    public async Task<IActionResult> Index()
    {
        var danhSachVay = await _duLieu.VayDuTiec
            .Include(v => v.DanhMuc)
            .Include(v => v.DanhSachHinhAnh)
            .Where(v => v.DangKinhDoanh)
            .OrderBy(v => v.TenVay)
            .ToListAsync();

        ViewBag.DanhMuc = await _duLieu.DanhMucVay.OrderBy(d => d.TenDanhMuc).ToListAsync();
        return View(danhSachVay);
    }

    public IActionResult Loi() => View();

    public IActionResult QuyTrinhThue() => View();

    public IActionResult ChinhSachDatCoc() => View();

    public IActionResult QuyTrinhHoanCoc() => View();

    public IActionResult LienHe() => View();
}
