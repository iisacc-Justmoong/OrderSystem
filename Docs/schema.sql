CREATE TABLE Products (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    Sku NVARCHAR(64) NOT NULL UNIQUE,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(1000) NULL,
    Price DECIMAL(18, 2) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL
);

CREATE TABLE Customers (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    Phone NVARCHAR(50) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE Orders (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    OrderNumber NVARCHAR(50) NOT NULL UNIQUE,
    CustomerId BIGINT NOT NULL,
    Status INT NOT NULL,
    TotalAmount DECIMAL(18, 2) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,

    CONSTRAINT FK_Orders_Customers
        FOREIGN KEY (CustomerId) REFERENCES Customers(Id)
);

CREATE TABLE OrderItems (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    OrderId BIGINT NOT NULL,
    ProductId BIGINT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18, 2) NOT NULL,
    LineTotal DECIMAL(18, 2) NOT NULL,

    CONSTRAINT FK_OrderItems_Orders
        FOREIGN KEY (OrderId) REFERENCES Orders(Id),

    CONSTRAINT FK_OrderItems_Products
        FOREIGN KEY (ProductId) REFERENCES Products(Id),

    CONSTRAINT CK_OrderItems_Quantity
        CHECK (Quantity > 0)
);

CREATE TABLE Inventory (
    ProductId BIGINT PRIMARY KEY,
    Quantity INT NOT NULL,
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_Inventory_Products
        FOREIGN KEY (ProductId) REFERENCES Products(Id),

    CONSTRAINT CK_Inventory_Quantity
        CHECK (Quantity >= 0)
);

CREATE TABLE InventoryTransactions (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    ProductId BIGINT NOT NULL,
    QuantityChange INT NOT NULL,
    Reason INT NOT NULL,
    ReferenceType NVARCHAR(50) NULL,
    ReferenceId BIGINT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_InventoryTransactions_Products
        FOREIGN KEY (ProductId) REFERENCES Products(Id)
);

CREATE TABLE OrderStatusHistories (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    OrderId BIGINT NOT NULL,
    FromStatus INT NULL,
    ToStatus INT NOT NULL,
    Reason NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_OrderStatusHistories_Orders
        FOREIGN KEY (OrderId) REFERENCES Orders(Id)
);

CREATE TABLE Payments (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    OrderId BIGINT NOT NULL,
    PaymentMethod INT NOT NULL,
    Status INT NOT NULL,
    Amount DECIMAL(18, 2) NOT NULL,
    PaidAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_Payments_Orders
        FOREIGN KEY (OrderId) REFERENCES Orders(Id)
);

CREATE INDEX IX_Orders_CustomerId ON Orders(CustomerId);
CREATE INDEX IX_Orders_Status ON Orders(Status);
CREATE INDEX IX_Orders_CreatedAt ON Orders(CreatedAt);
CREATE INDEX IX_OrderItems_OrderId ON OrderItems(OrderId);
CREATE INDEX IX_InventoryTransactions_ProductId ON InventoryTransactions(ProductId);
CREATE INDEX IX_OrderStatusHistories_OrderId ON OrderStatusHistories(OrderId);
CREATE INDEX IX_Payments_OrderId ON Payments(OrderId);
CREATE INDEX IX_Products_Name ON Products(Name);
