USE ThueVayDuTiec;
GO

IF OBJECT_ID(N'dbo.TheoDoiDonThue', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TheoDoiDonThue
    (
        MaDon INT NOT NULL,
        NgayNhan DATETIME2 NULL,
        NgayKhongDenNhan DATETIME2 NULL,
        GhiChuKhongDenNhan NVARCHAR(500) NULL,
        TienCocMat DECIMAL(18,2) NOT NULL CONSTRAINT MD_TheoDoi_TienCocMat DEFAULT 0,
        NgayKiemTra DATETIME2 NULL,
        TinhTrang NVARCHAR(30) NULL,
        GhiChu NVARCHAR(1000) NULL,
        TienCoc DECIMAL(18,2) NOT NULL CONSTRAINT MD_TheoDoi_TienCoc DEFAULT 0,
        TienThueDaThu DECIMAL(18,2) NOT NULL CONSTRAINT MD_TheoDoi_TienThueDaThu DEFAULT 0,
        NgayThuTienThue DATETIME2 NULL,
        TienKhauTru DECIMAL(18,2) NOT NULL CONSTRAINT MD_TheoDoi_KhauTru DEFAULT 0,
        TienHoanCoc DECIMAL(18,2) NOT NULL CONSTRAINT MD_TheoDoi_HoanCoc DEFAULT 0,
        HinhThucHoanCoc NVARCHAR(30) NULL,
        NgayHoanCoc DATETIME2 NULL,
        MaNDThucHien NVARCHAR(50) NULL,
        CONSTRAINT KC_TheoDoiDonThue PRIMARY KEY (MaDon),
        CONSTRAINT KN_TheoDoi_Don FOREIGN KEY (MaDon) REFERENCES dbo.DonThue(MaDon) ON DELETE CASCADE,
        CONSTRAINT RB_TheoDoi_TienCoc CHECK (TienCoc >= 0),
        CONSTRAINT RB_TheoDoi_KhauTru CHECK (TienKhauTru >= 0 AND TienKhauTru <= TienCoc),
        CONSTRAINT RB_TheoDoi_HoanCoc CHECK (TienHoanCoc >= 0 AND TienHoanCoc = TienCoc - TienKhauTru)
    );
END;
GO

IF COL_LENGTH(N'dbo.DonThue', N'LyDoHuy') IS NULL
BEGIN
    ALTER TABLE dbo.DonThue ADD LyDoHuy NVARCHAR(500) NULL;
END;
GO

IF COL_LENGTH(N'dbo.TheoDoiDonThue', N'NgayNhan') IS NOT NULL
    ALTER TABLE dbo.TheoDoiDonThue ALTER COLUMN NgayNhan DATETIME2 NULL;
GO

IF COL_LENGTH(N'dbo.TheoDoiDonThue', N'NgayKhongDenNhan') IS NULL
    ALTER TABLE dbo.TheoDoiDonThue ADD NgayKhongDenNhan DATETIME2 NULL;
GO

IF COL_LENGTH(N'dbo.TheoDoiDonThue', N'GhiChuKhongDenNhan') IS NULL
    ALTER TABLE dbo.TheoDoiDonThue ADD GhiChuKhongDenNhan NVARCHAR(500) NULL;
GO

IF COL_LENGTH(N'dbo.TheoDoiDonThue', N'TienCocMat') IS NULL
    ALTER TABLE dbo.TheoDoiDonThue ADD TienCocMat DECIMAL(18,2) NOT NULL CONSTRAINT MD_TheoDoi_TienCocMat DEFAULT 0;
GO

IF COL_LENGTH(N'dbo.TheoDoiDonThue', N'TienThueDaThu') IS NULL
    ALTER TABLE dbo.TheoDoiDonThue ADD TienThueDaThu DECIMAL(18,2) NOT NULL CONSTRAINT MD_TheoDoi_TienThueDaThu DEFAULT 0;
GO

IF COL_LENGTH(N'dbo.TheoDoiDonThue', N'NgayThuTienThue') IS NULL
    ALTER TABLE dbo.TheoDoiDonThue ADD NgayThuTienThue DATETIME2 NULL;
GO

IF OBJECT_ID(N'dbo.CK_TheoDoi_HoanCoc', N'C') IS NOT NULL
    ALTER TABLE dbo.TheoDoiDonThue DROP CONSTRAINT CK_TheoDoi_HoanCoc;
GO

IF OBJECT_ID(N'dbo.RB_TheoDoi_HoanCoc', N'C') IS NOT NULL
    ALTER TABLE dbo.TheoDoiDonThue DROP CONSTRAINT RB_TheoDoi_HoanCoc;
GO

ALTER TABLE dbo.TheoDoiDonThue
ADD CONSTRAINT CK_TheoDoi_HoanCoc
CHECK (TienHoanCoc >= 0 AND (TienHoanCoc = 0 OR TienHoanCoc = TienCoc - TienKhauTru));
GO
