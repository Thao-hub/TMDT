using System.ComponentModel.DataAnnotations;

namespace ThueVayDuTiec.Web.ViewModels;

public class DangKyViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
    [StringLength(100, ErrorMessage = "Họ và tên không được vượt quá 100 ký tự.")]
    [Display(Name = "Họ và tên")]
    public string HoTen { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email chưa đúng định dạng.")]
    [StringLength(150, ErrorMessage = "Email không được vượt quá 150 ký tự.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [Phone(ErrorMessage = "Số điện thoại chưa đúng định dạng.")]
    [StringLength(30)]
    [Display(Name = "Số điện thoại")]
    public string SoDienThoai { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [DataType(DataType.Password)]
    [StringLength(15, MinimumLength = 8, ErrorMessage = "Mật khẩu phải dài từ 8 đến 15 ký tự.")]
    [RegularExpression(@"(?=.*[a-z])(?=.*[A-Z])(?=.*[^a-zA-Z0-9]).*", ErrorMessage = "Mật khẩu cần có ít nhất 1 chữ thường, 1 chữ hoa và 1 ký tự đặc biệt.")]
    [Display(Name = "Mật khẩu")]
    public string MatKhau { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
    [DataType(DataType.Password)]
    [Compare(nameof(MatKhau), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    [Display(Name = "Xác nhận mật khẩu")]
    public string XacNhanMatKhau { get; set; } = "";
}

public class DangNhapViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email chưa đúng định dạng.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string MatKhau { get; set; } = "";

    [Display(Name = "Ghi nhớ đăng nhập")]
    public bool GhiNhoDangNhap { get; set; }

    public string? ReturnUrl { get; set; }
}

public class HoSoViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
    [StringLength(100, ErrorMessage = "Họ và tên không được vượt quá 100 ký tự.")]
    [Display(Name = "Họ và tên")]
    public string HoTen { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [Phone(ErrorMessage = "Số điện thoại chưa đúng định dạng.")]
    [StringLength(30)]
    [Display(Name = "Số điện thoại")]
    public string SoDienThoai { get; set; } = "";

    [Display(Name = "Email")]
    public string Email { get; set; } = "";
}
