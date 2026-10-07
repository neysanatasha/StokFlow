namespace StokFlow.Web.Models;

// Isian satu baris SO di layar (sebelum SO disimpan)
public class SoLineInput
{
    public string Sku { get; set; } = "";
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal => Quantity * UnitPrice;
}