using Microsoft.AspNetCore.Mvc;
using SSSite.Models;
using System.Text.Json;

namespace SSSite.Controllers
{
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

            var bodyContagem = await resContagem.Content.ReadAsStringAsync();
            Console.WriteLine($"[Top10] Status contagem: {resContagem.StatusCode}");
            Console.WriteLine($"[Top10] Body contagem: {bodyContagem}");

            if (resContagem.IsSuccessStatusCode)
                vm.Contagens = JsonSerializer.Deserialize<List<ContagemNumero>>(bodyContagem, opts) ?? new();

            // Busca último sorteio
            var resUltimo = await client.GetAsync(
                "/rest/v1/sorteios?select=numero,sorteado_em&order=sorteado_em.desc&limit=1");

            var bodyUltimo = await resUltimo.Content.ReadAsStringAsync();
            Console.WriteLine($"[Top10] Status ultimo: {resUltimo.StatusCode}");
            Console.WriteLine($"[Top10] Body ultimo: {bodyUltimo}");

            if (resUltimo.IsSuccessStatusCode)
            {
                var lista = JsonSerializer.Deserialize<List<UltimoSorteio>>(bodyUltimo, opts);
                vm.Ultimo = lista?.FirstOrDefault();
            }

            return View(vm);
        }
    }
}