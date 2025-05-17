using System.Net.Http.Json;
using Blazored.LocalStorage;
using BlazorWAemail.Shared;

public class AuthService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;

    public AuthService(HttpClient http, ILocalStorageService localStorage)
    {
        _http = http;
        _localStorage = localStorage;
    }

    public async Task<bool> SendCodeAsync(string email)
    {
        var response = await _http.PostAsJsonAsync("/api/Auth/sendcode", new SendCodeRequest { Email = email });
        return response.IsSuccessStatusCode;
    }

    public async Task<AuthResult?> VerifyCodeAsync(string email, string code)
    {
        var response = await _http.PostAsJsonAsync("/api/Auth/verifycode", new VerifyCodeRequest { Email = email, Code = code });
        if (!response.IsSuccessStatusCode)
            return null;
        var result = await response.Content.ReadFromJsonAsync<AuthResult>();
        if (result != null)
        {
            await _localStorage.SetItemAsync("authToken", result.Token);
        }
        return result;
    }

    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync("authToken");
    }

    public async Task<string?> GetTokenAsync()
    {
        return await _localStorage.GetItemAsync<string>("authToken");
    }
    public async Task LogoutAllDevicesAsync()
    {
        await _http.PostAsync("api/auth/logoutall", null); 
    }

}
