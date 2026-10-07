namespace StokFlow.Web.Models;

// Peran pengguna: menentukan hak akses
public enum UserRole
{
    Admin,
    Manajer,
    StafGudang
}

public class AppUser
{
    public int Id { get; set; }
    public string Username { get; set; } = "";       // nama untuk login
    public string FullName { get; set; } = "";       // nama lengkap untuk ditampilkan
    public string PasswordHash { get; set; } = "";   // kata sandi yang sudah DIACAK
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;       // akun aktif atau dinonaktifkan
}