using MTGProxyApp.Components;
using MTGProxyApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddHttpClient<HttpService>();
builder.Services.AddHttpClient<ScryfallService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.MapControllers();
app.MapBlazorHub();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<MTGProxyApp.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();