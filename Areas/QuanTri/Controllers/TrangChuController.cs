using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThueVayDuTiec.Web.Data;
using ThueVayDuTiec.Web.Services;
using ThueVayDuTiec.Web.ViewModels;

namespace ThueVayDuTiec.Web.Areas.QuanTri.Controllers;

[Area("QuanTri")]
[Authorize(Roles = TenVaiTro.QuanTriVien)]
public class TrangChuController : Controller
{
    private readonly UngDungDbContext _duLieu;

    public TrangChuController(UngDungDbContext duLieu) => _duLieu = duLieu;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var tongQuan = new TrangQuanTriViewModel
        {
            SoVayDangKinhDoanh = await _duLieu.VayDuTiec.CountAsync(v => v.DangKinhDoanh, cancellationToken),
            SoDanhMuc = await _duLieu.DanhMucVay.CountAsync(cancellationToken),
            SoDonCho = await _duLieu.DonThue.CountAsync(d => d.TrangThai == "ChoXacNhan", cancellationToken),
            SoDonDaXacNhan = await _duLieu.DonThue.CountAsync(d => d.TrangThai == "DaXacNhan", cancellationToken),
            DonGanDay = await _duLieu.DonThue
                .AsNoTracking()
                .OrderByDescending(d => d.NgayTao)
                .Take(5)
                .ToListAsync(cancellationToken)
        };

        return View(tongQuan);
    }
}
