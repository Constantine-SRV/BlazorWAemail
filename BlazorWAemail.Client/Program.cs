using BlazorWAemail.Client;
using BlazorWAemail.Client.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Net.Http;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// root components
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// local-storage for persisting the JWT
builder.Services.AddBlazoredLocalStorage();

// handler that adds the JWT from localStorage to every outgoing request
builder.Services.AddScoped<ApiAuthorizationMessageHandler>();

// single HttpClient for all API calls (points to the server)
builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri("http://localhost:5005/");
})
.AddHttpMessageHandler<ApiAuthorizationMessageHandler>();

// services that rely on ApiClient
builder.Services.AddScoped<AuthService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var apiClient = factory.CreateClient("ApiClient");
    var storage = sp.GetRequiredService<Blazored.LocalStorage.ILocalStorageService>();
    return new AuthService(apiClient, storage);
});
builder.Services.AddScoped<GetUserRolesService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var apiClient = factory.CreateClient("ApiClient");
    return new GetUserRolesService(apiClient);
});

// Blazor auth setup
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

await builder.Build().RunAsync();
