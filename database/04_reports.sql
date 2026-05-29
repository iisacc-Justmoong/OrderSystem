-- Daily sales report
SELECT
    CAST(o.CreatedAt AS DATE) AS SalesDate,
    COUNT(*) AS OrderCount,
    SUM(o.TotalAmount) AS TotalRevenue
FROM Orders o
WHERE o.Status <> 5
GROUP BY CAST(o.CreatedAt AS DATE)
ORDER BY SalesDate DESC;

-- Best-selling products
SELECT
    p.Sku,
    p.Name,
    SUM(oi.Quantity) AS TotalSold,
    SUM(oi.LineTotal) AS TotalRevenue
FROM OrderItems oi
INNER JOIN Products p
    ON p.Id = oi.ProductId
INNER JOIN Orders o
    ON o.Id = oi.OrderId
WHERE o.Status <> 5
GROUP BY p.Sku, p.Name
ORDER BY TotalSold DESC;

-- Current inventory status
SELECT
    p.Sku,
    p.Name,
    i.Quantity,
    p.Price,
    i.UpdatedAt
FROM Inventory i
INNER JOIN Products p
    ON p.Id = i.ProductId
ORDER BY p.Sku;

-- Customer order history
SELECT
    c.Name AS CustomerName,
    c.Email,
    o.OrderNumber,
    o.Status,
    o.TotalAmount,
    o.CreatedAt
FROM Orders o
INNER JOIN Customers c
    ON c.Id = o.CustomerId
ORDER BY o.CreatedAt DESC;

-- Inventory transaction audit trail
SELECT
    p.Sku,
    p.Name,
    it.QuantityChange,
    it.Reason,
    it.ReferenceType,
    it.ReferenceId,
    it.CreatedAt
FROM InventoryTransactions it
INNER JOIN Products p
    ON p.Id = it.ProductId
ORDER BY it.CreatedAt DESC;
