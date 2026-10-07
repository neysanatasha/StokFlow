namespace StokFlow.Web.Models;

public class Product
{
    private readonly List<StockMovement> _movements = new();

    public string Sku { get; private set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Stock { get; private set; }
    public int MinimumStock { get; set; }

    public IReadOnlyList<StockMovement> Movements => _movements;

    public bool IsLowStock => Stock <= MinimumStock;
    public bool IsOutOfStock => Stock == 0;
    public int TotalSold => _movements.Where(m => m.Reason == MovementReason.Terjual).Sum(m => -m.Quantity);
    public int TotalWasted => _movements.Where(m => m.Reason == MovementReason.LayuRusak).Sum(m => -m.Quantity);

    private Product() { }

    public Product(string sku, string name, decimal price, int stock, int minimumStock)
    {
        Sku = sku;
        Name = name;
        Price = price;
        MinimumStock = minimumStock;
        Stock = stock;
        _movements.Add(new StockMovement(DateTime.Now, MovementReason.StokAwal, stock, Stock));
    }

    public void StockIn(int qty, string reference = "")
    {
        if (qty <= 0)
            throw new InvalidOperationException("Jumlah barang masuk harus lebih dari 0.");

        Stock += qty;
        _movements.Add(new StockMovement(DateTime.Now, MovementReason.Pembelian, qty, Stock, reference));
    }

    public void StockOut(int qty, MovementReason reason, string reference = "")
    {
        if (qty <= 0)
            throw new InvalidOperationException("Jumlah barang keluar harus lebih dari 0.");
        if (qty > Stock)
            throw new InvalidOperationException($"Stok {Name} tidak cukup. Sisa stok: {Stock}.");
        if (reason != MovementReason.Terjual && reason != MovementReason.LayuRusak)
            throw new InvalidOperationException("Alasan barang keluar harus 'Terjual' atau 'Layu / rusak'.");

        Stock -= qty;
        _movements.Add(new StockMovement(DateTime.Now, reason, -qty, Stock, reference));
    }
}