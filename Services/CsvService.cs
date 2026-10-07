using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using StokFlow.Web.Data;
using StokFlow.Web.Models;

namespace StokFlow.Web.Services;

public class CsvService
{
    private const char Sep = ';';   // pemisah kolom (cocok untuk Excel versi Indonesia)

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly InventoryService _inventory;

    public CsvService(IDbContextFactory<AppDbContext> dbFactory, InventoryService inventory)
    {
        _dbFactory = dbFactory;
        _inventory = inventory;
    }

    // ================= EKSPOR =================
    public string ExportProducts()
    {
        var sb = new StringBuilder();
        Line(sb, "Kode", "Nama", "Harga", "Stok", "BatasMinimum", "Status");
        foreach (var p in _inventory.GetAll())
        {
            var status = p.IsOutOfStock ? "Habis" : p.IsLowStock ? "Hampir habis" : "Aman";
            Line(sb, p.Sku, p.Name, Num(p.Price), p.Stock.ToString(), p.MinimumStock.ToString(), status);
        }
        return sb.ToString();
    }

    public string ExportStockMovements()
    {
        var sb = new StringBuilder();
        Line(sb, "Tanggal", "Kode", "Produk", "Keterangan", "Masuk", "Keluar", "Sisa");

        var rows = _inventory.GetAll()
            .SelectMany(p => p.Movements.Select(m => new { Product = p, Movement = m }))
            .OrderBy(x => x.Movement.Date)
            .ThenBy(x => x.Movement.Id);

        foreach (var x in rows)
        {
            var m = x.Movement;
            Line(sb,
                m.Date.ToString("yyyy-MM-dd HH:mm"),
                x.Product.Sku,
                x.Product.Name,
                m.Description,
                m.Quantity > 0 ? m.Quantity.ToString() : "",
                m.Quantity < 0 ? (-m.Quantity).ToString() : "",
                m.StockAfter.ToString());
        }
        return sb.ToString();
    }

    public string ExportSalesOrders()
    {
        using var db = _dbFactory.CreateDbContext();
        var orders = db.SalesOrders.Include(s => s.Lines).AsNoTracking().OrderBy(s => s.Id).ToList();

        var sb = new StringBuilder();
        Line(sb, "NoSO", "Tanggal", "Pelanggan", "Status", "Kode", "Produk", "Jumlah", "Harga", "Subtotal");
        foreach (var so in orders)
            foreach (var l in so.Lines)
                Line(sb, so.Number, so.CreatedAt.ToString("yyyy-MM-dd"), so.CustomerName, so.StatusText,
                     l.ProductSku, l.ProductName, l.Quantity.ToString(), Num(l.UnitPrice), Num(l.Subtotal));
        return sb.ToString();
    }

    public string ExportPurchaseOrders()
    {
        using var db = _dbFactory.CreateDbContext();
        var orders = db.PurchaseOrders.Include(p => p.Lines).AsNoTracking().OrderBy(p => p.Id).ToList();

        var sb = new StringBuilder();
        Line(sb, "NoPO", "Tanggal", "Pemasok", "Status", "Kode", "Produk", "Jumlah", "HargaBeli", "Subtotal");
        foreach (var po in orders)
            foreach (var l in po.Lines)
                Line(sb, po.Number, po.CreatedAt.ToString("yyyy-MM-dd"), po.SupplierName, po.StatusText,
                     l.ProductSku, l.ProductName, l.Quantity.ToString(), Num(l.UnitCost), Num(l.Subtotal));
        return sb.ToString();
    }

    // Contoh file untuk impor
    public string ProductTemplate()
    {
        var sb = new StringBuilder();
        Line(sb, "Kode", "Nama", "Harga", "StokAwal", "BatasMinimum");
        Line(sb, "FLO_010", "Bunga Tulip Kuning", "45000", "20", "5");
        return sb.ToString();
    }

    // ================= IMPOR =================
    public ImportResult ImportProducts(string content, ClaimsPrincipal user)
    {
        if (!user.IsInRole("Admin") && !user.IsInRole("Manajer"))
            throw new InvalidOperationException("Hanya Admin atau Manajer yang boleh mengimpor produk.");

        var lines = content.Replace("\r\n", "\n").Replace('\r', '\n')
                           .Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
            throw new InvalidOperationException("File kosong.");

        // Kenali pemisah kolom otomatis: titik koma atau koma
        var sep = lines[0].Contains(';') ? ';' : ',';

        var header = lines[0].Split(sep)
                             .Select(h => h.Trim().Trim('"').Trim('\uFEFF').ToLower())
                             .ToArray();
        string[] expected = { "kode", "nama", "harga", "stokawal", "batasminimum" };
        if (header.Length < 5 || !header.Take(5).SequenceEqual(expected))
            throw new InvalidOperationException(
                "Format kolom tidak sesuai. Baris pertama harus: Kode;Nama;Harga;StokAwal;BatasMinimum");

        var result = new ImportResult();

        for (int i = 1; i < lines.Length; i++)
        {
            var rowNo = i + 1;   // nomor baris seperti di Excel
            var cols = lines[i].Split(sep).Select(c => c.Trim().Trim('"')).ToArray();

            if (cols.All(string.IsNullOrWhiteSpace))
                continue;
            if (cols.Length < 5)
            {
                result.Errors.Add($"Baris {rowNo}: jumlah kolom kurang.");
                continue;
            }
            if (!TryMoney(cols[2], out var price))
            {
                result.Errors.Add($"Baris {rowNo}: harga '{cols[2]}' bukan angka.");
                continue;
            }
            if (!int.TryParse(cols[3], out var stock))
            {
                result.Errors.Add($"Baris {rowNo}: stok awal '{cols[3]}' bukan angka bulat.");
                continue;
            }
            if (!int.TryParse(cols[4], out var minimum))
            {
                result.Errors.Add($"Baris {rowNo}: batas minimum '{cols[4]}' bukan angka bulat.");
                continue;
            }

            try
            {
                // Lewat aturan yang SAMA dengan form tambah produk
                _inventory.AddProduct(new ProductForm
                {
                    Sku = cols[0],
                    Name = cols[1],
                    Price = price,
                    Stock = stock,
                    MinimumStock = minimum
                });
                result.Added++;
            }
            catch (InvalidOperationException ex)
            {
                result.Errors.Add($"Baris {rowNo} ({cols[0]}): {ex.Message}");
            }
        }

        return result;
    }

    // ================= PEMBANTU =================

    // Ubah teks CSV menjadi isi file (dengan penanda BOM supaya Excel membaca hurufnya dengan benar)
    public static byte[] ToFileBytes(string csv) =>
        Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();

    // Tulis satu baris CSV
    private static void Line(StringBuilder sb, params string[] values)
    {
        sb.Append(string.Join(Sep, values.Select(Escape)));
        sb.Append("\r\n");
    }

    // Kalau isi kolom mengandung pemisah atau tanda kutip, bungkus dengan tanda kutip
    private static string Escape(string value)
    {
        if (value.Contains(Sep) || value.Contains('"') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }

    // Angka tanpa pemisah ribuan, contoh: 28999
    private static string Num(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    // Baca harga rupiah: "45000", "45.000", "Rp 45.000" → 45000
    private static bool TryMoney(string text, out decimal value)
    {
        var digits = new string(text.Where(char.IsDigit).ToArray());
        value = 0;
        return digits.Length > 0 &&
               decimal.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out value);
    }
}