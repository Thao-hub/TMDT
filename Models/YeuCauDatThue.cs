using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ThueVayDuTiec.Web.Models;

public class YeuCauDatThue
{
    public int MaVay { get; set; }

    [Required]
    public DateTime NgayBatDauThue { get; set; } = DateTime.Today.AddDays(1);

    [Required]
    public DateTime NgayKetThucThue { get; set; } = DateTime.Today.AddDays(2);

    [Required(ErrorMessage = "Vui lòng tải ảnh chuyển khoản lên trước khi gửi yêu cầu.")]
    public IFormFile? AnhChuyenKhoan { get; set; }
}
