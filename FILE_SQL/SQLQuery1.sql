/*select * from Role
INSERT INTO Role (RoleName) VALUES ('Admin');
INSERT INTO Role (RoleName) VALUES ('User');


select * from [User]
INSERT INTO [User] (Username, [Password], FullName, Email, Phone, RoleID)
VALUES (N'user1', N'123456', N'Trần Đức Lương', N'user01@gmail.com', N'0905123456', 2);

INSERT INTO [User] (Username, [Password], FullName, Email, Phone, RoleID)
VALUES (N'admin1', N'123456', N'Admin1', N'admin1@gmail.com', N'0987421503', 1);


select * from [category]
INSERT INTO Category (CategoryName)
VALUES 
(N'Căn hộ / Chung cư'),
(N'Nhà ở riêng lẻ'),
(N'Đất nền'),
(N'Biệt thự');


UPDATE [User]
SET IsSuperAdmin = 1
WHERE Username = 'admin1';*/


-- 1) Thêm trường click_count trong Projects -> hiển thị các Projects nôỉ bật nhất -------------------------
USE DBRealEstate;
GO

ALTER TABLE Projects
ADD ClickCount INT DEFAULT 0;
GO


--2) Thêm district và sửa xóa ràng buộc --------------------
USE DBRealEstate;
GO

-- Xóa khóa ngoại cũ của CommuneWard
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_CommuneWard_Province')
    ALTER TABLE CommuneWard DROP CONSTRAINT FK_CommuneWard_Province;
GO

-- Thêm bảng District
IF OBJECT_ID('District', 'U') IS NULL
BEGIN
    CREATE TABLE District (
        DistrictID INT IDENTITY(1,1) PRIMARY KEY,
        DistrictName NVARCHAR(255) NOT NULL,
        ProvinceID INT NOT NULL,
        CONSTRAINT FK_District_Province FOREIGN KEY (ProvinceID)
            REFERENCES Province(ProvinceID)
            ON DELETE CASCADE
    );
END
GO

-- Xóa cột ProvinceID cũ và thêm cột DistrictID
IF COL_LENGTH('CommuneWard', 'ProvinceID') IS NOT NULL
    ALTER TABLE CommuneWard DROP COLUMN ProvinceID;
GO

IF COL_LENGTH('CommuneWard', 'DistrictID') IS NULL
    ALTER TABLE CommuneWard ADD DistrictID INT NULL;
GO

-- Tạo FK mới cho CommuneWard → District
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_CommuneWard_District')
    ALTER TABLE CommuneWard
    ADD CONSTRAINT FK_CommuneWard_District FOREIGN KEY (DistrictID)
        REFERENCES District(DistrictID)
        ON DELETE CASCADE;
GO


--3. Thêm dữ liệu dự án rồi chuyển status thành Đã duyệt 

/*VD như này:
update Projects set Status = N'Đã duyệt' where ProjectID >4
delete from Projects where ProjectID <=4*/

-- theem lien he 

ALTER TABLE Post ADD ContactPhone NVARCHAR(10) NULL;


