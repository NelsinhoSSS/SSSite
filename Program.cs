var builder = WebApplication.CreateBuilder(args);

// 1. CONFIGURAÇÃO DO SUPABASE
var url = Environment.GetEnvironmentVariable("SUPABASE_URL") ?? builder.Configuration["Supabase:Url"];
var key = Environment.GetEnvironmentVariable("SUPABASE_KEY") ?? builder.Configuration["Supabase:Key"];

if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(key))
{
    Console.WriteLine("AVISO: Chaves do Supabase não encontradas!");
}
else
{
    builder.Services.AddHttpClient("Supabase", client =>
    {
        client.BaseAddress = new Uri(url);
        client.DefaultRequestHeaders.Add("apikey", key);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {key}");
    });
}

// 2. SERVIÇOS E CONTROLLERS
builder.Services.AddHttpClient(); // usado pelo MtgController para chamar a API do Gemini
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