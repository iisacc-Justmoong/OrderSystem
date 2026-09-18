using Xunit;

namespace OrderSystem.Tests;

public sealed class DatabaseScriptContractTests
{
    private static readonly string RepositoryRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    [Fact]
    public void DatabaseScripts_AreNumberedInExecutionOrder()
    {
        var databaseDirectory = Path.Combine(RepositoryRoot, "database");

        Assert.True(Directory.Exists(databaseDirectory));
        Assert.True(File.Exists(Path.Combine(databaseDirectory, "README.md")));
        Assert.True(File.Exists(Path.Combine(databaseDirectory, "01_schema.sql")));
        Assert.True(File.Exists(Path.Combine(databaseDirectory, "02_sample-data.sql")));
        Assert.True(File.Exists(Path.Combine(databaseDirectory, "03_procedures.sql")));
        Assert.True(File.Exists(Path.Combine(databaseDirectory, "04_reports.sql")));
        Assert.True(File.Exists(Path.Combine(databaseDirectory, "INTERVIEW_NOTES.md")));

        var readme = File.ReadAllText(Path.Combine(databaseDirectory, "README.md"));
        Assert.Contains("01_schema.sql", readme);
        Assert.Contains("02_sample-data.sql", readme);
        Assert.Contains("03_procedures.sql", readme);
        Assert.Contains("04_reports.sql", readme);
        Assert.Contains("INTERVIEW_NOTES.md", readme);
    }

    [Fact]
    public void SchemaScript_DefinesRetailOrderLedgerTablesAndIndexes()
    {
        var schema = ReadDatabaseScript("01_schema.sql");

        foreach (var table in new[]
                 {
                     "Customers",
                     "Products",
                     "Inventory",
                     "Orders",
                     "OrderItems",
                     "InventoryTransactions",
                     "OrderStatusHistories",
                     "Payments"
                 })
        {
            Assert.Contains($"CREATE TABLE {table}", schema);
        }

        Assert.Contains("CHECK (Quantity >= 0)", schema);
        Assert.Contains("CHECK (Quantity > 0)", schema);
        Assert.Contains("CREATE INDEX IX_Orders_CustomerId", schema);
        Assert.Contains("CREATE INDEX IX_Orders_Status", schema);
        Assert.Contains("CREATE INDEX IX_InventoryTransactions_ProductId", schema);
    }

    [Fact]
    public void SampleDataScript_SeedsMasterDataAndInitialInventoryOnly()
    {
        var sampleData = ReadDatabaseScript("02_sample-data.sql");

        Assert.Contains("INSERT INTO Customers", sampleData);
        Assert.Contains("INSERT INTO Products", sampleData);
        Assert.Contains("INSERT INTO Inventory", sampleData);
        Assert.Contains("InitialStock", sampleData);
        Assert.DoesNotContain("INSERT INTO Orders", sampleData);
        Assert.DoesNotContain("INSERT INTO OrderItems", sampleData);
    }

    [Fact]
    public void ProcedureScript_ContainsTransactionalBusinessOperations()
    {
        var procedures = ReadDatabaseScript("03_procedures.sql");

        Assert.Contains("CREATE OR ALTER PROCEDURE AdjustInventory", procedures);
        Assert.Contains("CREATE OR ALTER PROCEDURE CreateOrder", procedures);
        Assert.Contains("CREATE OR ALTER PROCEDURE CancelOrder", procedures);
        Assert.Contains("BEGIN TRANSACTION", procedures);
        Assert.Contains("ROLLBACK TRANSACTION", procedures);
        Assert.Contains("COMMIT TRANSACTION", procedures);
        Assert.Contains("THROW 50001", procedures);
    }

    [Fact]
    public void ReportsScript_ContainsRetailReportingQueries()
    {
        var reports = ReadDatabaseScript("04_reports.sql");

        Assert.Contains("Daily sales report", reports);
        Assert.Contains("Best-selling products", reports);
        Assert.Contains("Current inventory status", reports);
        Assert.Contains("Customer order history", reports);
        Assert.Contains("GROUP BY", reports);
        Assert.Contains("INNER JOIN", reports);
    }

    [Fact]
    public void InterviewNotes_ExplainDatabaseDesignForRetailOrderSystem()
    {
        var notes = ReadDatabaseScript("INTERVIEW_NOTES.md");

        Assert.Contains("온라인 주문 및 소매 관리 시스템", notes);
        Assert.Contains("Orders와 OrderItems를 1:N 관계", notes);
        Assert.Contains("OrderItems에 UnitPrice를 별도로 저장", notes);
        Assert.Contains("InventoryTransactions", notes);
        Assert.Contains("OrderStatusHistories", notes);
        Assert.Contains("하나의 트랜잭션", notes);
        Assert.Contains("CustomerId, Status, CreatedAt", notes);
        Assert.Contains("1분 답변", notes);
    }

    private static string ReadDatabaseScript(string fileName)
    {
        return File.ReadAllText(Path.Combine(RepositoryRoot, "database", fileName));
    }
}
