using Supabase;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);

// 1. CONFIGURAÇÃO DE REDE E JSON
builder.Services.AddHttpClient();

// Define a política de CORS para permitir que o site local acesse o Render
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configuração de Controllers otimizada para letras minúsculas
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // ESSENCIAL: Mantemos isso porque o BaseModel do Supabase ainda tem referências circulares
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;

        // Como agora seu Model é minúsculo (igual ao JSON), não precisamos forçar NamingPolicy
        options.JsonSerializerOptions.PropertyNamingPolicy = null;

        // Mantemos como garantia para pequenas variações
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// 2. CONFIGURAÇÃO DO SUPABASE
var url = Environment.GetEnvironmentVariable("SUPABASE_URL");
var key = Environment.GetEnvironmentVariable("SUPABASE_KEY");

// Registramos o cliente (O Render vai ler as variáveis de ambiente aqui)
builder.Services.AddScoped(_ => new Supabase.Client(url!, key!));

// 3. SESSÃO E ACESSO AO CONTEXTO
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// 4. PIPELINE DE EXECUÇÃO
app.UseCors("AllowAll");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Sessão SEMPRE antes de Authorization
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();