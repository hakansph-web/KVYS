using Blazored.LocalStorage;
using KVYS.Web.Components;
using KVYS.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Blazored LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Add Authentication & Authorization
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "BlazorServer";
    options.DefaultChallengeScheme = "BlazorServer";
}).AddCookie("BlazorServer", options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/login";
});
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

// Register AuthenticationStateProvider
builder.Services.AddScoped<KvysAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<KvysAuthStateProvider>());

// Configure HttpClient for API
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5268";
builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient<AuthService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
