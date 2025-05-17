using BlazorWAemail.Client;
using BlazorWAemail.Client.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// root components
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// local storage (JWT)
builder.Services.AddBlazoredLocalStorage();

// handler that adds JWT to each request
builder.Services.AddScoped<ApiAuthorizationMessageHandler>();

// single HttpClient for API
builder.Services.AddHttpClient("ApiClient", c =>
{
    c.BaseAddress = new Uri("http://localhost:5005/");
})
.AddHttpMessageHandler<ApiAuthorizationMessageHandler>();

// ----------------- application services -----------------

// AuthService (scoped)
builder.Services.AddScoped<AuthService>(sp =>
{
    var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("ApiClient");
    var store = sp.GetRequiredService<ILocalStorageService>();
    var js = sp.GetRequiredService<IJSRuntime>();

    return new AuthService(http, store, js);
});

// GetUserRolesService (scoped – кеш сидит в localStorage, не в RAM)
builder.Services.AddScoped<GetUserRolesService>(sp =>
{
    var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("ApiClient");
    var storage = sp.GetRequiredService<ILocalStorageService>();
    var auth = sp.GetRequiredService<AuthService>();
    var authState = sp.GetRequiredService<AuthenticationStateProvider>();

    return new GetUserRolesService(http, storage, auth, authState);
});
// blazor auth plumbing
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider,
                            CustomAuthenticationStateProvider>();

await builder.Build().RunAsync();
