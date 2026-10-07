using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using StokFlow.Web.Auth;
using StokFlow.Web.Data;
using StokFlow.Web.Models;

namespace StokFlow.Web.Services;

public class ApprovalService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public ApprovalService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    // ---------- Mengajukan ----------
    public StockRequest CreateRequest(string sku, MovementReason reason, int quantity, string note, ClaimsPrincipal user)
    {
        using var db = _dbFactory.CreateDbContext();
        var product = db.Products.FirstOrDefault(p => p.Sku == sku)
            ?? throw new InvalidOperationException("Produk tidak ditemukan.");

        var request = new StockRequest(product, reason, quantity, note, user.Username(), user.FullName());
        db.StockRequests.Add(request);
        db.SaveChanges();
        return request;
    }

    // ---------- Membaca ----------
    public List<StockRequest> GetPending()
    {
        using var db = _dbFactory.CreateDbContext();
        return db.StockRequests.AsNoTracking()
                 .Where(r => r.Status == RequestStatus.Pending)
                 .OrderBy(r => r.RequestedAt)
                 .ToList();
    }

    public List<StockRequest> GetHistory(int take = 50)
    {
        using var db = _dbFactory.CreateDbContext();
        return db.StockRequests.AsNoTracking()
                 .Where(r => r.Status != RequestStatus.Pending)
                 .OrderByDescending(r => r.DecidedAt)
                 .Take(take)
                 .ToList();
    }

    public int GetPendingCount()
    {
        using var db = _dbFactory.CreateDbContext();
        return db.StockRequests.Count(r => r.Status == RequestStatus.Pending);
    }

    // ---------- Menyetujui ----------
    public void Approve(int requestId, ClaimsPrincipal user)
    {
        EnsureCanApprove(user);

        using var db = _dbFactory.CreateDbContext();
        var request = db.StockRequests.FirstOrDefault(r => r.Id == requestId)
            ?? throw new InvalidOperationException("Pengajuan tidak ditemukan.");
        var product = db.Products.Include(p => p.Movements).FirstOrDefault(p => p.Sku == request.ProductSku)
            ?? throw new InvalidOperationException("Produk sudah tidak ada.");

        request.Approve(user.Username(), user.FullName());

        // Stok diperiksa LAGI di sini (bisa saja sudah berubah sejak diajukan)
        if (request.IsStockIn)
            product.StockIn(request.Quantity, request.Number);
        else
            product.StockOut(request.Quantity, request.Reason, request.Number);

        // Persetujuan + perubahan stok + kartu stok tersimpan BERSAMAAN (semua atau tidak sama sekali)
        db.SaveChanges();
    }

    // ---------- Menolak ----------
    public void Reject(int requestId, string reason, ClaimsPrincipal user)
    {
        EnsureCanApprove(user);

        using var db = _dbFactory.CreateDbContext();
        var request = db.StockRequests.FirstOrDefault(r => r.Id == requestId)
            ?? throw new InvalidOperationException("Pengajuan tidak ditemukan.");

        request.Reject(user.Username(), user.FullName(), reason);
        db.SaveChanges();
    }

    // Penjagaan lapis kedua: walaupun tombol disembunyikan, service tetap memeriksa hak akses
    private static void EnsureCanApprove(ClaimsPrincipal user)
    {
        if (!user.CanApprove())
            throw new InvalidOperationException("Hanya Manajer atau Admin yang boleh memutuskan pengajuan.");
    }
}