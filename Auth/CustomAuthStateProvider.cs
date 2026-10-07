using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace StokFlow.Web.Auth;

// "Kartu tanda masuk" yang disimpan di browser
public record UserSession(string Username, string FullName, string Role);

// Penjaga yang selalu bisa ditanya: "siapa yang sedang login?"
public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ProtectedSessionStorage _storage;   // penyimpanan browser yang dienkripsi
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    public CustomAuthStateProvider(ProtectedSessionStorage storage)
    {
        _storage = storage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var result = await _storage.GetAsync<UserSession>("userSession");
            var session = result.Success ? result.Value : null;
            return new AuthenticationState(session == null ? Anonymous : CreatePrincipal(session));
        }
        catch
        {
            return new AuthenticationState(Anonymous);   // kalau gagal dibaca, anggap belum login
        }
    }

    public async Task LoginAsync(UserSession session)
    {
        await _storage.SetAsync("userSession", session);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(CreatePrincipal(session))));
    }

    public async Task LogoutAsync()
    {
        await _storage.DeleteAsync("userSession");
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(Anonymous)));
    }

    // Mengubah kartu tanda masuk menjadi "identitas" yang dimengerti .NET
    private static ClaimsPrincipal CreatePrincipal(UserSession s) =>
        new(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, s.Username),
            new Claim("FullName", s.FullName),
            new Claim(ClaimTypes.Role, s.Role)
        }, "StokFlowAuth"));
}