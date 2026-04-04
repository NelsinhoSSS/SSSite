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