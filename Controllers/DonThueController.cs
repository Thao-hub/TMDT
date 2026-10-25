using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThueVayDuTiec.Web.Models;
using ThueVayDuTiec.Web.Data;
using System.Security.Claims;

namespace ThueVayDuTiec.Web.Controllers;

[Authorize]
public class DonThueController : Controller
{
    private readonly UngDungDbContext _duLieu;

    public DonThueController(UngDungDbContext duLieu) => _duLieu = duLieu;

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
        return View(new YeuCauDatThue
        {
            MaVay = vay.MaVay,
            NgayBatDauThue = DateTime.Today.AddDays(1),
            NgayKetThucThue = DateTime.Today.AddDays(2)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
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

        if (yeuCau.NgayBatDauThue.Date < DateTime.Today)
            ModelState.AddModelError(nameof(yeuCau.NgayBatDauThue), "Ngày bắt đầu thuê không được ở trong quá khứ.");

        if (yeuCau.NgayKetThucThue.Date < yeuCau.NgayBatDauThue.Date)
            ModelState.AddModelError(nameof(yeuCau.NgayKetThucThue), "Ngày trả phải bằng hoặc sau ngày bắt đầu thuê.");

        if (ModelState.IsValid)
        {
            // Hai khoảng ngày bị xem là trùng nếu chúng giao nhau.
            bool biTrungLich = await _duLieu.ChiTietDonThue
                .Include(c => c.DonThue)
                .AnyAsync(c => c.MaVay == yeuCau.MaVay
                    && c.DonThue!.TrangThai != "DaHuy"
                    && yeuCau.NgayBatDauThue.Date <= c.DonThue.NgayKetThucThue.Date
                    && yeuCau.NgayKetThucThue.Date >= c.DonThue.NgayBatDauThue.Date, cancellationToken);

            if (biTrungLich)
                ModelState.AddModelError("", "Váy đã có người đặt trong khoảng ngày này. Vui lòng chọn ngày khác.");
        }

        if (!ModelState.IsValid)
        {
            GanThongTinThanhVien(nguoiDung);
            return View(yeuCau);
        }

        int soNgay = (yeuCau.NgayKetThucThue.Date - yeuCau.NgayBatDauThue.Date).Days + 1;
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
        await _duLieu.SaveChangesAsync();
        return RedirectToAction(nameof(XacNhan), new { id = donThue.MaDonThue });
    }

    public async Task<IActionResult> XacNhan(int id, CancellationToken cancellationToken)
    {
        IQueryable<DonThue> truyVan = _duLieu.DonThue
            .Include(d => d.DanhSachChiTiet)
            .ThenInclude(c => c.Vay)
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
}
