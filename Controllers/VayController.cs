using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThueVayDuTiec.Web.Data;

namespace ThueVayDuTiec.Web.Controllers;

public class VayController : Controller
{
    private readonly UngDungDbContext _duLieu;

    public VayController(UngDungDbContext duLieu) => _duLieu = duLieu;

    public async Task<IActionResult> ChiTiet(int id)
    {
        var vay = await _duLieu.VayDuTiec
            .Include(v => v.DanhMuc)
            .Include(v => v.DanhSachHinhAnh)
            .FirstOrDefaultAsync(v => v.MaVay == id && v.DangKinhDoanh);

        if (vay == null) return NotFound();
        return View(vay);
    }
}
