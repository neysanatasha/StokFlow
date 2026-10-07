using Microsoft.EntityFrameworkCore;
using StokFlow.Web.Models;

namespace StokFlow.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<StockRequest> StockRequests => Set<StockRequest>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderLine> SalesOrderLines => Set<SalesOrderLine>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Product>(e =>
        {
            e.HasKey(p => p.Sku);
            e.Property(p => p.Name).HasMaxLength(200);
            e.Ignore(p => p.IsLowStock);
            e.Ignore(p => p.IsOutOfStock);
            e.Ignore(p => p.TotalSold);
            e.Ignore(p => p.TotalWasted);
            e.HasMany(p => p.Movements)
             .WithOne()
             .HasForeignKey("ProductSku")
             .OnDelete(DeleteBehavior.Cascade);
            e.Navigation(p => p.Movements).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        b.Entity<StockMovement>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Reason).HasConversion<string>();
            e.Ignore(m => m.ReasonText);
            e.Ignore(m => m.Description);
        });

        b.Entity<AppUser>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Username).HasMaxLength(50);
            e.Property(u => u.FullName).HasMaxLength(100);
            e.Property(u => u.Role).HasConversion<string>();
        });

        b.Entity<StockRequest>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Reason).HasConversion<string>();
            e.Property(r => r.Status).HasConversion<string>();
            e.Ignore(r => r.Number);
            e.Ignore(r => r.IsStockIn);
            e.Ignore(r => r.ReasonText);
        });

        b.Entity<Supplier>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.Name).IsUnique();
            e.Property(s => s.Name).HasMaxLength(150);
        });

        b.Entity<PurchaseOrder>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Status).HasConversion<string>();
            e.Ignore(p => p.Total);
            e.Ignore(p => p.TotalQuantity);
            e.Ignore(p => p.Number);
            e.Ignore(p => p.StatusText);
            e.HasMany(p => p.Lines)
             .WithOne()
             .HasForeignKey("PurchaseOrderId")
             .OnDelete(DeleteBehavior.Cascade);
            e.Navigation(p => p.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        b.Entity<PurchaseOrderLine>(e =>
        {
            e.HasKey(l => l.Id);
            e.Ignore(l => l.Subtotal);
        });

        b.Entity<Customer>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.Name).IsUnique();
            e.Property(c => c.Name).HasMaxLength(150);
        });

        b.Entity<SalesOrder>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Status).HasConversion<string>();
            e.Ignore(s => s.Total);
            e.Ignore(s => s.TotalQuantity);
            e.Ignore(s => s.Number);
            e.Ignore(s => s.StatusText);
            e.Ignore(s => s.IsAutoApproved);
            e.HasMany(s => s.Lines)
             .WithOne()
             .HasForeignKey("SalesOrderId")
             .OnDelete(DeleteBehavior.Cascade);
            e.Navigation(s => s.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        b.Entity<SalesOrderLine>(e =>
        {
            e.HasKey(l => l.Id);
            e.Ignore(l => l.Subtotal);
        });
    }
}