namespace StokFlow.Web.Models;

// Isian satu baris PO di layar (sebelum PO disimpan)
public class PoLineInput
{
    public string Sku { get; set; } = "";
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal Subtotal => Quantity * UnitCost;
}