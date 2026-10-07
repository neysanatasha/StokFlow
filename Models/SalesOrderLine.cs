namespace StokFlow.Web.Models;

// Satu baris barang di dalam Sales Order
public class SalesOrderLine
{
    public int Id { get; private set; }
    public string ProductSku { get; private set; } = "";
    public string ProductName { get; private set; } = "";
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }        // harga jual saat pesanan dibuat
    public decimal Subtotal => Quantity * UnitPrice;

    private SalesOrderLine() { }   // untuk database

    public SalesOrderLine(Product product, int quantity)
    {
        if (quantity <= 0)
            throw new InvalidOperationException($"Jumlah {product.Name} harus lebih dari 0.");
        if (quantity > product.Stock)
            throw new InvalidOperationException($"Stok {product.Name} hanya {product.Stock}, tidak cukup untuk {quantity}.");

        ProductSku = product.Sku;
        ProductName = product.Name;
        Quantity = quantity;
        UnitPrice = product.Price;   // snapshot harga saat ini
    }
}