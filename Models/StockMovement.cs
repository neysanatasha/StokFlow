namespace StokFlow.Web.Models;

public enum MovementReason
{
    StokAwal,
    Pembelian,
    Terjual,
    LayuRusak
}

public class StockMovement
{
    public int Id { get; private set; }
    public DateTime Date { get; private set; }
    public MovementReason Reason { get; private set; }
    public int Quantity { get; private set; }          // positif = masuk, negatif = keluar
    public int StockAfter { get; private set; }
    public string Reference { get; private set; } = "";   // nomor dokumen asal, contoh: PO-0001

    private StockMovement() { }

    public StockMovement(DateTime date, MovementReason reason, int quantity, int stockAfter, string reference = "")
    {
        Date = date;
        Reason = reason;
        Quantity = quantity;
        StockAfter = stockAfter;
        Reference = reference;
    }

    public string ReasonText => Reason switch
    {
        MovementReason.StokAwal  => "Stok awal",
        MovementReason.Pembelian => "Barang masuk (pembelian)",
        MovementReason.Terjual   => "Terjual",
        MovementReason.LayuRusak => "Layu / rusak",
        _ => Reason.ToString()
    };

    // Keterangan lengkap untuk kartu stok
    public string Description => Reference == "" ? ReasonText : $"{ReasonText} — {Reference}";
}