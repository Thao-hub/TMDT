using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ThueVayDuTiec.Web.Services;
using ThueVayDuTiec.Web.ViewModels;

namespace ThueVayDuTiec.Web.Areas.QuanTri.Controllers;

[Area("QuanTri")]
[Authorize(Roles = TenVaiTro.QuanTriVien)]
public class ThongTinCuaHangController : Controller
{
    private readonly IConfiguration _cauHinh;
    private readonly IWebHostEnvironment _moiTruong;

    public ThongTinCuaHangController(IConfiguration cauHinh, IWebHostEnvironment moiTruong)
    {
        _cauHinh = cauHinh;
        _moiTruong = moiTruong;
    }

    [HttpGet]
    public IActionResult Index() => View(TaoViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(ThongTinCuaHangViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var duongDan = Path.Combine(_moiTruong.ContentRootPath, "appsettings.json");
        try
        {
            var cauHinhJson = JsonNode.Parse(System.IO.File.ReadAllText(duongDan))?.AsObject();
            if (cauHinhJson is null)
                throw new JsonException("Không đọc được cấu hình cửa hàng.");

            var thongTin = cauHinhJson["ThongTinCuaHang"] as JsonObject;
            if (thongTin is null)
            {
                thongTin = new JsonObject();
                cauHinhJson["ThongTinCuaHang"] = thongTin;
            }
            thongTin["DiaChi"] = JsonValue.Create(model.DiaChi.Trim());
            thongTin["SoDienThoai"] = JsonValue.Create(model.SoDienThoai.Trim());
            thongTin["Email"] = JsonValue.Create(model.Email.Trim());
            thongTin["GioHoTro"] = JsonValue.Create(model.GioHoTro.Trim());

            System.IO.File.WriteAllText(duongDan, cauHinhJson.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
            if (_cauHinh is IConfigurationRoot cauHinhGoc)
                cauHinhGoc.Reload();

            TempData["ThongBao"] = "Đã cập nhật thông tin cửa hàng.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception loi) when (loi is IOException or UnauthorizedAccessException or JsonException)
        {
            ModelState.AddModelError(string.Empty, "Không thể lưu cấu hình. Hãy kiểm tra quyền ghi file appsettings.json rồi thử lại.");
            return View(model);
        }
    }

    private ThongTinCuaHangViewModel TaoViewModel() => new()
    {
        DiaChi = DocGiaTri("DiaChi"),
        SoDienThoai = DocGiaTri("SoDienThoai"),
        Email = DocGiaTri("Email"),
        GioHoTro = DocGiaTri("GioHoTro")
    };

    private string DocGiaTri(string ten)
    {
        var giaTri = _cauHinh[$"ThongTinCuaHang:{ten}"] ?? "";
        return giaTri.Equals("Chưa cập nhật", StringComparison.OrdinalIgnoreCase) ? "" : giaTri;
    }
}
