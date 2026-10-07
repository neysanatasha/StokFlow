namespace StokFlow.Web.Models;

public enum SoStatus
{
    PendingApproval,   // menunggu persetujuan
    Approved,          // disetujui, siap dikirim
    Rejected,          // ditolak
    Shipped            // sudah dikirim
}

// Dokumen Sales Order: satu "kepala" + beberapa baris barang
public class SalesOrder
{
    // Batas nilai: di atas ini wajib disetujui Manajer
    public const decimal ApprovalLimit = 1_000_000m;

    private readonly List<SalesOrderLine> _lines = new();

    public int Id { get; private set; }
    public int CustomerId { get; private set; }
    public string CustomerName { get; private set; } = "";
    public string Note { get; private set; } = "";
    public SoStatus Status { get; private set; }

    public string CreatedBy { get; private set; } = "";
    public string CreatedByName { get; private set; } = "";
    public DateTime CreatedAt { get; private set; }

    public string? DecidedBy { get; private set; }
    public string? DecidedByName { get; private set; }
    public DateTime? DecidedAt { get; private set; }
    public string? RejectReason { get; private set; }

    public string? ShippedBy { get; private set; }
    public string? ShippedByName { get; private set; }
    public DateTime? ShippedAt { get; private set; }

    public IReadOnlyList<SalesOrderLine> Lines => _lines;

    // Hasil hitungan (tidak disimpan)
    public decimal Total => _lines.Sum(l => l.Subtotal);
    public int TotalQuantity => _lines.Sum(l => l.Quantity);
    public string Number => $"SO-{Id:D4}";
    public bool IsAutoApproved => DecidedBy == "sistem";
    public string StatusText => Status switch
    {
        SoStatus.PendingApproval => "Menunggu Persetujuan",
        SoStatus.Approved        => "Siap Dikirim",
        SoStatus.Rejected        => "Ditolak",
        SoStatus.Shipped         => "Dikirim",
        _ => Status.ToString()
    };

    private SalesOrder() { }   // untuk database

    public SalesOrder(Customer customer, List<SalesOrderLine> lines, string note,
                      string createdBy, string createdByName)
    {
        if (lines.Count == 0)
            throw new InvalidOperationException("SO harus berisi minimal 1 produk.");
        if (lines.GroupBy(l => l.ProductSku).Any(g => g.Count() > 1))
            throw new InvalidOperationException("Produk yang sama tidak boleh muncul dua kali dalam satu SO.");

        CustomerId = customer.Id;
        CustomerName = customer.Name;
        Note = note.Trim();
        CreatedBy = createdBy;
        CreatedByName = createdByName;
        CreatedAt = DateTime.Now;
        _lines.AddRange(lines);

        // Aturan batas persetujuan
        if (Total <= ApprovalLimit)
        {
            Status = SoStatus.Approved;
            DecidedBy = "sistem";
            DecidedByName = "Otomatis (di bawah batas)";
            DecidedAt = DateTime.Now;
        }
        else
        {
            Status = SoStatus.PendingApproval;
        }
    }

    public void Approve(string username, string fullName)
    {
        EnsurePendingAndNotOwn(username);
        Status = SoStatus.Approved;
        DecidedBy = username;
        DecidedByName = fullName;
        DecidedAt = DateTime.Now;
    }

    public void Reject(string username, string fullName, string reason)
    {
        EnsurePendingAndNotOwn(username);
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Alasan penolakan wajib diisi.");

        Status = SoStatus.Rejected;
        DecidedBy = username;
        DecidedByName = fullName;
        DecidedAt = DateTime.Now;
        RejectReason = reason.Trim();
    }

    public void MarkShipped(string username, string fullName)
    {
        if (Status != SoStatus.Approved)
            throw new InvalidOperationException($"{Number} belum disetujui atau sudah dikirim.");

        Status = SoStatus.Shipped;
        ShippedBy = username;
        ShippedByName = fullName;
        ShippedAt = DateTime.Now;
    }

    private void EnsurePendingAndNotOwn(string username)
    {
        if (Status != SoStatus.PendingApproval)
            throw new InvalidOperationException($"{Number} sudah diproses sebelumnya.");
        if (username == CreatedBy)
            throw new InvalidOperationException("Anda tidak boleh memutuskan SO buatan Anda sendiri.");
    }
}