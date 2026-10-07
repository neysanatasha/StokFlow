using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using StokFlow.Web.Auth;
using StokFlow.Web.Data;
using StokFlow.Web.Models;

namespace StokFlow.Web.Services;

public class PurchaseOrderService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public PurchaseOrderService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    // ================= PEMASOK =================
    public List<Supplier> GetSuppliers()
    {
        using var db = _dbFactory.CreateDbContext();
        return db.Suppliers.AsNoTracking().OrderBy(s => s.Name).ToList();
    }

    public Supplier AddSupplier(string name, string phone, string address, ClaimsPrincipal user)
    {
        if (!user.CanApprove())
            throw new InvalidOperationException("Hanya Manajer atau Admin yang boleh menambah pemasok.");
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Nama pemasok wajib diisi.");

        using var db = _dbFactory.CreateDbContext();
        var cleanName = name.Trim();
        if (db.Suppliers.Any(s => s.Name.ToLower() == cleanName.ToLower()))
            throw new InvalidOperationException($"Pemasok {cleanName} sudah terdaftar.");

        var supplier = new Supplier { Name = cleanName, Phone = phone.Trim(), Address = address.Trim() };
        db.Suppliers.Add(supplier);
        db.SaveChanges();
        return supplier;
    }

    // ================= PURCHASE ORDER =================
    public List<PurchaseOrder> GetAll()
    {
        using var db = _dbFactory.CreateDbContext();
        return db.PurchaseOrders
                 .Include(p => p.Lines)
                 .AsNoTracking()
                 .OrderByDescending(p => p.Id)
                 .ToList();
    }

    public int GetPendingCount()
    {
        using var db = _dbFactory.CreateDbContext();
        return db.PurchaseOrders.Count(p => p.Status == PoStatus.PendingApproval);
    }

    public PurchaseOrder Create(int supplierId, List<PoLineInput> inputs, string note, ClaimsPrincipal user)
    {
        using var db = _dbFactory.CreateDbContext();
        var supplier = db.Suppliers.FirstOrDefault(s => s.Id == supplierId)
            ?? throw new InvalidOperationException("Pilih pemasok terlebih dahulu.");

        var lines = new List<PurchaseOrderLine>();
        foreach (var input in inputs)
        {
            var product = db.Products.FirstOrDefault(p => p.Sku == input.Sku)
                ?? throw new InvalidOperationException($"Produk {input.Sku} tidak ditemukan.");
            lines.Add(new PurchaseOrderLine(product, input.Quantity, input.UnitCost));
        }

        var po = new PurchaseOrder(supplier, lines, note, user.Username(), user.FullName());
        db.PurchaseOrders.Add(po);
        db.SaveChanges();
        return po;
    }

    public void Approve(int id, ClaimsPrincipal user)
    {
        EnsureCanApprove(user);
        using var db = _dbFactory.CreateDbContext();
        var po = LoadPo(db, id);
        po.Approve(user.Username(), user.FullName());
        db.SaveChanges();
    }

    public void Reject(int id, string reason, ClaimsPrincipal user)
    {
        EnsureCanApprove(user);
        using var db = _dbFactory.CreateDbContext();
        var po = LoadPo(db, id);
        po.Reject(user.Username(), user.FullName(), reason);
        db.SaveChanges();
    }

    // Barang datang: semua stok bertambah SEKALIGUS
    public void Receive(int id, ClaimsPrincipal user)
    {
        using var db = _dbFactory.CreateDbContext();
        var po = LoadPo(db, id);

        po.MarkReceived(user.Username(), user.FullName());

        foreach (var line in po.Lines)
        {
            var product = db.Products.Include(p => p.Movements).FirstOrDefault(p => p.Sku == line.ProductSku)
                ?? throw new InvalidOperationException($"Produk {line.ProductName} sudah tidak ada.");
            product.StockIn(line.Quantity, po.Number);   // kartu stok mencatat nomor PO
        }

        // Status PO + stok semua produk + kartu stok tersimpan bersamaan
        db.SaveChanges();
    }

    private static PurchaseOrder LoadPo(AppDbContext db, int id) =>
        db.PurchaseOrders.Include(p => p.Lines).FirstOrDefault(p => p.Id == id)
        ?? throw new InvalidOperationException("PO tidak ditemukan.");

    private static void EnsureCanApprove(ClaimsPrincipal user)
    {
        if (!user.CanApprove())
            throw new InvalidOperationException("Hanya Manajer atau Admin yang boleh memutuskan PO.");
    }
}