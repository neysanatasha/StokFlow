namespace StokFlow.Web.Models;

public enum PoStatus
{
    PendingApproval,   // menunggu persetujuan
    Approved,          // disetujui, menunggu barang datang
    Rejected,          // ditolak
    Received           // barang sudah diterima
}

// Dokumen Purchase Order: satu "kepala" + beberapa baris barang
public class PurchaseOrder
{
    private readonly List<PurchaseOrderLine> _lines = new();

    public int Id { get; private set; }
    public int SupplierId { get; private set; }
    public string SupplierName { get; private set; } = "";
    public string Note { get; private set; } = "";
    public PoStatus Status { get; private set; } = PoStatus.PendingApproval;

    public string CreatedBy { get; private set; } = "";
    public string CreatedByName { get; private set; } = "";
    public DateTime CreatedAt { get; private set; }

    public string? DecidedBy { get; private set; }
    public string? DecidedByName { get; private set; }
    public DateTime? DecidedAt { get; private set; }
    public string? RejectReason { get; private set; }

    public string? ReceivedBy { get; private set; }
    public string? ReceivedByName { get; private set; }
    public DateTime? ReceivedAt { get; private set; }

    public IReadOnlyList<PurchaseOrderLine> Lines => _lines;

    // Hasil hitungan (tidak disimpan)
    public decimal Total => _lines.Sum(l => l.Subtotal);
    public int TotalQuantity => _lines.Sum(l => l.Quantity);
    public string Number => $"PO-{Id:D4}";
    public string StatusText => Status switch
    {
        PoStatus.PendingApproval => "Menunggu Persetujuan",
        PoStatus.Approved        => "Disetujui",
        PoStatus.Rejected        => "Ditolak",
        PoStatus.Received        => "Diterima",
        _ => Status.ToString()
    };

    private PurchaseOrder() { }   // untuk database

    public PurchaseOrder(Supplier supplier, List<PurchaseOrderLine> lines, string note,
                         string createdBy, string createdByName)
    {
        if (lines.Count == 0)
            throw new InvalidOperationException("PO harus berisi minimal 1 produk.");
        if (lines.GroupBy(l => l.ProductSku).Any(g => g.Count() > 1))
            throw new InvalidOperationException("Produk yang sama tidak boleh muncul dua kali dalam satu PO.");

        SupplierId = supplier.Id;
        SupplierName = supplier.Name;
        Note = note.Trim();
        CreatedBy = createdBy;
        CreatedByName = createdByName;
        CreatedAt = DateTime.Now;
        _lines.AddRange(lines);
    }

    public void Approve(string username, string fullName)
    {
        EnsurePendingAndNotOwn(username);
        Status = PoStatus.Approved;
        DecidedBy = username;
        DecidedByName = fullName;
        DecidedAt = DateTime.Now;
    }

    public void Reject(string username, string fullName, string reason)
    {
        EnsurePendingAndNotOwn(username);
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Alasan penolakan wajib diisi.");

        Status = PoStatus.Rejected;
        DecidedBy = username;
        DecidedByName = fullName;
        DecidedAt = DateTime.Now;
        RejectReason = reason.Trim();
    }

    public void MarkReceived(string username, string fullName)
    {
        if (Status != PoStatus.Approved)
            throw new InvalidOperationException($"{Number} belum disetujui atau barangnya sudah diterima.");

        Status = PoStatus.Received;
        ReceivedBy = username;
        ReceivedByName = fullName;
        ReceivedAt = DateTime.Now;
    }

    private void EnsurePendingAndNotOwn(string username)
    {
        if (Status != PoStatus.PendingApproval)
            throw new InvalidOperationException($"{Number} sudah diproses sebelumnya.");
        if (username == CreatedBy)
            throw new InvalidOperationException("Anda tidak boleh memutuskan PO buatan Anda sendiri.");
    }
}