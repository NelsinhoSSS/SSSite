using Supabase;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);

// 1. CONFIGURAÇÃO DE REDE E JSON
builder.Services.AddHttpClient();

// Define a política de CORS para que o seu site local consiga ler os dados do Render
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configura os Controllers para não travarem ao ler o Supabase (BaseModel)
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // ESSENCIAL: Evita o erro 500 ao transformar dados do Supabase em JSON
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;

        // Mantém os nomes das propriedades exatamente como no Model (Maiúsculas)
        options.JsonSerializerOptions.PropertyNamingPolicy = null;

        // Aceita qualquer variação de maiúscula/minúscula no JSON
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// 2. CONFIGURAÇÃO DO SUPABASE
var url = Environment.GetEnvironmentVariable("SUPABASE_URL");
var key = Environment.GetEnvironmentVariable("SUPABASE_KEY");

// Registramos o cliente do Supabase. 
// Se as chaves estiverem vazias no Render, o erro aparecerá nos logs.
builder.Services.AddScoped(_ => new Supabase.Client(url!, key!));

// 3. CONFIGURAÇÃO DE SESSÃO E CONTEXTO
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
app.UseCors("AllowAll"); // Ativa a permissão de acesso que definimos acima

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();