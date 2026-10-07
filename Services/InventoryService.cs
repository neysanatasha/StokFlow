using Microsoft.EntityFrameworkCore;
using StokFlow.Web.Data;
using StokFlow.Web.Models;

namespace StokFlow.Web.Services;

public class InventoryService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public InventoryService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    // ---------- Ambil data ----------
    public List<Product> GetAll()
    {
        using var db = _dbFactory.CreateDbContext();
        return db.Products
                 .Include(p => p.Movements)
                 .AsNoTracking()
                 .OrderBy(p => p.Sku)
                 .ToList();
    }

    // ---------- Tambah ----------
    public Product AddProduct(ProductForm form)
    {
        if (string.IsNullOrWhiteSpace(form.Sku) || string.IsNullOrWhiteSpace(form.Name))
            throw new InvalidOperationException("Kode dan nama produk wajib diisi.");
        if (form.Price <= 0)
            throw new InvalidOperationException("Harga harus lebih dari 0.");
        if (form.Stock < 0 || form.MinimumStock < 0)
            throw new InvalidOperationException("Stok dan batas minimum tidak boleh minus.");

        using var db = _dbFactory.CreateDbContext();
        var sku = form.Sku.Trim().ToUpper();

        if (db.Products.Any(p => p.Sku == sku))
            throw new InvalidOperationException($"Kode {sku} sudah dipakai produk lain.");

        var product = new Product(sku, form.Name.Trim(), form.Price, form.Stock, form.MinimumStock);
        db.Products.Add(product);
        db.SaveChanges();
        return product;
    }

    // ---------- Hapus ----------
    public void DeleteProduct(string sku)
    {
        using var db = _dbFactory.CreateDbContext();
        var product = db.Products.Include(p => p.Movements).FirstOrDefault(p => p.Sku == sku)
            ?? throw new InvalidOperationException("Produk tidak ditemukan.");
        db.Products.Remove(product);
        db.SaveChanges();
    }

    // ---------- Ringkasan untuk dashboard ----------
    public DashboardSummary GetSummary()
    {
        var products = GetAll();
        return new DashboardSummary
        {
            TotalProducts    = products.Count,
            OutOfStockCount  = products.Count(p => p.IsOutOfStock),
            LowStockCount    = products.Count(p => p.IsLowStock && !p.IsOutOfStock),
            TotalStockValue  = products.Sum(p => p.Price * p.Stock),
            TotalSalesValue  = products.Sum(p => p.Price * p.TotalSold),
            TotalWastedValue = products.Sum(p => p.Price * p.TotalWasted),
            NeedRestock      = products.Where(p => p.IsLowStock).OrderBy(p => p.Stock).ToList()
        };
    }
}

// Wadah angka-angka untuk halaman Dashboard
public class DashboardSummary
{
    public int TotalProducts { get; set; }
    public int OutOfStockCount { get; set; }
    public int LowStockCount { get; set; }
    public decimal TotalStockValue { get; set; }
    public decimal TotalSalesValue { get; set; }
    public decimal TotalWastedValue { get; set; }
    public List<Product> NeedRestock { get; set; } = new();
}