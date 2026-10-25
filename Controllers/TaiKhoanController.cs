using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ThueVayDuTiec.Web.Models;
using ThueVayDuTiec.Web.Services;
using ThueVayDuTiec.Web.ViewModels;

namespace ThueVayDuTiec.Web.Controllers;

public class TaiKhoanController : Controller
{
    private readonly UserManager<NguoiDung> _quanLyNguoiDung;
    private readonly SignInManager<NguoiDung> _quanLyDangNhap;
    private readonly RoleManager<IdentityRole> _quanLyVaiTro;

    public TaiKhoanController(
        UserManager<NguoiDung> quanLyNguoiDung,
        SignInManager<NguoiDung> quanLyDangNhap,
        RoleManager<IdentityRole> quanLyVaiTro)
    {
        _quanLyNguoiDung = quanLyNguoiDung;
        _quanLyDangNhap = quanLyDangNhap;
        _quanLyVaiTro = quanLyVaiTro;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult DangKy(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DangKy(DangKyViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
            return View(model);

        if (await _quanLyNguoiDung.FindByEmailAsync(model.Email) is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "Email này đã được sử dụng.");
            return View(model);
        }

        if (!await _quanLyVaiTro.RoleExistsAsync(TenVaiTro.KhachHang))
        {
            var ketQuaTaoVaiTro = await _quanLyVaiTro.CreateAsync(new IdentityRole(TenVaiTro.KhachHang));
            if (!ketQuaTaoVaiTro.Succeeded)
            {
                ThemLoi(ketQuaTaoVaiTro);
                return View(model);
            }
        }

        var nguoiDung = new NguoiDung
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            HoTen = model.HoTen.Trim(),
            PhoneNumber = model.SoDienThoai.Trim()
        };
        IdentityResult ketQuaTaoNguoiDung;
        try
        {
            ketQuaTaoNguoiDung = await _quanLyNguoiDung.CreateAsync(nguoiDung, model.MatKhau);
        }
        catch (DbUpdateException loi) when (loi.GetBaseException() is SqlException { Number: 2601 or 2627 })
        {
            ModelState.AddModelError(nameof(model.Email), "Email này đã được sử dụng.");
            return View(model);
        }

        if (!ketQuaTaoNguoiDung.Succeeded)
        {
            ThemLoi(ketQuaTaoNguoiDung);
            return View(model);
        }

        var ketQuaGanVaiTro = await _quanLyNguoiDung.AddToRoleAsync(nguoiDung, TenVaiTro.KhachHang);
        if (!ketQuaGanVaiTro.Succeeded)
        {
            await _quanLyNguoiDung.DeleteAsync(nguoiDung);
            ThemLoi(ketQuaGanVaiTro);
            return View(model);
        }

        TempData["ThongBao"] = "Đăng ký thành công. Vui lòng đăng nhập để tiếp tục.";
        return RedirectToAction(nameof(DangNhap), new { returnUrl });
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult DangNhap(string? returnUrl = null)
    {
        return View(new DangNhapViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DangNhap(DangNhapViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var ketQua = await _quanLyDangNhap.PasswordSignInAsync(
            model.Email.Trim(), model.MatKhau, model.GhiNhoDangNhap, lockoutOnFailure: true);
        if (!ketQua.Succeeded)
        {
            ModelState.AddModelError("", ketQua.IsLockedOut
                ? "Tài khoản tạm khóa do đăng nhập sai nhiều lần. Vui lòng thử lại sau."
                : "Email hoặc mật khẩu không chính xác.");
            return View(model);
        }

        if (Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl!);

        var nguoiDung = await _quanLyNguoiDung.FindByEmailAsync(model.Email.Trim());
        if (nguoiDung is not null
            && await _quanLyNguoiDung.IsInRoleAsync(nguoiDung, TenVaiTro.QuanTriVien))
            return RedirectToAction("Index", "TrangChu", new { area = "QuanTri" });

        return RedirectToAction("Index", "TrangChu");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DangXuat()
    {
        await _quanLyDangNhap.SignOutAsync();
        return RedirectToAction("Index", "TrangChu");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> HoSo()
    {
        var nguoiDung = await _quanLyNguoiDung.GetUserAsync(User);
        if (nguoiDung is null)
            return Challenge();

        return View(new HoSoViewModel
        {
            HoTen = nguoiDung.HoTen,
            SoDienThoai = nguoiDung.PhoneNumber ?? "",
            Email = nguoiDung.Email ?? ""
        });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HoSo(HoSoViewModel model)
    {
        var nguoiDung = await _quanLyNguoiDung.GetUserAsync(User);
        if (nguoiDung is null)
            return Challenge();

        model.Email = nguoiDung.Email ?? "";
        if (!ModelState.IsValid)
            return View(model);

        nguoiDung.HoTen = model.HoTen.Trim();
        nguoiDung.PhoneNumber = model.SoDienThoai.Trim();
        var ketQua = await _quanLyNguoiDung.UpdateAsync(nguoiDung);
        if (!ketQua.Succeeded)
        {
            ThemLoi(ketQua);
            return View(model);
        }

        await _quanLyDangNhap.RefreshSignInAsync(nguoiDung);
        TempData["ThongBao"] = "Thông tin cá nhân đã được cập nhật.";
        return RedirectToAction(nameof(HoSo));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult TuChoi() => View();

    private void ThemLoi(IdentityResult ketQua)
    {
        foreach (var loi in ketQua.Errors)
        {
            var thongBao = loi.Code switch
            {
                "DuplicateUserName" or "DuplicateEmail" => "Email này đã được sử dụng.",
                "InvalidEmail" => "Email chưa đúng định dạng.",
                "InvalidUserName" => "Email không hợp lệ.",
                "PasswordTooShort" => "Mật khẩu chưa đáp ứng yêu cầu độ dài.",
                "PasswordRequiresDigit" => "Mật khẩu cần có ít nhất một chữ số.",
                "PasswordRequiresUpper" => "Mật khẩu cần có ít nhất một chữ in hoa.",
                "PasswordRequiresLower" => "Mật khẩu cần có ít nhất một chữ thường.",
                "PasswordRequiresNonAlphanumeric" => "Mật khẩu cần có ít nhất một ký tự đặc biệt.",
                _ => "Không thể hoàn tất thao tác tài khoản. Vui lòng kiểm tra lại thông tin."
            };
            ModelState.AddModelError("", thongBao);
        }
    }
}
