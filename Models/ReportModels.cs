namespace StokFlow.Web.Models;

// ================= HASIL LAPORAN LENGKAP =================
public class ReportResult
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    public int ShippedOrderCount { get; set; }
    public decimal TotalSales { get; set; }        // penjualan
    public decimal TotalCogs { get; set; }         // modal (HPP)
    public decimal GrossProfit { get; set; }       // laba kotor
    public decimal TotalPurchases { get; set; }    // pembelian
    public decimal TotalWaste { get; set; }        // kerugian layu/rusak
    public bool HasMissingCost { get; set; }       // ada produk tanpa data harga beli?

    public List<ProductSalesRow> ProductSales { get; set; } = new();
    public List<PartyRow> CustomerSales { get; set; } = new();
    public List<PartyRow> SupplierPurchases { get; set; } = new();
    public List<WasteRow> Waste { get; set; } = new();
    public List<StockValueRow> StockValues { get; set; } = new();
}

// ================= SATU BARIS: PENJUALAN PER PRODUK =================
public class ProductSalesRow
{
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }      // penjualan
    public decimal? Cogs { get; set; }        // modal; kosong (null) kalau harga beli tidak diketahui

    public decimal? Profit => Cogs.HasValue ? Revenue - Cogs.Value : null;
    public decimal? MarginPercent => Profit.HasValue && Revenue > 0 ? Profit.Value / Revenue * 100 : null;
}

// ================= SATU BARIS: PER PELANGGAN / PER PEMASOK =================
public class PartyRow
{
    public string Name { get; set; } = "";
    public int DocumentCount { get; set; }    // jumlah SO / PO
    public decimal Total { get; set; }
}

// ================= SATU BARIS: KERUGIAN LAYU / RUSAK =================
public class WasteRow
{
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitValue { get; set; }          // nilai per unit
    public bool UsesSellingPrice { get; set; }      // true = memakai harga jual karena harga beli tidak ada
    public decimal Value => Quantity * UnitValue;
}

// ================= SATU BARIS: NILAI STOK SAAT INI =================
public class StockValueRow
{
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public int Stock { get; set; }
    public decimal? AverageCost { get; set; }       // harga beli rata-rata
    public decimal SellingPrice { get; set; }       // harga jual

    public decimal? ValueAtCost => AverageCost.HasValue ? Stock * AverageCost.Value : null;
    public decimal ValueAtPrice => Stock * SellingPrice;
}