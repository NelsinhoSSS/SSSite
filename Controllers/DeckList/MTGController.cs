using Microsoft.AspNetCore.Mvc;
using SSSite.Models;
using System.Text;
using System.Text.Json;

namespace SSSite.Controllers
{
    public class MTGController : Controller
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public MTGController(IHttpClientFactory httpFactory)
        {
            _httpFactory = httpFactory;
        }

        // 1. INDEX
        public async Task<IActionResult> Index()
        {
            var vm = new MtgViewModel();
            try
            {
                var client = _httpFactory.CreateClient("Supabase");
                var res = await client.GetAsync("/rest/v1/mtg_decks?select=*&order=criado_em.desc");
                if (res.IsSuccessStatusCode)
                {
                    var body = await res.Content.ReadAsStringAsync();
                    var decks = JsonSerializer.Deserialize<List<MtgDeck>>(body, _jsonOpts) ?? new();

                    foreach (var deck in decks)
                        if (!string.IsNullOrEmpty(deck.Lista))
                            deck.Cartas = ParsearLista(deck.Lista);

                    vm.Decks = decks;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MTG] Erro: {ex.Message}");
            }

            return View("~/Views/Decklist/MTG.cshtml", vm);
        }

        // 2. CRIAR DECK COM LISTA
        [HttpPost]
        public async Task<IActionResult> CriarDeckComLista(string nome, string? comandante, string? listaTexto)
        {
            if (string.IsNullOrWhiteSpace(nome)) return RedirectToAction("Index");

            try
            {
                var client = _httpFactory.CreateClient("Supabase");
                var listaFormatada = ConverterParaStorage(listaTexto);
                var payload = new { nome = nome.Trim(), comandante = comandante?.Trim(), lista = listaFormatada };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                await client.PostAsync("/rest/v1/mtg_decks", content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MTG] Erro ao criar deck: {ex.Message}");
            }

            return RedirectToAction("Index");
        }

        // 3. EDITAR LISTA
        [HttpPost]
        public async Task<IActionResult> SalvarCartas(long deckId, string listaTexto)
        {
            try
            {
                var client = _httpFactory.CreateClient("Supabase");
                var listaFormatada = ConverterParaStorage(listaTexto);
                var payload = new { lista = listaFormatada };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                await client.PatchAsync($"/rest/v1/mtg_decks?id=eq.{deckId}", content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MTG] Erro ao salvar lista: {ex.Message}");
            }

            return RedirectToAction("Index");
        }

        // 4. EXCLUIR DECK
        [HttpPost]
        public async Task<IActionResult> ExcluirDeck(long deckId)
        {
            try
            {
                var client = _httpFactory.CreateClient("Supabase");
                await client.DeleteAsync($"/rest/v1/mtg_decks?id=eq.{deckId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MTG] Erro ao excluir: {ex.Message}");
            }

            return RedirectToAction("Index");
        }

        // 5. PARSEAR LISTA (separador: |)
        private List<MtgCarta> ParsearLista(string lista)
        {
            var cartas = new List<MtgCarta>();
            foreach (var entry in lista.Split('|'))
            {
                var l = entry.Trim();
                if (string.IsNullOrEmpty(l)) continue;

                var partes = l.Split(' ', 2);
                if (partes.Length == 2 && int.TryParse(partes[0], out int qtd))
                {
                    cartas.Add(new MtgCarta
                    {
                        Quantidade = qtd,
                        Nome = partes[1].Trim()
                    });
                }
            }
            return cartas;
        }

        // 6. CONVERTER LISTA DO USUÁRIO (linhas) PARA STORAGE (|)
        private string ConverterParaStorage(string? listaTexto)
        {
            if (string.IsNullOrWhiteSpace(listaTexto)) return "";

            var entradas = listaTexto
                .Split('\n')
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrEmpty(l) && !l.StartsWith("//"));

            return string.Join("|", entradas);
        }
    }
}