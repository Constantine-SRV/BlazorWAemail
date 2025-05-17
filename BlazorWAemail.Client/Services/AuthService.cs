using System.Net.Http.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using BlazorWAemail.Shared;

namespace BlazorWAemail.Client.Services;

/// <summary>
/// Handles login (email + code) and all logout cases.
/// Scoped lifetime: lives for the lifetime of the browser tab.
/// </summary>
public class AuthService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;
    private readonly IJSRuntime _js;
 

    public AuthService(HttpClient http,
                       ILocalStorageService localStorage,
                       IJSRuntime js )
    {
        _http         = http;
        _localStorage = localStorage;
        _js           = js;
      
    }

    /* ---------- LOGIN ---------- */

    public async Task<bool> SendCodeAsync(string email)
    {
        var resp = await _http.PostAsJsonAsync("/api/Auth/sendcode",
                                               new SendCodeRequest { Email = email });
        return resp.IsSuccessStatusCode;
    }

    public async Task<AuthResult?> VerifyCodeAsync(string email, string code)
    {
        var resp = await _http.PostAsJsonAsync("/api/Auth/verifycode",
                                               new VerifyCodeRequest { Email = email, Code = code });

        if (!resp.IsSuccessStatusCode)
            return null;

        var result = await resp.Content.ReadFromJsonAsync<AuthResult>();
        if (result is not null)
            await _localStorage.SetItemAsync("authToken", result.Token);

        return result;
    }

    /* ---------- LOGOUT (helpers) ---------- */

    /// <summary>Logout on this browser only: clear token, roles cache, auth-state.</summary>
    public async Task LocalLogoutAsync(AuthenticationStateProvider authProvider)
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", "authToken");
        await _js.InvokeVoidAsync("localStorage.removeItem", "roles");
       
        if (authProvider is CustomAuthenticationStateProvider custom)
            await custom.MarkUserAsLoggedOut();
    }

    /// <summary>
    /// Global logout (delete tokens in DB) + local cleanup.
    /// </summary>
    public async Task FullLogoutAllDevicesAsync(AuthenticationStateProvider authProvider)
    {
        try
        {
            await _http.PostAsync("api/auth/logoutall", null); // 401/500 are ignored
        }
        catch { /* network errors ignored */ }

        await LocalLogoutAsync(authProvider);
    }
}
