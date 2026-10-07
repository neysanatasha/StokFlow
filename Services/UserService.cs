using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StokFlow.Web.Auth;
using StokFlow.Web.Data;
using StokFlow.Web.Models;

namespace StokFlow.Web.Services;

public class UserService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly PasswordHasher<AppUser> _hasher = new();

    public UserService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    // ================= LOGIN =================
    public AppUser? ValidateLogin(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return null;

        var name = username.Trim().ToLower();

        using var db = _dbFactory.CreateDbContext();
        var user = db.Users.AsNoTracking().FirstOrDefault(u => u.Username == name && u.IsActive);
        if (user == null)
            return null;

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Failed ? null : user;
    }

    public static AppUser CreateUser(string username, string fullName, string password, UserRole role)
    {
        var user = new AppUser
        {
            Username = username.Trim().ToLower(),
            FullName = fullName,
            Role = role
        };
        user.PasswordHash = new PasswordHasher<AppUser>().HashPassword(user, password);
        return user;
    }

    // ================= PROFIL SENDIRI =================
    public void ChangeOwnPassword(ClaimsPrincipal currentUser, string oldPassword, string newPassword, string confirmPassword)
    {
        var myName = currentUser.Username();

        using var db = _dbFactory.CreateDbContext();
        var user = db.Users.FirstOrDefault(u => u.Username == myName)
            ?? throw new InvalidOperationException("Pengguna tidak ditemukan.");

        if (_hasher.VerifyHashedPassword(user, user.PasswordHash, oldPassword) == PasswordVerificationResult.Failed)
            throw new InvalidOperationException("Kata sandi lama salah.");
        if (newPassword != confirmPassword)
            throw new InvalidOperationException("Konfirmasi kata sandi baru tidak sama.");
        if (newPassword == oldPassword)
            throw new InvalidOperationException("Kata sandi baru harus berbeda dari yang lama.");
        ValidatePassword(newPassword);

        user.PasswordHash = _hasher.HashPassword(user, newPassword);
        db.SaveChanges();
    }

    // ================= KELOLA PENGGUNA (khusus Admin) =================
    public List<AppUser> GetAll(ClaimsPrincipal currentUser)
    {
        EnsureAdmin(currentUser);
        using var db = _dbFactory.CreateDbContext();
        return db.Users.AsNoTracking().OrderBy(u => u.Username).ToList();
    }

    public AppUser AddUser(string username, string fullName, string password, UserRole role, ClaimsPrincipal currentUser)
    {
        EnsureAdmin(currentUser);

        var name = username.Trim().ToLower();
        if (!Regex.IsMatch(name, "^[a-z0-9]{3,20}$"))
            throw new InvalidOperationException("Nama pengguna harus 3–20 karakter, hanya huruf kecil dan angka, tanpa spasi.");
        if (string.IsNullOrWhiteSpace(fullName))
            throw new InvalidOperationException("Nama lengkap wajib diisi.");
        ValidatePassword(password);

        using var db = _dbFactory.CreateDbContext();
        if (db.Users.Any(u => u.Username == name))
            throw new InvalidOperationException($"Nama pengguna {name} sudah dipakai.");

        var user = CreateUser(name, fullName.Trim(), password, role);
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    public void ResetPassword(int userId, string newPassword, ClaimsPrincipal currentUser)
    {
        EnsureAdmin(currentUser);
        ValidatePassword(newPassword);

        using var db = _dbFactory.CreateDbContext();
        var user = LoadUser(db, userId);
        user.PasswordHash = _hasher.HashPassword(user, newPassword);
        db.SaveChanges();
    }

    public void ChangeRole(int userId, UserRole newRole, ClaimsPrincipal currentUser)
    {
        EnsureAdmin(currentUser);

        using var db = _dbFactory.CreateDbContext();
        var user = LoadUser(db, userId);

        if (user.Username == currentUser.Username())
            throw new InvalidOperationException("Anda tidak bisa mengubah peran akun Anda sendiri.");
        if (user.Role == UserRole.Admin && newRole != UserRole.Admin)
            EnsureNotLastActiveAdmin(db, user);

        user.Role = newRole;
        db.SaveChanges();
    }

    public void SetActive(int userId, bool active, ClaimsPrincipal currentUser)
    {
        EnsureAdmin(currentUser);

        using var db = _dbFactory.CreateDbContext();
        var user = LoadUser(db, userId);

        if (user.Username == currentUser.Username())
            throw new InvalidOperationException("Anda tidak bisa menonaktifkan akun Anda sendiri.");
        if (!active && user.Role == UserRole.Admin)
            EnsureNotLastActiveAdmin(db, user);

        user.IsActive = active;
        db.SaveChanges();
    }

    // ================= PEMBANTU =================
    private static AppUser LoadUser(AppDbContext db, int userId) =>
        db.Users.FirstOrDefault(u => u.Id == userId)
        ?? throw new InvalidOperationException("Pengguna tidak ditemukan.");

    private static void EnsureAdmin(ClaimsPrincipal user)
    {
        if (!user.IsInRole("Admin"))
            throw new InvalidOperationException("Hanya Admin yang boleh mengelola pengguna.");
    }

    // Pagar pengaman: sistem tidak boleh kehilangan semua Admin aktif
    private static void EnsureNotLastActiveAdmin(AppDbContext db, AppUser user)
    {
        var otherActiveAdmins = db.Users.Count(u => u.Role == UserRole.Admin && u.IsActive && u.Id != user.Id);
        if (otherActiveAdmins == 0)
            throw new InvalidOperationException("Harus ada minimal satu Admin aktif.");
    }

    // Aturan kata sandi
    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 6)
            throw new InvalidOperationException("Kata sandi minimal 6 karakter.");
    }
}