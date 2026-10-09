using Invoice.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Invoice.Infrastructure.Persistence;

/// <summary>
/// سياق قاعدة البيانات — جسر بين كود C# وجداول SQL Server.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // ========= الجداول =========

    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Invoice.Domain.Entities.Invoice> Invoices => Set<Invoice.Domain.Entities.Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<InvoicePayment> InvoicePayments => Set<InvoicePayment>();
    public DbSet<InvoiceDiscount> InvoiceDiscounts => Set<InvoiceDiscount>();
    public DbSet<InvoiceExpense> InvoiceExpenses => Set<InvoiceExpense>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ========= إعدادات Supplier =========

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.ToTable("Suppliers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.RowVersion).IsRowVersion();
        });

        // ========= إعدادات Product =========

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Pattern).HasMaxLength(100);
            entity.Property(e => e.Ah).HasPrecision(18, 4);
            entity.Property(e => e.L).HasPrecision(18, 4);
            entity.Property(e => e.W).HasPrecision(18, 4);
            entity.Property(e => e.H).HasPrecision(18, 4);
            entity.Property(e => e.Oh).HasPrecision(18, 4);
            entity.Property(e => e.LayoutImageFilename).HasMaxLength(200);
            entity.Property(e => e.RowVersion).IsRowVersion();

            entity.HasOne(e => e.Supplier)
                  .WithMany()
                  .HasForeignKey(e => e.SupplierId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ========= إعدادات Invoice =========

        modelBuilder.Entity<Invoice.Domain.Entities.Invoice>(entity =>
        {
            entity.ToTable("Invoices");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InvoiceNo).IsRequired().HasMaxLength(30);
            entity.HasIndex(e => e.InvoiceNo).IsUnique();
            entity.Property(e => e.SupplierInvoiceNo).HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.DisplayFields).HasMaxLength(4000);
            entity.Property(e => e.Notes).HasMaxLength(4000);
            entity.Property(e => e.Notes2).HasMaxLength(4000);

            entity.Property(e => e.TotalPayments).HasPrecision(18, 2);
            entity.Property(e => e.TotalDiscounts).HasPrecision(18, 2);
            entity.Property(e => e.NetPayments).HasPrecision(18, 2);
            entity.Property(e => e.TotalExpenses).HasPrecision(18, 2);
            entity.Property(e => e.NetCost).HasPrecision(18, 2);
            entity.Property(e => e.TotalUsd).HasPrecision(18, 2);
            entity.Property(e => e.ExchangeRate).HasPrecision(18, 6);
            entity.Property(e => e.TotalSales).HasPrecision(18, 2);
            entity.Property(e => e.TotalProfit).HasPrecision(18, 2);
            entity.Property(e => e.ProfitMargin).HasPrecision(9, 4);

            entity.Property(e => e.RowVersion).IsRowVersion();

            entity.HasOne(e => e.Supplier)
                  .WithMany()
                  .HasForeignKey(e => e.SupplierId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ========= إعدادات InvoiceItem =========

        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.ToTable("InvoiceItems");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            entity.Property(e => e.PurchaseUsd).HasPrecision(18, 4);
            entity.Property(e => e.PurchaseDzd).HasPrecision(18, 4);
            entity.Property(e => e.SaleDzd).HasPrecision(18, 4);
            entity.Property(e => e.TotalUsd).HasPrecision(18, 2);
            entity.Property(e => e.TotalDzd).HasPrecision(18, 2);
            entity.Property(e => e.TotalSale).HasPrecision(18, 2);
            entity.Property(e => e.UnitProfit).HasPrecision(18, 2);
            entity.Property(e => e.TotalProfit).HasPrecision(18, 2);

            entity.HasOne(e => e.Invoice)
                  .WithMany(i => i.Items)
                  .HasForeignKey(e => e.InvoiceId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                  .WithMany()
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ========= إعدادات InvoicePayment =========

        modelBuilder.Entity<InvoicePayment>(entity =>
        {
            entity.ToTable("InvoicePayments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Label).HasMaxLength(200);
            entity.Property(e => e.Amount).HasPrecision(18, 2);

            entity.HasOne(e => e.Invoice)
                  .WithMany(i => i.Payments)
                  .HasForeignKey(e => e.InvoiceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ========= إعدادات InvoiceDiscount =========

        modelBuilder.Entity<InvoiceDiscount>(entity =>
        {
            entity.ToTable("InvoiceDiscounts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Label).HasMaxLength(200);
            entity.Property(e => e.Amount).HasPrecision(18, 2);

            entity.HasOne(e => e.Invoice)
                  .WithMany(i => i.Discounts)
                  .HasForeignKey(e => e.InvoiceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ========= إعدادات InvoiceExpense =========

        modelBuilder.Entity<InvoiceExpense>(entity =>
        {
            entity.ToTable("InvoiceExpenses");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Label).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Amount).HasPrecision(18, 2);

            entity.HasOne(e => e.Invoice)
                  .WithMany(i => i.Expenses)
                  .HasForeignKey(e => e.InvoiceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}