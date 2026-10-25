using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThueVayDuTiec.Web.Data;
using ThueVayDuTiec.Web.Services;

namespace ThueVayDuTiec.Web.Controllers;

[Authorize]
public class ThongBaoController : Controller
{
    private readonly UngDungDbContext _duLieu;

    public ThongBaoController(UngDungDbContext duLieu) => _duLieu = duLieu;

    [HttpGet]
    public async Task<IActionResult> DanhSach(CancellationToken cancellationToken)
    {
        var laQuanTri = User.IsInRole(TenVaiTro.QuanTriVien);
        var truyVan = _duLieu.DonThue.AsNoTracking();
        if (laQuanTri)
        {
            var soDonCho = await truyVan.CountAsync(d => d.TrangThai == "ChoXacNhan", cancellationToken);
            var donCho = await truyVan
                .Where(d => d.TrangThai == "ChoXacNhan")
                .OrderByDescending(d => d.NgayTao)
                .Take(8)
                .Select(d => new
                {
                    maDon = d.MaDonThue,
                    khachHang = d.HoTenKhach,
                    trangThai = d.TrangThai,
                    ngayTao = d.NgayTao,
                    ngayThue = d.NgayBatDauThue,
                    ngayTra = d.NgayKetThucThue,
                    url = $"/QuanTri/DonThue/ChiTiet/{d.MaDonThue}"
                })
                .ToListAsync(cancellationToken);

            return Json(new { soLuong = soDonCho, laQuanTri = true, danhSach = donCho });
        }

        var nguoiDungId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(nguoiDungId)) return Unauthorized();

        truyVan = truyVan.Where(d => d.NguoiDungId == nguoiDungId);
        var soDonChoCuaKhach = await truyVan.CountAsync(d => d.TrangThai == "ChoXacNhan", cancellationToken);
        var donCuaKhach = await truyVan
            .OrderByDescending(d => d.NgayTao)
            .Take(8)
            .Select(d => new
            {
                maDon = d.MaDonThue,
                khachHang = d.HoTenKhach,
                trangThai = d.TrangThai,
                ngayTao = d.NgayTao,
                ngayThue = d.NgayBatDauThue,
                ngayTra = d.NgayKetThucThue,
                url = "/DonThue/CuaToi"
            })
            .ToListAsync(cancellationToken);

        return Json(new { soLuong = soDonChoCuaKhach, laQuanTri = false, danhSach = donCuaKhach });
    }
}
