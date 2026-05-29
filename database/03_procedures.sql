IF OBJECT_ID('CancelOrder', 'P') IS NOT NULL DROP PROCEDURE CancelOrder;
IF OBJECT_ID('CreateOrder', 'P') IS NOT NULL DROP PROCEDURE CreateOrder;
IF OBJECT_ID('AdjustInventory', 'P') IS NOT NULL DROP PROCEDURE AdjustInventory;
GO

IF TYPE_ID('OrderItemRequest') IS NOT NULL DROP TYPE OrderItemRequest;
GO

CREATE TYPE OrderItemRequest AS TABLE (
    ProductId BIGINT NOT NULL,
    Quantity INT NOT NULL CHECK (Quantity > 0)
);
GO

CREATE OR ALTER PROCEDURE AdjustInventory
    @ProductId BIGINT,
    @QuantityChange INT,
    @Reason NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    UPDATE Inventory
    SET Quantity = Quantity + @QuantityChange,
        UpdatedAt = SYSUTCDATETIME()
    WHERE ProductId = @ProductId
      AND Quantity + @QuantityChange >= 0;

    IF @@ROWCOUNT = 0
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50001, 'Inventory adjustment failed. Product not found or insufficient stock.', 1;
    END;

    INSERT INTO InventoryTransactions
        (ProductId, QuantityChange, Reason, ReferenceType, ReferenceId)
    VALUES
        (@ProductId, @QuantityChange, @Reason, 'ManualAdjustment', NULL);

    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE CreateOrder
    @CustomerId BIGINT,
    @Items OrderItemRequest READONLY,
    @PaymentMethod NVARCHAR(50) = NULL,
    @OrderId BIGINT OUTPUT,
    @OrderNumber NVARCHAR(50) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM Customers WHERE Id = @CustomerId)
    BEGIN
        THROW 50002, 'Customer was not found.', 1;
    END;

    IF NOT EXISTS (SELECT 1 FROM @Items)
    BEGIN
        THROW 50003, 'Order must contain at least one item.', 1;
    END;

    IF EXISTS (
        SELECT 1
        FROM @Items i
        LEFT JOIN Products p ON p.Id = i.ProductId
        WHERE p.Id IS NULL OR p.IsActive = 0
    )
    BEGIN
        THROW 50004, 'Order contains a missing or inactive product.', 1;
    END;

    BEGIN TRANSACTION;

    DECLARE @Now DATETIME2 = SYSUTCDATETIME();
    DECLARE @TotalAmount DECIMAL(18, 2);

    SELECT @TotalAmount = SUM(i.Quantity * p.Price)
    FROM @Items i
    INNER JOIN Products p ON p.Id = i.ProductId;

    SET @OrderNumber = CONCAT(
        'ORD-',
        FORMAT(@Now, 'yyyyMMddHHmmssfff'),
        '-',
        RIGHT(CONVERT(NVARCHAR(36), NEWID()), 8)
    );

    INSERT INTO Orders (OrderNumber, CustomerId, Status, TotalAmount, CreatedAt, UpdatedAt)
    VALUES (@OrderNumber, @CustomerId, 0, @TotalAmount, @Now, @Now);

    SET @OrderId = CONVERT(BIGINT, SCOPE_IDENTITY());

    UPDATE inv
    SET inv.Quantity = inv.Quantity - itemTotals.Quantity,
        inv.UpdatedAt = @Now
    FROM Inventory inv
    INNER JOIN (
        SELECT ProductId, SUM(Quantity) AS Quantity
        FROM @Items
        GROUP BY ProductId
    ) itemTotals ON itemTotals.ProductId = inv.ProductId
    WHERE inv.Quantity >= itemTotals.Quantity;

    IF @@ROWCOUNT <> (SELECT COUNT(DISTINCT ProductId) FROM @Items)
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50005, 'Order creation failed because one or more products have insufficient stock.', 1;
    END;

    INSERT INTO OrderItems (OrderId, ProductId, Quantity, UnitPrice, LineTotal)
    SELECT
        @OrderId,
        i.ProductId,
        i.Quantity,
        p.Price,
        i.Quantity * p.Price
    FROM @Items i
    INNER JOIN Products p ON p.Id = i.ProductId;

    INSERT INTO InventoryTransactions (ProductId, QuantityChange, Reason, ReferenceType, ReferenceId, CreatedAt)
    SELECT ProductId, -SUM(Quantity), 'OrderCreated', 'Order', @OrderId, @Now
    FROM @Items
    GROUP BY ProductId;

    INSERT INTO OrderStatusHistories (OrderId, FromStatus, ToStatus, Reason, CreatedAt)
    VALUES (@OrderId, NULL, 0, 'Order created', @Now);

    IF @PaymentMethod IS NOT NULL
    BEGIN
        INSERT INTO Payments (OrderId, PaymentMethod, Status, Amount, PaidAt, CreatedAt)
        VALUES (@OrderId, @PaymentMethod, 'Paid', @TotalAmount, @Now, @Now);
    END;

    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE CancelOrder
    @OrderId BIGINT,
    @Reason NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @CurrentStatus INT;
    DECLARE @Now DATETIME2 = SYSUTCDATETIME();

    SELECT @CurrentStatus = Status
    FROM Orders
    WHERE Id = @OrderId;

    IF @CurrentStatus IS NULL
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50006, 'Order was not found.', 1;
    END;

    IF @CurrentStatus NOT IN (0, 1)
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50007, 'Order cannot be cancelled after fulfillment has started.', 1;
    END;

    UPDATE Orders
    SET Status = 5,
        UpdatedAt = @Now
    WHERE Id = @OrderId;

    UPDATE inv
    SET inv.Quantity = inv.Quantity + oi.Quantity,
        inv.UpdatedAt = @Now
    FROM Inventory inv
    INNER JOIN OrderItems oi ON oi.ProductId = inv.ProductId
    WHERE oi.OrderId = @OrderId;

    INSERT INTO InventoryTransactions (ProductId, QuantityChange, Reason, ReferenceType, ReferenceId, CreatedAt)
    SELECT ProductId, Quantity, 'OrderCancelled', 'Order', @OrderId, @Now
    FROM OrderItems
    WHERE OrderId = @OrderId;

    INSERT INTO OrderStatusHistories (OrderId, FromStatus, ToStatus, Reason, CreatedAt)
    VALUES (@OrderId, @CurrentStatus, 5, COALESCE(@Reason, 'Order cancelled'), @Now);

    COMMIT TRANSACTION;
END;
GO
