using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ThueVayDuTiec.Web.Data;
using ThueVayDuTiec.Web.Models;
using ThueVayDuTiec.Web.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<UngDungDbContext>(tuyChon =>
    tuyChon.UseSqlServer(builder.Configuration.GetConnectionString("KetNoiMacDinh")));
builder.Services.AddIdentity<NguoiDung, IdentityRole>(tuyChon =>
    {
        tuyChon.User.RequireUniqueEmail = true;
        tuyChon.SignIn.RequireConfirmedAccount = false;
        tuyChon.Password.RequiredLength = 8;
        tuyChon.Password.RequiredUniqueChars = 1;
        tuyChon.Password.RequireDigit = false;
        tuyChon.Password.RequireLowercase = true;
        tuyChon.Password.RequireUppercase = true;
        tuyChon.Password.RequireNonAlphanumeric = true;
        tuyChon.Lockout.MaxFailedAccessAttempts = 5;
        tuyChon.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
        tuyChon.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<UngDungDbContext>()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(tuyChon =>
{
    tuyChon.LoginPath = "/TaiKhoan/DangNhap";
    tuyChon.AccessDeniedPath = "/TaiKhoan/TuChoi";
    tuyChon.Cookie.HttpOnly = true;
    tuyChon.Cookie.SameSite = SameSiteMode.Lax;
    tuyChon.SlidingExpiration = true;
});

var ungDung = builder.Build();

if (args.Contains("--tao-quan-tri", StringComparer.OrdinalIgnoreCase))
{
    using var phamVi = ungDung.Services.CreateScope();
    await KhoiTaoQuanTri.TaoTaiKhoanAsync(phamVi.ServiceProvider);
    return;
}

if (!ungDung.Environment.IsDevelopment())
{
    ungDung.UseExceptionHandler("/TrangChu/Loi");
    ungDung.UseHsts();
}

ungDung.UseHttpsRedirection();
ungDung.UseStaticFiles();
ungDung.UseRouting();
ungDung.UseAuthentication();
ungDung.UseAuthorization();

ungDung.MapAreaControllerRoute(
    name: "khuQuanTri",
    areaName: "QuanTri",
    pattern: "QuanTri/{controller=TrangChu}/{action=Index}/{id?}");
ungDung.MapControllerRoute(
    name: "macDinh",
    pattern: "{controller=TrangChu}/{action=Index}/{id?}");

ungDung.Run();
