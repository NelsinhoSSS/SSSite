using Microsoft.AspNetCore.Mvc;
using SSSite.Models;
using System.Text.Json;

namespace SSSite.Controllers
{
    public class SorteioBackgroundService : BackgroundService
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILogger<SorteioBackgroundService> _logger;

        public SorteioBackgroundService(IHttpClientFactory httpFactory,
                                        ILogger<SorteioBackgroundService> logger)
        {
            _httpFactory = httpFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var agora = DateTime.UtcNow;

                // Calcula quanto tempo falta para meia-noite UTC
                var proximoSorteio = agora.Date.AddDays(1);
                var espera = proximoSorteio - agora;

                _logger.LogInformation("Próximo sorteio em {Espera}", espera);
                await Task.Delay(espera, stoppingToken);

                await RealizarSorteioAsync();
            }
        }

        private async Task RealizarSorteioAsync()
        {
            try
            {
                var numero = System.Random.Shared.Next(1, 11); // 1 a 10
                var client = _httpFactory.CreateClient("Supabase");

                var payload = new { numero };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync("/rest/v1/sorteios", content);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Número {Numero} sorteado com sucesso!", numero);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao realizar o sorteio diário.");
            }
        }
    }

    public class Top10Controller : Controller
    {
        private readonly IHttpClientFactory _httpFactory;

        public Top10Controller(IHttpClientFactory httpFactory)
        {
            _httpFactory = httpFactory;
        }

        public async Task<IActionResult> Index()
        {
            var client = _httpFactory.CreateClient("Supabase");
            var vm = new Top10ViewModel();
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Busca contagem por número (via view)
            var resContagem = await client.GetAsync(
                "/rest/v1/contagem_numeros?select=numero,total_sorteios,ultimo_sorteio&order=numero.asc");

            if (resContagem.IsSuccessStatusCode)
            {
                var body = await resContagem.Content.ReadAsStringAsync();
                vm.Contagens = JsonSerializer.Deserialize<List<ContagemNumero>>(body, opts) ?? new();
            }

            // Busca último sorteio
            var resUltimo = await client.GetAsync(
                "/rest/v1/sorteios?select=numero,sorteado_em&order=sorteado_em.desc&limit=1");

            if (resUltimo.IsSuccessStatusCode)
            {
                var body = await resUltimo.Content.ReadAsStringAsync();
                var lista = JsonSerializer.Deserialize<List<UltimoSorteio>>(body, opts);
                vm.Ultimo = lista?.FirstOrDefault();
            }

            return View(vm);
        }
    }
}