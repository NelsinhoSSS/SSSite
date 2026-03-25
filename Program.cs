using Supabase;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);

// 1. CONFIGURAÇÃO DO SUPABASE
// Ele tenta pegar do Sistema (Render) ou do JSON (Visual Studio)
var url = Environment.GetEnvironmentVariable("SUPABASE_URL") ?? builder.Configuration["Supabase:Url"];
var key = Environment.GetEnvironmentVariable("SUPABASE_KEY") ?? builder.Configuration["Supabase:Key"];

if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(key))
{
    // Isso evita o erro de tela vermelha e te avisa no console o que falta
    Console.WriteLine("AVISO: Chaves do Supabase não encontradas!");
}
else
{
    builder.Services.AddScoped(_ => new Supabase.Client(url, key, new SupabaseOptions { AutoConnectRealtime = true }));
}

// 2. SERVIÇOS E CONTROLLERS
builder.Services.AddHttpClient();
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

var app = builder.Build();

// 3. PIPELINE
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();