using Supabase;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

// 2. Configuração do Supabase
var url = Environment.GetEnvironmentVariable("SUPABASE_URL");
var key = Environment.GetEnvironmentVariable("SUPABASE_KEY");

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Isso evita que o BaseModel trave o JSON
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // Mantém os nomes como estão no Model
    });

// Isso evita que o site dê erro se as variáveis estiverem vazias
if (!string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(key))
{
    builder.Services.AddScoped(_ => new Supabase.Client(url, key));
}

// 3. Configuração de Sessão (Essencial para o Modo ADM funcionar)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Adiciona suporte a HttpContext para o Layout acessar a Session
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews();

var app = builder.Build();
app.UseCors("AllowAll");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// 4. Ativar Sessão (Sempre antes de Authorization)
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();