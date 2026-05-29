using Microsoft.EntityFrameworkCore;
using OrderSystem.Models;

namespace OrderSystem.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<Inventory> Inventories => Set<Inventory>();

    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();

    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();

    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(product => product.Sku).HasMaxLength(64).IsRequired();
            entity.Property(product => product.Name).HasMaxLength(200).IsRequired();
            entity.Property(product => product.Description).HasMaxLength(1000);
            entity.Property(product => product.Price).HasColumnType("decimal(18,2)");
            entity.HasIndex(product => product.Sku).IsUnique();
            entity.HasIndex(product => product.Name);
            entity.HasOne(product => product.Inventory)
                .WithOne(inventory => inventory.Product)
                .HasForeignKey<Inventory>(inventory => inventory.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(customer => customer.Name).HasMaxLength(100).IsRequired();
            entity.Property(customer => customer.Email).HasMaxLength(255).IsRequired();
            entity.Property(customer => customer.Phone).HasMaxLength(50);
            entity.HasIndex(customer => customer.Email).IsUnique();
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(order => order.OrderNumber).HasMaxLength(50).IsRequired();
            entity.Property(order => order.Status)
                .HasConversion<int>();
            entity.Property(order => order.TotalAmount).HasColumnType("decimal(18,2)");
            entity.HasIndex(order => order.OrderNumber).IsUnique();
            entity.HasIndex(order => order.CustomerId);
            entity.HasIndex(order => order.Status);
            entity.HasIndex(order => order.CreatedAt);
            entity.HasOne(order => order.Customer)
                .WithMany(customer => customer.Orders)
                .HasForeignKey(order => order.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(order => order.Items)
                .WithOne(item => item.Order)
                .HasForeignKey(item => item.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(order => order.StatusHistories)
                .WithOne(history => history.Order)
                .HasForeignKey(history => history.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(order => order.Payments)
                .WithOne(payment => payment.Order)
                .HasForeignKey(payment => payment.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.Property(item => item.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(item => item.LineTotal).HasColumnType("decimal(18,2)");
            entity.ToTable(table => table.HasCheckConstraint("CK_OrderItems_Quantity", "Quantity > 0"));
            entity.HasIndex(item => item.OrderId);
            entity.HasOne(item => item.Product)
                .WithMany(product => product.OrderItems)
                .HasForeignKey(item => item.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(inventory => inventory.ProductId);
            entity.ToTable(table => table.HasCheckConstraint("CK_Inventory_Quantity", "Quantity >= 0"));
        });

        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.Property(transaction => transaction.Reason).HasConversion<int>();
            entity.Property(transaction => transaction.ReferenceType).HasMaxLength(50);
            entity.HasIndex(transaction => transaction.ProductId);
            entity.HasOne(transaction => transaction.Product)
                .WithMany(product => product.InventoryTransactions)
                .HasForeignKey(transaction => transaction.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.Property(history => history.FromStatus).HasConversion<int?>();
            entity.Property(history => history.ToStatus).HasConversion<int>();
            entity.Property(history => history.Reason).HasMaxLength(500);
            entity.HasIndex(history => history.OrderId);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.Property(payment => payment.PaymentMethod).HasConversion<int>();
            entity.Property(payment => payment.Status).HasConversion<int>();
            entity.Property(payment => payment.Amount).HasColumnType("decimal(18,2)");
            entity.HasIndex(payment => payment.OrderId);
        });
    }
}
