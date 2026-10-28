using System.Security.Claims;
using MafaliCrm.Web.Components;
using MafaliCrm.Web.Data;
using MafaliCrm.Web.Models;
using MafaliCrm.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

if (Environment.GetEnvironmentVariable("PORT") is { Length: > 0 } port)
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}
// Every interface, not just localhost, so other machines on the office LAN can
// reach it. Plain HTTP is deliberate; see docs/01-installation.md.
else if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.UseUrls("http://0.0.0.0:5298");
}

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Without persisted keys a fresh one is generated per start, invalidating every
// auth cookie. Keep and back up this folder, or every restart logs everyone out.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "AppData", "DataProtectionKeys")))
    .SetApplicationName("MafaliCrm");

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
    });

// Everything requires authentication by default. The custom requirement exempts
// /_framework and /_blazor, without which the Blazor runtime cannot load.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .AddRequirements(new AuthenticatedUnlessBlazorInfrastructure())
        .Build();
});
builder.Services.AddSingleton<IAuthorizationHandler, AuthenticatedUnlessBlazorInfrastructureHandler>();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<CurrentUserService>();

builder.Services.AddScoped<UserClock>();

// Factory, not AddDbContext: a Blazor Server DI scope lasts the whole browser
// session. Never inject AppDbContext directly — take a fresh one per operation.
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<FamilleService>();
builder.Services.AddScoped<FranchiseService>();
builder.Services.AddScoped<PaysService>();
builder.Services.AddScoped<AssistanteService>();
builder.Services.AddScoped<ClientService>();
builder.Services.AddScoped<ClientGridQueryService>();
builder.Services.AddScoped<ClientValidationService>();
builder.Services.AddScoped<ClientFilterService>();
builder.Services.AddScoped<FiltreOperatriceService>();
builder.Services.AddScoped<HistoriqueService>();
builder.Services.AddScoped<HistoriqueFilterService>();
builder.Services.AddScoped<FiltreHistoriqueService>();
builder.Services.AddScoped<CaService>();
builder.Services.AddScoped<ClientImportExportService>();
builder.Services.AddScoped<RappelService>();
builder.Services.AddScoped<BonCommandeTemplateService>();
builder.Services.AddScoped<BonCommandeEnCoursService>();
builder.Services.AddScoped<DevisEnCoursService>();

builder.Services.AddScoped<CircuitContext>();
builder.Services.AddSingleton<ConnectedUserRegistry>();
// Registered on the CircuitHandler base type: that is what Blazor resolves.
// Registered on the concrete class it would silently never be called.
builder.Services.AddScoped<CircuitHandler, ConnectedUserCircuitHandler>();

builder.Services.AddSingleton<RappelPopupDispatcher>();
builder.Services.AddHostedService<RappelScanService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/login", async (
    [FromForm] string login,
    [FromForm] string password,
    [FromForm] string? returnUrl,
    HttpContext http,
    UserService userService) =>
{
    var (result, user) = await userService.LoginAsync(login, password);
    if (result != LoginResult.Success || user is null)
    {
        var reason = result switch
        {
            LoginResult.Inactive => "inactive",
            LoginResult.LockedOut => "locked",
            _ => "invalid",
        };
        return Results.Redirect($"/login?error={reason}");
    }

    var identity = new ClaimsIdentity(
        [new Claim(ClaimTypes.Name, user.Login), new Claim(ClaimTypes.Role, user.Role)],
        CookieAuthenticationDefaults.AuthenticationScheme);
    await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

    return Results.Redirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
})
// Antiforgery off: a token is bound to the identity that issued it, which breaks
// revisiting /login while already signed in. Anonymous: nobody is logged in yet.
.DisableAntiforgery()
.AllowAnonymous();

app.MapPost("/logout", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
