namespace StokFlow.Web.Models;

public enum RequestStatus
{
    Pending,    // menunggu
    Approved,   // disetujui
    Rejected    // ditolak
}

// Satu pengajuan perubahan stok
public class StockRequest
{
    public int Id { get; private set; }
    public string ProductSku { get; private set; } = "";
    public string ProductName { get; private set; } = "";
    public MovementReason Reason { get; private set; }
    public int Quantity { get; private set; }
    public string Note { get; private set; } = "";
    public RequestStatus Status { get; private set; } = RequestStatus.Pending;

    // Siapa & kapan mengajukan
    public string RequestedBy { get; private set; } = "";
    public string RequestedByName { get; private set; } = "";
    public DateTime RequestedAt { get; private set; }

    // Siapa & kapan memutuskan
    public string? DecidedBy { get; private set; }
    public string? DecidedByName { get; private set; }
    public DateTime? DecidedAt { get; private set; }
    public string? RejectReason { get; private set; }

    // Hasil hitungan (tidak disimpan)
    public string Number => $"REQ-{Id:D4}";
    public bool IsStockIn => Reason == MovementReason.Pembelian;
    public string ReasonText => Reason switch
    {
        MovementReason.Pembelian => "Barang masuk",
        MovementReason.Terjual   => "Terjual",
        MovementReason.LayuRusak => "Layu / rusak",
        _ => Reason.ToString()
    };

    private StockRequest() { }   // untuk database

    public StockRequest(Product product, MovementReason reason, int quantity, string note,
                        string requestedBy, string requestedByName)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("Jumlah harus lebih dari 0.");
        if (reason == MovementReason.StokAwal)
            throw new InvalidOperationException("Jenis perubahan tidak valid.");
        if (reason != MovementReason.Pembelian && quantity > product.Stock)
            throw new InvalidOperationException(
                $"Stok {product.Name} hanya {product.Stock}, tidak cukup untuk dikeluarkan {quantity}.");

        ProductSku = product.Sku;
        ProductName = product.Name;
        Reason = reason;
        Quantity = quantity;
        Note = note.Trim();
        RequestedBy = requestedBy;
        RequestedByName = requestedByName;
        RequestedAt = DateTime.Now;
    }

    public void Approve(string username, string fullName)
    {
        EnsureCanDecide(username);
        Status = RequestStatus.Approved;
        DecidedBy = username;
        DecidedByName = fullName;
        DecidedAt = DateTime.Now;
    }

    public void Reject(string username, string fullName, string reason)
    {
        EnsureCanDecide(username);
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Alasan penolakan wajib diisi.");

        Status = RequestStatus.Rejected;
        DecidedBy = username;
        DecidedByName = fullName;
        DecidedAt = DateTime.Now;
        RejectReason = reason.Trim();
    }

    // Aturan: hanya yang masih menunggu, dan bukan milik sendiri
    private void EnsureCanDecide(string username)
    {
        if (Status != RequestStatus.Pending)
            throw new InvalidOperationException($"Pengajuan {Number} sudah diproses sebelumnya.");
        if (username == RequestedBy)
            throw new InvalidOperationException("Anda tidak boleh memutuskan pengajuan Anda sendiri.");
    }
}