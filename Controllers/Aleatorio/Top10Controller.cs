using Microsoft.AspNetCore.Mvc;
using SSSite.Models;
using System.Text.Json;

namespace SSSite.Controllers
{
    public class Top10Controller : Controller
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public Top10Controller(IHttpClientFactory httpFactory)
        {
            _httpFactory = httpFactory;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new Top10ViewModel();

            try
            {
                var client = _httpFactory.CreateClient("Supabase");

                // Busca todos os sorteios direto da tabela
                var res = await client.GetAsync("/rest/v1/sorteios?select=*&order=sorteado_em.desc");

                if (res.IsSuccessStatusCode)
                {
                    var body = await res.Content.ReadAsStringAsync();
                    var sorteios = JsonSerializer.Deserialize<List<UltimoSorteio>>(body, _jsonOpts) ?? new();

                    // Último sorteio
                    vm.Ultimo = sorteios.FirstOrDefault();

                    // Contagem por número (1 a 10) feita no C#
                    vm.Contagens = Enumerable.Range(1, 10).Select(n => new ContagemNumero
                    {
                        Numero = n,
                        TotalSorteios = sorteios.Count(s => s.Numero == n),
                        UltimoSorteio = sorteios.Where(s => s.Numero == n)
                                                 .Select(s => (DateTime?)s.SorteadoEm)
                                                 .FirstOrDefault()
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Top10] Erro: {ex.Message}");
            }

            return View(vm);
        }
    }
}