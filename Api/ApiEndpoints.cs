using StokFlow.Web.Models;
using StokFlow.Web.Services;

namespace StokFlow.Web.Api;

// Semua "pintu" API dikumpulkan di sini
public static class ApiEndpoints
{
    public static void MapStokFlowApi(this WebApplication app)
    {
        // Semua alamat di bawah /api wajib membawa kunci API yang benar
        var api = app.MapGroup("/api").AddEndpointFilter(async (context, next) =>
        {
            var http = context.HttpContext;
            var config = http.RequestServices.GetRequiredService<IConfiguration>();
            var expectedKey = config["Integration:ApiKey"];

            // Kunci boleh dikirim lewat header "X-API-Key" (cara utama) atau ?apiKey=... (untuk dicoba di browser)
            var givenKey = http.Request.Headers["X-API-Key"].FirstOrDefault()
                           ?? http.Request.Query["apiKey"].FirstOrDefault();

            if (string.IsNullOrEmpty(expectedKey) || givenKey != expectedKey)
                return Results.Json(new { error = "API key tidak valid atau tidak ada." }, statusCode: 401);

            return await next(context);
        });

        // ---------- Semua produk ----------
        api.MapGet("/products", (InventoryService inventory) =>
            inventory.GetAll().Select(ToProductData));

        // ---------- Satu produk ----------
        api.MapGet("/products/{sku}", (string sku, InventoryService inventory) =>
        {
            var p = FindProduct(inventory, sku);
            return p is null
                ? Results.NotFound(new { error = $"Produk {sku} tidak ditemukan." })
                : Results.Ok(ToProductData(p));
        });

        // ---------- Kartu stok satu produk ----------
        api.MapGet("/products/{sku}/movements", (string sku, InventoryService inventory) =>
        {
            var p = FindProduct(inventory, sku);
            if (p is null)
                return Results.NotFound(new { error = $"Produk {sku} tidak ditemukan." });

            var movements = p.Movements
                .OrderBy(m => m.Id)
                .Select(m => new
                {
                    date = m.Date,
                    description = m.Description,
                    quantity = m.Quantity,
                    stockAfter = m.StockAfter,
                    reference = m.Reference
                });

            return Results.Ok(new { sku = p.Sku, name = p.Name, movements });
        });

        // ---------- Produk yang perlu dibeli lagi ----------
        api.MapGet("/low-stock", (InventoryService inventory) =>
            inventory.GetAll()
                .Where(p => p.IsLowStock)
                .OrderBy(p => p.Stock)
                .Select(p => new
                {
                    sku = p.Sku,
                    name = p.Name,
                    stock = p.Stock,
                    minimumStock = p.MinimumStock,
                    shortage = p.MinimumStock - p.Stock
                }));
    }

    // ---------- Pembantu ----------
    private static Product? FindProduct(InventoryService inventory, string sku) =>
        inventory.GetAll().FirstOrDefault(p => p.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase));

    // Bentuk data produk yang dikirim keluar (tidak semua isi Product perlu dibagikan)
    private static object ToProductData(Product p) => new
    {
        sku = p.Sku,
        name = p.Name,
        price = p.Price,
        stock = p.Stock,
        minimumStock = p.MinimumStock,
        status = p.IsOutOfStock ? "Habis" : p.IsLowStock ? "Hampir habis" : "Aman"
    };
}