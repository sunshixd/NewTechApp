CREATE TABLE dbo.ProductTypes (
    ProductTypeID INT PRIMARY KEY IDENTITY(1,1),
    TypeName NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(255) NULL
);
GO

-- Таблица поставщиков
CREATE TABLE dbo.Suppliers (
    SupplierID INT PRIMARY KEY IDENTITY(1,1),
    SupplierName NVARCHAR(100) NOT NULL UNIQUE,
    ContactInfo NVARCHAR(255) NULL,
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

-- Таблица партнеров
CREATE TABLE dbo.Partners (
    PartnerID INT PRIMARY KEY IDENTITY(1,1),
    PartnerName NVARCHAR(100) NOT NULL UNIQUE,
    ContactPerson NVARCHAR(100) NULL,
    Phone NVARCHAR(20) NULL,
    Email NVARCHAR(100) NULL,
    Address NVARCHAR(255) NULL,
    TotalPurchaseAmount DECIMAL(18,2) DEFAULT 0,
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

-- Затем создаем таблицы, которые ссылаются на уже созданные
-- Таблица товаров
CREATE TABLE dbo.Products (
    ProductID INT PRIMARY KEY IDENTITY(1,1),
    ProductTypeID INT NULL FOREIGN KEY REFERENCES dbo.ProductTypes(ProductTypeID),
    ProductName NVARCHAR(100) NOT NULL,
    Article NVARCHAR(50) NOT NULL UNIQUE,
    SupplierID INT NULL FOREIGN KEY REFERENCES dbo.Suppliers(SupplierID),
    MinPartnerPrice DECIMAL(18,2) NOT NULL CHECK (MinPartnerPrice >= 0),
    PromoPrice DECIMAL(18,2) NULL CHECK (PromoPrice >= 0),
    CostPrice DECIMAL(18,2) NULL CHECK (CostPrice >= 0),
    ProductionTimeHours INT NOT NULL DEFAULT 0 CHECK (ProductionTimeHours >= 0),
    WorkshopNumber INT NOT NULL DEFAULT 1 CHECK (WorkshopNumber >= 1),
    Description NVARCHAR(MAX) NULL,
    ImageUrl NVARCHAR(255) NULL,
    PackageLength DECIMAL(10,2) NULL,
    PackageWidth DECIMAL(10,2) NULL,
    PackageHeight DECIMAL(10,2) NULL,
    WeightNet DECIMAL(10,2) NULL,
    WeightGross DECIMAL(10,2) NULL,
    QualityCertificateUrl NVARCHAR(255) NULL,
    StandardNumber NVARCHAR(50) NULL,
    PeopleRequired INT NULL DEFAULT 1 CHECK (PeopleRequired >= 1),
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE()
);
GO

-- Таблица пользователей
CREATE TABLE dbo.Users (
    UserID INT PRIMARY KEY IDENTITY(1,1),
    Login NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash VARBINARY(256) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Role NVARCHAR(20) NOT NULL CHECK (Role IN ('Admin', 'Manager', 'Client', 'Guest')),
    PhotoUrl NVARCHAR(255) NULL,
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

-- Таблица статусов заказов
CREATE TABLE dbo.OrderStatuses (
    StatusID INT PRIMARY KEY IDENTITY(1,1),
    StatusName NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(255) NULL
);
GO

-- Таблица заказов
CREATE TABLE dbo.Orders (
    OrderID INT PRIMARY KEY IDENTITY(1,1),
    Article NVARCHAR(50) NOT NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Новый',
    OrderDate DATE NOT NULL DEFAULT GETDATE(),
    TotalAmount DECIMAL(18,2) NOT NULL CHECK (TotalAmount >= 0),
    PartnerID INT NOT NULL FOREIGN KEY REFERENCES dbo.Partners(PartnerID),
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE()
);
GO

-- Таблица элементов заказа
CREATE TABLE dbo.OrderItems (
    OrderItemID INT PRIMARY KEY IDENTITY(1,1),
    OrderID INT NOT NULL FOREIGN KEY REFERENCES dbo.Orders(OrderID),
    ProductID INT NOT NULL FOREIGN KEY REFERENCES dbo.Products(ProductID),
    Quantity INT NOT NULL CHECK (Quantity > 0),
    UnitPrice DECIMAL(18,2) NOT NULL CHECK (UnitPrice >= 0),
    TotalPrice DECIMAL(18,2) NOT NULL CHECK (TotalPrice >= 0)
);
GO

-- Таблица материалов для товаров
CREATE TABLE dbo.ProductMaterials (
    MaterialID INT PRIMARY KEY IDENTITY(1,1),
    ProductID INT NOT NULL FOREIGN KEY REFERENCES dbo.Products(ProductID),
    MaterialName NVARCHAR(100) NOT NULL,
    RequiredQty DECIMAL(10,2) NOT NULL CHECK (RequiredQty >= 0),
    Unit NVARCHAR(20) NOT NULL DEFAULT 'шт.'
);
GO

-- Таблица истории входов (создаем последней, так как она не имеет внешних ключей на другие таблицы)
CREATE TABLE dbo.LoginHistory (
    EntryID INT PRIMARY KEY IDENTITY(1,1),
    AttemptAt DATETIME NOT NULL DEFAULT GETDATE(),
    Login NVARCHAR(50) NULL,
    Success BIT NOT NULL,
    Reason NVARCHAR(100) NULL
);
GO

-- =============================================
-- ИНДЕКСЫ ДЛЯ ПРОИЗВОДИТЕЛЬНОСТИ
-- =============================================
CREATE INDEX IX_Products_SupplierID ON dbo.Products(SupplierID);
CREATE INDEX IX_Products_ProductTypeID ON dbo.Products(ProductTypeID);
CREATE INDEX IX_Products_Article ON dbo.Products(Article);
CREATE INDEX IX_Orders_PartnerID ON dbo.Orders(PartnerID);
CREATE INDEX IX_Orders_OrderDate ON dbo.Orders(OrderDate);
CREATE INDEX IX_OrderItems_OrderID ON dbo.OrderItems(OrderID);
CREATE INDEX IX_OrderItems_ProductID ON dbo.OrderItems(ProductID);
CREATE INDEX IX_LoginHistory_Login ON dbo.LoginHistory(Login);
CREATE INDEX IX_LoginHistory_AttemptAt ON dbo.LoginHistory(AttemptAt);
GO

-- =============================================
-- ТРИГГЕРЫ
-- =============================================
CREATE TRIGGER trg_Products_UpdateDate
ON dbo.Products
AFTER UPDATE
AS
BEGIN
    UPDATE dbo.Products
    SET UpdatedAt = GETDATE()
    FROM dbo.Products p
    INNER JOIN inserted i ON p.ProductID = i.ProductID
END;
GO

CREATE TRIGGER trg_Orders_UpdateDate
ON dbo.Orders
AFTER UPDATE
AS
BEGIN
    UPDATE dbo.Orders
    SET UpdatedAt = GETDATE()
    FROM dbo.Orders o
    INNER JOIN inserted i ON o.OrderID = i.OrderID
END;
GO
