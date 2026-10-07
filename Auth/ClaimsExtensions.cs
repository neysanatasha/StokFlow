using System.Security.Claims;

namespace StokFlow.Web.Auth;

// Cara singkat membaca data pengguna yang sedang login
public static class ClaimsExtensions
{
    public static string Username(this ClaimsPrincipal user) => user.Identity?.Name ?? "";
    public static string FullName(this ClaimsPrincipal user) => user.FindFirst("FullName")?.Value ?? "";
    public static bool CanApprove(this ClaimsPrincipal user) => user.IsInRole("Manajer") || user.IsInRole("Admin");
}