namespace StokFlow.Web.Models;

public class ProductForm
{
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int MinimumStock { get; set; }
}