namespace StokFlow.Web.Models;

// Hasil impor: berapa yang berhasil, dan daftar baris yang gagal
public class ImportResult
{
    public int Added { get; set; }
    public List<string> Errors { get; set; } = new();
}