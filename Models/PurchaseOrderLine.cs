namespace StokFlow.Web.Models;

// Satu baris barang di dalam PO
public class PurchaseOrderLine
{
    public int Id { get; private set; }
    public string ProductSku { get; private set; } = "";
    public string ProductName { get; private set; } = "";
    public int Quantity { get; private set; }
    public decimal UnitCost { get; private set; }          // harga beli per unit
    public decimal Subtotal => Quantity * UnitCost;

    private PurchaseOrderLine() { }   // untuk database

    public PurchaseOrderLine(Product product, int quantity, decimal unitCost)
    {
        if (quantity <= 0)
            throw new InvalidOperationException($"Jumlah {product.Name} harus lebih dari 0.");
        if (unitCost <= 0)
            throw new InvalidOperationException($"Harga beli {product.Name} harus lebih dari 0.");

        ProductSku = product.Sku;
        ProductName = product.Name;
        Quantity = quantity;
        UnitCost = unitCost;
    }
}