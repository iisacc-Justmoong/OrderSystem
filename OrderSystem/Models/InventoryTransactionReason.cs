namespace OrderSystem.Models;

public enum InventoryTransactionReason
{
    StockReceived = 0,
    OrderCreated = 1,
    OrderCancelled = 2,
    ManualAdjustment = 3
}
