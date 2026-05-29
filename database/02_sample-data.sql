INSERT INTO Customers (Name, Email, Phone)
VALUES
    ('Kim Minsoo', 'minsoo@example.com', '010-1111-2222'),
    ('Lee Jiyoon', 'jiyoon@example.com', '010-3333-4444');
GO

INSERT INTO Products (Sku, Name, Description, Price)
VALUES
    ('TSHIRT-BLK-M', 'Black T-Shirt M', 'Black cotton t-shirt, medium size', 29000),
    ('TSHIRT-WHT-L', 'White T-Shirt L', 'White cotton t-shirt, large size', 29000),
    ('JEANS-BLU-32', 'Blue Jeans 32', 'Classic blue denim jeans, waist 32', 59000),
    ('CAP-BLK-FREE', 'Black Cap Free Size', 'Adjustable black cap', 19000);
GO

INSERT INTO Inventory (ProductId, Quantity)
SELECT Id, 50
FROM Products;
GO

INSERT INTO InventoryTransactions (ProductId, QuantityChange, Reason, ReferenceType, ReferenceId)
SELECT Id, 50, 'InitialStock', 'Seed', NULL
FROM Products;
GO
