using Microsoft.AspNetCore.Identity;
using System.Text;
using ThueVayDuTiec.Web.Models;

namespace ThueVayDuTiec.Web.Services;

public static class KhoiTaoQuanTri
{
    public static async Task TaoTaiKhoanAsync(IServiceProvider dichVu)
    {
        Console.InputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        Console.Write("Họ và tên quản trị viên: ");
        var hoTen = Console.ReadLine()?.Trim();
        Console.Write("Email quản trị viên: ");
        var email = Console.ReadLine()?.Trim();
        Console.Write("Mật khẩu: ");
        var matKhau = NhapMatKhau();

        if (string.IsNullOrWhiteSpace(hoTen) || string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Họ tên và email không được để trống.");
        }

        if (string.IsNullOrEmpty(matKhau))
        {
            throw new InvalidOperationException("Mật khẩu không được để trống.");
        }

        var daXacNhanMatKhau = false;
        for (var lanThu = 1; lanThu <= 3; lanThu++)
        {
            Console.Write($"Nhập lại mật khẩu (lần {lanThu}/3): ");
            if (matKhau == NhapMatKhau())
            {
                daXacNhanMatKhau = true;
                break;
            }

            Console.WriteLine("Mật khẩu xác nhận không khớp.");
        }

        if (!daXacNhanMatKhau)
        {
            throw new InvalidOperationException("Đã nhập sai mật khẩu xác nhận 3 lần. Hãy chạy lại lệnh để thử lại.");
        }

        var quanLyVaiTro = dichVu.GetRequiredService<RoleManager<IdentityRole>>();
        var quanLyNguoiDung = dichVu.GetRequiredService<UserManager<NguoiDung>>();

        foreach (var tenVaiTro in TenVaiTro.TatCa)
        {
            if (!await quanLyVaiTro.RoleExistsAsync(tenVaiTro))
            {
                var ketQuaTaoVaiTro = await quanLyVaiTro.CreateAsync(new IdentityRole(tenVaiTro));
                KiemTraKetQua(ketQuaTaoVaiTro, "Không thể tạo vai trò " + tenVaiTro);
            }
        }

        var nguoiDung = await quanLyNguoiDung.FindByEmailAsync(email);
        if (nguoiDung is null)
        {
            nguoiDung = new NguoiDung
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                HoTen = hoTen
            };
            var ketQuaTaoNguoiDung = await quanLyNguoiDung.CreateAsync(nguoiDung, matKhau);
            KiemTraKetQua(ketQuaTaoNguoiDung, "Không thể tạo tài khoản quản trị");
        }
        else if (!await quanLyNguoiDung.CheckPasswordAsync(nguoiDung, matKhau))
        {
            throw new InvalidOperationException("Email đã tồn tại; mật khẩu cung cấp không khớp nên không thay đổi vai trò.");
        }
        else if (!string.Equals(nguoiDung.HoTen, hoTen, StringComparison.Ordinal))
        {
            nguoiDung.HoTen = hoTen;
            var ketQuaCapNhatNguoiDung = await quanLyNguoiDung.UpdateAsync(nguoiDung);
            KiemTraKetQua(ketQuaCapNhatNguoiDung, "Không thể cập nhật họ tên quản trị viên");
        }

        if (!await quanLyNguoiDung.IsInRoleAsync(nguoiDung, TenVaiTro.QuanTriVien))
        {
            var ketQuaCapVaiTro = await quanLyNguoiDung.AddToRoleAsync(nguoiDung, TenVaiTro.QuanTriVien);
            KiemTraKetQua(ketQuaCapVaiTro, "Không thể gán vai trò quản trị viên");
        }

        Console.WriteLine("Thiết lập tài khoản quản trị viên thành công.");
    }

    private static string NhapMatKhau()
    {
        var matKhau = new StringBuilder();
        ConsoleKeyInfo phim;

        while ((phim = Console.ReadKey(intercept: true)).Key != ConsoleKey.Enter)
        {
            if (phim.Key == ConsoleKey.Backspace)
            {
                if (matKhau.Length > 0)
                {
                    matKhau.Length--;
                    Console.Write("\b \b");
                }

                continue;
            }

            if (phim.Key == ConsoleKey.C && phim.Modifiers.HasFlag(ConsoleModifiers.Control))
            {
                throw new OperationCanceledException("Đã hủy thiết lập tài khoản quản trị.");
            }

            if (!char.IsControl(phim.KeyChar))
            {
                matKhau.Append(phim.KeyChar);
                Console.Write('*');
            }
        }

        Console.WriteLine();
        return matKhau.ToString();
    }

    public static void KiemTraKetQua(IdentityResult ketQua, string thongBao)
    {
        if (!ketQua.Succeeded)
        {
            throw new InvalidOperationException(
                thongBao + ": " + string.Join("; ", ketQua.Errors.Select(loi => loi.Description)));
        }
    }
}
