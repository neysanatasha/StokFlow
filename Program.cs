using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using StokFlow.Web.Api;          // ← BARU
using StokFlow.Web.Auth;
using StokFlow.Web.Data;
using StokFlow.Web.Models;
using StokFlow.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ================= DAFTAR LAYANAN =================
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Database SQLite
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite("Data Source=stokflow.db"));

// Layanan aplikasi
builder.Services.AddSingleton<InventoryService>();
builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<ApprovalService>();
builder.Services.AddSingleton<PurchaseOrderService>();
builder.Services.AddSingleton<SalesOrderService>();
builder.Services.AddSingleton<ReportService>();
builder.Services.AddSingleton<CsvService>();

// Login & hak akses
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());

// Jawaban API ditulis rapi (bertingkat) supaya mudah dibaca manusia   ← BARU
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.WriteIndented = true);

var app = builder.Build();

// ================= SIAPKAN DATABASE =================
using (var db = app.Services.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext())
{
    db.Database.EnsureCreated();

    if (!db.Products.Any())
    {
        db.Products.AddRange(
            new Product("FLO_001", "Bunga Lily Putih",  28999, 34, 10),
            new Product("FLO_002", "Bunga Mawar Merah", 50000, 76, 35),
            new Product("FLO_003", "Bunga Matahari",    35000, 1,  20),
            new Product("FLO_004", "Bunga Mawar Merah", 98980, 5,  30));
        db.SaveChanges();
    }

    if (!db.Users.Any())
    {
        db.Users.AddRange(
            UserService.CreateUser("admin",   "Administrator", "admin123",   UserRole.Admin),
            UserService.CreateUser("manajer", "Manajer Toko",  "manajer123", UserRole.Manajer),
            UserService.CreateUser("staf",    "Staf Gudang",   "staf123",    UserRole.StafGudang));
        db.SaveChanges();
    }

    if (!db.Suppliers.Any())
    {
        db.Suppliers.AddRange(
            new Supplier { Name = "Kebun Bunga Sejahtera", Phone = "0812-0000-1111", Address = "Jl. Kebun Raya No. 1" },
            new Supplier { Name = "CV Florist Nusantara",  Phone = "0813-0000-2222", Address = "Jl. Melati No. 12" });
        db.SaveChanges();
    }

    if (!db.Customers.Any())
    {
        db.Customers.AddRange(
            new Customer { Name = "Hotel Mawar Indah", Phone = "0811-0000-3333", Address = "Jl. Anggrek No. 5" },
            new Customer { Name = "Toko Kado Ceria",   Phone = "0814-0000-4444", Address = "Jl. Kenanga No. 8" },
            new Customer { Name = "Pelanggan Umum",    Phone = "-",              Address = "-" });
        db.SaveChanges();
    }
}

// ================= PENGATURAN HALAMAN =================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapStokFlowApi();            // ← BARU: pintu-pintu API
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// ================= JALANKAN (harus paling bawah) =================
app.Run();