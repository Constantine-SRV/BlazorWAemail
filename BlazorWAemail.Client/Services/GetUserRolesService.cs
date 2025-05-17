using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace BlazorWAemail.Client.Services;

/// <summary>
/// Fetches user roles from /api/UserRoles and caches them in localStorage.
/// The cache contains the roles list and the UTC timestamp of retrieval.
/// </summary>
public sealed class GetUserRolesService
{
    private const string CacheKey = "roles";                // localStorage key
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    private readonly HttpClient _http;
    private readonly ILocalStorageService _storage;
    private readonly AuthService _auth;
    private readonly AuthenticationStateProvider _authState;

    public GetUserRolesService(HttpClient http,
                               ILocalStorageService storage,
                               AuthService auth,
                               AuthenticationStateProvider authState)
    {
        _http      = http;
        _storage   = storage;
        _auth      = auth;
        _authState = authState;
    }

    /// <summary>
    /// Returns roles from cache if fresh; otherwise fetches from API and updates cache.
    /// </summary>
    public async Task<List<string>> GetRolesAsync()
    {
        // --- 1. raw JSON from localStorage ------------------------------------
        string? json = null;
        try
        {
            json = await _storage.GetItemAsync<string>(CacheKey);
        }
        catch
        {
            // extremely rare: even string retrieval failed – just ignore
        }

        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                var cached = JsonSerializer.Deserialize<RoleCacheDto>(json);
                if (cached is { Roles: not null } &&
                    DateTime.UtcNow - cached.StoredUtc < Ttl)
                {
                    return cached.Roles;
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[roles-cache] broken json → drop: {ex.Message}");
                await _storage.RemoveItemAsync(CacheKey);
            }
        }

        // --- 2. API call -------------------------------------------------------
        var resp = await _http.GetAsync("api/UserRoles");

        if (resp.StatusCode == HttpStatusCode.Unauthorized)
        {
            await _auth.FullLogoutAllDevicesAsync(_authState);
            throw new UnauthorizedAccessException("Roles API returned 401.");
        }

        resp.EnsureSuccessStatusCode();
        var roles = await resp.Content.ReadFromJsonAsync<List<string>>() ?? new();

        // --- 3. save fresh cache ----------------------------------------------
        var dto = new RoleCacheDto { Roles = roles, StoredUtc = DateTime.UtcNow };
        var newJson = JsonSerializer.Serialize(dto);
        await _storage.SetItemAsync(CacheKey, newJson);

        return roles;
    }


    /// <summary>Removes cached roles (call on logout).</summary>
    public async Task ClearAsync() =>
        await _storage.RemoveItemAsync(CacheKey);

    /* ---------- DTO for serialization ---------- */
    private sealed class RoleCacheDto
    {
        public List<string> Roles { get; set; } = new();
        public DateTime StoredUtc { get; set; }
    }
}
