# Nền tảng thương mại điện tử cho thuê váy dự tiệc

Website ASP.NET Core MVC hỗ trợ khách hàng duyệt bộ sưu tập, đặt thuê váy và theo dõi đơn; quản trị viên quản lý váy, danh mục, đơn thuê và lịch.

## Công nghệ

- C#, ASP.NET Core MVC, Razor, HTML, CSS, JavaScript.
- .NET 10, Entity Framework Core 10.0.3, ASP.NET Core Identity 10.0.3.
- Microsoft SQL Server.

## Chức năng

- Khách chưa đăng nhập xem trang chủ, tìm/lọc váy và xem chi tiết.
- Đăng ký tài khoản khách hàng, đăng nhập, đăng xuất, cập nhật hồ sơ.
- Đặt thuê yêu cầu đăng nhập; hồ sơ Identity được lưu cùng đơn thuê.
- Khách xem và hủy đơn đang chờ xác nhận của chính mình. Các truy vấn chi tiết, hủy và xác nhận đều kiểm tra chủ sở hữu ở máy chủ.
- Quản trị viên quản lý váy, danh mục, đơn thuê, xác nhận/hủy đơn và xem lịch từng váy.
- Các biểu mẫu POST có chống giả mạo yêu cầu (anti-forgery).

## Cấu hình và chạy

1. Cài .NET 10 SDK và SQL Server.
2. Kiểm tra chuỗi kết nối `KetNoiMacDinh` trong `appsettings.json`; dùng biến môi trường hoặc User Secrets cho thông tin nhạy cảm ở môi trường chia sẻ.
3. Chạy:

   ```powershell
   dotnet restore
   dotnet build
   dotnet run
   ```

## Cập nhật cơ sở dữ liệu (chưa tự động áp dụng)

Dự án hiện quản lý schema bằng SQL script, chưa dùng EF Core Migrations. Ứng dụng **không tự chạy migration hoặc cập nhật schema khi khởi động**.

### Khởi tạo database mới

Schema được quản lý bằng duy nhất tệp `ThueVayDuTiec.sql`. Tệp này tạo database mới, các bảng cần cho website/ASP.NET Core Identity và dữ liệu khởi tạo. Script không chuyển đổi schema cũ; chỉ chạy một lần trên database mới, không chạy lại khi các bảng đã được tạo.

Tên bảng, cột và constraint trong schema mới dùng tiếng Việt không dấu. ASP.NET Core Identity được ánh xạ sang `NguoiDung`, `VaiTro`, `VaiTroNguoiDung`, `TuyenBoNguoiDung`, `TuyenBoVaiTro`, `DangNhapNgoai` và `MaTruyCapNguoiDung`. Hãy xem lại script và tự chạy thủ công trên database mới khi sẵn sàng. Trong giai đoạn hiện tại không có script nào được thực thi.

Mỗi váy có thể lưu nhiều ảnh trong bảng `HinhAnhVay`; quản trị viên có thể tải thêm, thay, xóa ảnh và chọn ảnh đại diện. Ứng dụng chấp nhận JPG, PNG, WEBP và GIF, tối đa 8 MB mỗi tệp, lưu dưới `wwwroot/images/vay`.

Nếu đang dùng database cũ, chạy phần chuyển đổi sau một lần để tạo bảng ảnh và chuyển các đường dẫn đã lưu:

```sql
CREATE TABLE HinhAnhVay
(
    MaAnh INT IDENTITY(1,1) NOT NULL CONSTRAINT KC_HinhAnhVay PRIMARY KEY,
    MaVay INT NOT NULL,
    DuongDan NVARCHAR(500) NOT NULL,
    AnhChinh BIT NOT NULL CONSTRAINT MD_HinhAnhVay_AnhChinh DEFAULT 0,
    ThuTu INT NOT NULL CONSTRAINT MD_HinhAnhVay_ThuTu DEFAULT 0,
    CONSTRAINT KN_HinhAnhVay_Vay FOREIGN KEY (MaVay) REFERENCES VayDuTiec(MaVay) ON DELETE CASCADE
);
CREATE INDEX CM_HinhAnhVay_ThuTu ON HinhAnhVay(MaVay, ThuTu);

INSERT INTO HinhAnhVay (MaVay, DuongDan, AnhChinh, ThuTu)
SELECT v.MaVay, a.DuongDan, a.AnhChinh, a.ThuTu
FROM VayDuTiec v
CROSS APPLY
(
    VALUES
        (v.HinhAnh, 1, 0),
        (v.HinhAnh2, 0, 1),
        (v.HinhAnh3, 0, 2)
) a(DuongDan, AnhChinh, ThuTu)
WHERE NULLIF(LTRIM(RTRIM(a.DuongDan)), N'') IS NOT NULL;
```

Sau khi kiểm tra dữ liệu đã chuyển, có thể bỏ hai cột giới hạn ảnh cũ:

```sql
ALTER TABLE VayDuTiec DROP COLUMN HinhAnh2, HinhAnh3;
```

Giữ cột `HinhAnh` để tương thích ảnh đại diện cũ; ứng dụng đồng bộ cột này khi thêm, thay hoặc chọn ảnh đại diện.

## Tạo tài khoản quản trị đầu tiên

Không có mật khẩu quản trị mặc định trong mã nguồn. Sau khi tự áp dụng script SQL và cấu hình chuỗi kết nối, mở PowerShell tại thư mục dự án và chạy lệnh:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project .\ThueVayDuTiec.Web.csproj -- --tao-quan-tri
Remove-Item Env:ASPNETCORE_ENVIRONMENT
```

Chương trình sẽ hỏi họ tên, email, mật khẩu và xác nhận mật khẩu; ký tự mật khẩu được che khi nhập. Lệnh thiết lập riêng này tạo vai trò còn thiếu và tài khoản quản trị; nếu email đã tồn tại, chỉ gán quyền khi mật khẩu nhập vào khớp với tài khoản đó. Lệnh không chạy khi website khởi động thông thường. Mọi môi trường chấp nhận mật khẩu từ 1 ký tự. Production vẫn yêu cầu có ít nhất một chữ số; Development chấp nhận mật khẩu chỉ gồm chữ hoặc số.

## Bố cục chính

- `Controllers`, `Areas/QuanTri/Controllers`: luồng khách hàng và quản trị.
- `Models`, `ViewModels`: entity Identity/nghiệp vụ và dữ liệu biểu mẫu.
- `Data/UngDungDbContext.cs`: Identity DbContext và quan hệ dữ liệu thuê.
- `Views`, `Areas/QuanTri/Views`: giao diện Razor.
- `ThueVayDuTiec.sql`: script duy nhất khởi tạo schema cuối cùng, bao gồm Identity; không chứa lệnh chuyển đổi schema cũ.

## Kiểm thử và giới hạn

Đã chạy `dotnet restore` và `dotnet build` sau khi tích hợp; build thành công. Chưa chạy luồng đăng ký/đăng nhập/đặt thuê trên SQL Server và chưa thực thi script SQL hoặc kết nối database trong giai đoạn này. Trước khi dùng, hãy kiểm tra và chạy `ThueVayDuTiec.sql` thủ công trên database mới.

Giá thuê hiện tính cả ngày bắt đầu và ngày kết thúc. Phiên bản này chưa tích hợp thanh toán, xác minh email hoặc cơ chế giữ lịch có thời hạn.
