using Microsoft.EntityFrameworkCore;
using StokFlow.Web.Data;
using StokFlow.Web.Models;

namespace StokFlow.Web.Services;

public class ReportService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public ReportService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public ReportResult Build(DateTime from, DateTime to)
    {
        if (to.Date < from.Date)
            throw new InvalidOperationException("Tanggal akhir tidak boleh sebelum tanggal awal.");

        var start = from.Date;
        var end = to.Date.AddDays(1);   // sampai akhir hari terakhir

        using var db = _dbFactory.CreateDbContext();

        // ---------- Ambil data mentah ----------
        var products = db.Products.Include(p => p.Movements).AsNoTracking().ToList();
        var shippedSos = db.SalesOrders.Include(s => s.Lines).AsNoTracking()
                           .Where(s => s.Status == SoStatus.Shipped).ToList();
        var receivedPos = db.PurchaseOrders.Include(p => p.Lines).AsNoTracking()
                            .Where(p => p.Status == PoStatus.Received).ToList();

        // ---------- Harga beli rata-rata per produk (dari SEMUA PO yang sudah diterima) ----------
        var avgCost = receivedPos
            .SelectMany(p => p.Lines)
            .GroupBy(l => l.ProductSku)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.Subtotal) / g.Sum(l => l.Quantity));

        // ---------- Saring berdasarkan periode ----------
        var sos = shippedSos.Where(s => s.ShippedAt >= start && s.ShippedAt < end).ToList();
        var pos = receivedPos.Where(p => p.ReceivedAt >= start && p.ReceivedAt < end).ToList();

        // ---------- 1. Penjualan per produk ----------
        var productSales = sos
            .SelectMany(s => s.Lines)
            .GroupBy(l => l.ProductSku)
            .Select(g =>
            {
                var qty = g.Sum(l => l.Quantity);
                decimal? cogs = null;
                if (avgCost.TryGetValue(g.Key, out var cost))
                    cogs = cost * qty;

                return new ProductSalesRow
                {
                    Sku = g.Key,
                    Name = g.First().ProductName,
                    Quantity = qty,
                    Revenue = g.Sum(l => l.Subtotal),
                    Cogs = cogs
                };
            })
            .OrderByDescending(r => r.Revenue)
            .ToList();

        // ---------- 2a. Penjualan per pelanggan ----------
        var customerSales = sos
            .GroupBy(s => s.CustomerName)
            .Select(g => new PartyRow { Name = g.Key, DocumentCount = g.Count(), Total = g.Sum(s => s.Total) })
            .OrderByDescending(r => r.Total)
            .ToList();

        // ---------- 2b. Pembelian per pemasok ----------
        var supplierPurchases = pos
            .GroupBy(p => p.SupplierName)
            .Select(g => new PartyRow { Name = g.Key, DocumentCount = g.Count(), Total = g.Sum(p => p.Total) })
            .OrderByDescending(r => r.Total)
            .ToList();

        // ---------- 3. Kerugian layu / rusak ----------
        var waste = products
            .Select(p =>
            {
                var qty = p.Movements
                    .Where(m => m.Reason == MovementReason.LayuRusak && m.Date >= start && m.Date < end)
                    .Sum(m => -m.Quantity);

                var hasCost = avgCost.TryGetValue(p.Sku, out var cost);
                return new WasteRow
                {
                    Sku = p.Sku,
                    Name = p.Name,
                    Quantity = qty,
                    UnitValue = hasCost ? cost : p.Price,
                    UsesSellingPrice = !hasCost
                };
            })
            .Where(r => r.Quantity > 0)
            .OrderByDescending(r => r.Value)
            .ToList();

        // ---------- 4. Nilai stok saat ini ----------
        var stockValues = products
            .OrderBy(p => p.Sku)
            .Select(p =>
            {
                decimal? cost = null;
                if (avgCost.TryGetValue(p.Sku, out var c))
                    cost = c;

                return new StockValueRow
                {
                    Sku = p.Sku,
                    Name = p.Name,
                    Stock = p.Stock,
                    AverageCost = cost,
                    SellingPrice = p.Price
                };
            })
            .ToList();

        // ---------- Ringkasan ----------
        return new ReportResult
        {
            From = start,
            To = to.Date,
            ShippedOrderCount = sos.Count,
            TotalSales = productSales.Sum(r => r.Revenue),
            TotalCogs = productSales.Sum(r => r.Cogs ?? 0),
            GrossProfit = productSales.Sum(r => r.Profit ?? 0),
            HasMissingCost = productSales.Any(r => r.Cogs == null),
            TotalPurchases = pos.Sum(p => p.Total),
            TotalWaste = waste.Sum(w => w.Value),
            ProductSales = productSales,
            CustomerSales = customerSales,
            SupplierPurchases = supplierPurchases,
            Waste = waste,
            StockValues = stockValues
        };
    }
}