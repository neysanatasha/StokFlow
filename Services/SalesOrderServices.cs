using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using StokFlow.Web.Auth;
using StokFlow.Web.Data;
using StokFlow.Web.Models;

namespace StokFlow.Web.Services;

public class SalesOrderService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public SalesOrderService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    // ================= PELANGGAN =================
    public List<Customer> GetCustomers()
    {
        using var db = _dbFactory.CreateDbContext();
        return db.Customers.AsNoTracking().OrderBy(c => c.Name).ToList();
    }

    public Customer AddCustomer(string name, string phone, string address)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Nama pelanggan wajib diisi.");

        using var db = _dbFactory.CreateDbContext();
        var cleanName = name.Trim();
        if (db.Customers.Any(c => c.Name.ToLower() == cleanName.ToLower()))
            throw new InvalidOperationException($"Pelanggan {cleanName} sudah terdaftar.");

        var customer = new Customer { Name = cleanName, Phone = phone.Trim(), Address = address.Trim() };
        db.Customers.Add(customer);
        db.SaveChanges();
        return customer;
    }

    // ================= SALES ORDER =================
    public List<SalesOrder> GetAll()
    {
        using var db = _dbFactory.CreateDbContext();
        return db.SalesOrders
                 .Include(s => s.Lines)
                 .AsNoTracking()
                 .OrderByDescending(s => s.Id)
                 .ToList();
    }

    public int GetPendingCount()
    {
        using var db = _dbFactory.CreateDbContext();
        return db.SalesOrders.Count(s => s.Status == SoStatus.PendingApproval);
    }

    public SalesOrder Create(int customerId, List<SoLineInput> inputs, string note, ClaimsPrincipal user)
    {
        using var db = _dbFactory.CreateDbContext();
        var customer = db.Customers.FirstOrDefault(c => c.Id == customerId)
            ?? throw new InvalidOperationException("Pilih pelanggan terlebih dahulu.");

        var lines = new List<SalesOrderLine>();
        foreach (var input in inputs)
        {
            var product = db.Products.FirstOrDefault(p => p.Sku == input.Sku)
                ?? throw new InvalidOperationException($"Produk {input.Sku} tidak ditemukan.");
            lines.Add(new SalesOrderLine(product, input.Quantity));
        }

        var so = new SalesOrder(customer, lines, note, user.Username(), user.FullName());
        db.SalesOrders.Add(so);
        db.SaveChanges();
        return so;
    }

    public void Approve(int id, ClaimsPrincipal user)
    {
        EnsureCanApprove(user);
        using var db = _dbFactory.CreateDbContext();
        var so = LoadSo(db, id);
        so.Approve(user.Username(), user.FullName());
        db.SaveChanges();
    }

    public void Reject(int id, string reason, ClaimsPrincipal user)
    {
        EnsureCanApprove(user);
        using var db = _dbFactory.CreateDbContext();
        var so = LoadSo(db, id);
        so.Reject(user.Username(), user.FullName(), reason);
        db.SaveChanges();
    }

    // Kirim barang: semua stok berkurang SEKALIGUS
    public void Ship(int id, ClaimsPrincipal user)
    {
        using var db = _dbFactory.CreateDbContext();
        var so = LoadSo(db, id);

        so.MarkShipped(user.Username(), user.FullName());

        foreach (var line in so.Lines)
        {
            var product = db.Products.Include(p => p.Movements).FirstOrDefault(p => p.Sku == line.ProductSku)
                ?? throw new InvalidOperationException($"Produk {line.ProductName} sudah tidak ada.");

            // Stok DIPERIKSA ULANG di sini. Kalau kurang, semuanya batal (tidak ada yang tersimpan).
            product.StockOut(line.Quantity, MovementReason.Terjual, so.Number);
        }

        db.SaveChanges();
    }

    private static SalesOrder LoadSo(AppDbContext db, int id) =>
        db.SalesOrders.Include(s => s.Lines).FirstOrDefault(s => s.Id == id)
        ?? throw new InvalidOperationException("SO tidak ditemukan.");

    private static void EnsureCanApprove(ClaimsPrincipal user)
    {
        if (!user.CanApprove())
            throw new InvalidOperationException("Hanya Manajer atau Admin yang boleh memutuskan SO.");
    }
}