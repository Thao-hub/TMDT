using System.ComponentModel.DataAnnotations;

namespace ThueVayDuTiec.Web.ViewModels;

public class ThongTinCuaHangViewModel
{
    [StringLength(300, ErrorMessage = "Địa chỉ không được vượt quá 300 ký tự.")]
    [Display(Name = "Địa chỉ cửa hàng")]
    public string DiaChi { get; set; } = "";

    [Phone(ErrorMessage = "Số điện thoại chưa đúng định dạng.")]
    [StringLength(30, ErrorMessage = "Số điện thoại không được vượt quá 30 ký tự.")]
    [Display(Name = "Số điện thoại")]
    public string SoDienThoai { get; set; } = "";

    [EmailAddress(ErrorMessage = "Email chưa đúng định dạng.")]
    [StringLength(150, ErrorMessage = "Email không được vượt quá 150 ký tự.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = "";

    [StringLength(100, ErrorMessage = "Giờ hỗ trợ không được vượt quá 100 ký tự.")]
    [Display(Name = "Giờ hỗ trợ")]
    public string GioHoTro { get; set; } = "";
}
