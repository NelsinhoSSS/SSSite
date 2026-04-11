using Microsoft.AspNetCore.Mvc;
using SSSite.Models;
using System.Text;
using System.Text.Json;

namespace SSSite.Controllers
{
    // Controller responsável por toda a lógica de decks de Yu-Gi-Oh!
    // Estrutura praticamente idêntica ao MTGController — a principal diferença
    // está no ParsearLista (ponto 5), que trata o formato "3x" usado no Yu-Gi-Oh!
    public class YuGiOhController : Controller
    {
        // Fábrica de HttpClient injetada via construtor.
        // Evita problemas de "socket exhaustion" ao reutilizar conexões de forma segura.
        private readonly IHttpClientFactory _httpFactory;

        // Configuração de desserialização JSON: ignora diferença entre maiúsculas
        // e minúsculas. Ex: "nome" no JSON casa com "Nome" no modelo C#.
        private readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        // Construtor com injeção de dependência.
        // O ASP.NET injeta automaticamente o IHttpClientFactory configurado no Program.cs.
        public YuGiOhController(IHttpClientFactory httpFactory)
        {
            _httpFactory = httpFactory;
        }

        // ─────────────────────────────────────────────
        // 1. INDEX — Página principal com todos os decks
        // ─────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            // Cria o ViewModel vazio que será preenchido e enviado à View.
            var vm = new YuGiOhViewModel();

            try
            {
                // Obtém o HttpClient pré-configurado com a URL base e headers do Supabase.
                var client = _httpFactory.CreateClient("Supabase");

                // GET na API REST do Supabase:
                // - "select=*"              → traz todas as colunas da tabela
                // - "order=criado_em.desc"  → ordena do mais recente para o mais antigo
                var res = await client.GetAsync("/rest/v1/yugioh_decks?select=*&order=criado_em.desc");

                if (res.IsSuccessStatusCode) // Verifica se a resposta foi 2xx (sucesso)
                {
                    // Lê o corpo da resposta como string JSON.
                    var body = await res.Content.ReadAsStringAsync();

                    // Desserializa o JSON para uma lista de objetos YuGiOhDeck.
                    // "?? new()" garante lista vazia em vez de null caso a desserialização falhe.
                    var decks = JsonSerializer.Deserialize<List<YuGiOhDeck>>(body, _jsonOpts) ?? new();

                    // Converte a lista armazenada (separada por "|") em objetos YuGiOhCarta
                    // para que a View possa exibir as cartas de forma estruturada.
                    foreach (var deck in decks)
                        if (!string.IsNullOrEmpty(deck.Lista))
                            deck.Cartas = ParsearLista(deck.Lista);

                    vm.Decks = decks;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[YuGiOh] Erro: {ex.Message}");
            }

            // Renderiza a View passando o ViewModel.
            // Caminho explícito porque a View está em Views/Decklist/, não em Views/YuGiOh/.
            return View("~/Views/Decklist/YuGiOh.cshtml", vm);
        }

        // ──────────────────────────────────────────────────────
        // 2. CRIAR DECK COM LISTA — Insere um novo deck no banco
        // ──────────────────────────────────────────────────────
        // [HttpPost] restringe esta action a requisições POST (vindas de um <form>).
        [HttpPost]
        public async Task<IActionResult> CriarDeckComLista(string nome, string? listaTexto)
        {
            // Validação: se o nome estiver vazio, volta para a Index sem fazer nada.
            if (string.IsNullOrWhiteSpace(nome)) return RedirectToAction("Index");

            try
            {
                var client = _httpFactory.CreateClient("Supabase");

                // Converte o texto do textarea (linhas) para o formato de storage (separado por "|").
                var listaFormatada = ConverterParaStorage(listaTexto);

                // Objeto anônimo com os dados a serem inseridos no banco.
                var payload = new { nome = nome.Trim(), lista = listaFormatada };

                // Serializa para JSON e empacota no corpo HTTP com encoding UTF-8.
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                // POST insere um novo registro na tabela "yugioh_decks".
                await client.PostAsync("/rest/v1/yugioh_decks", content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[YuGiOh] Erro ao criar deck: {ex.Message}");
            }

            // Padrão PRG (Post/Redirect/Get): redireciona após o POST para evitar
            // reenvio do formulário ao atualizar a página.
            return RedirectToAction("Index");
        }

        // ────────────────────────────────────────────────────────────
        // 3. SALVAR CARTAS — Atualiza a lista de cartas de um deck existente
        // ────────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> SalvarCartas(long deckId, string listaTexto)
        {
            try
            {
                var client = _httpFactory.CreateClient("Supabase");
                var listaFormatada = ConverterParaStorage(listaTexto);

                // Monta apenas o campo "lista" para atualização parcial.
                var payload = new { lista = listaFormatada };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                // PATCH atualiza apenas os campos enviados (não sobrescreve os outros).
                // "?id=eq.{deckId}" → sintaxe do Supabase REST equivalente a WHERE id = deckId.
                await client.PatchAsync($"/rest/v1/yugioh_decks?id=eq.{deckId}", content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[YuGiOh] Erro ao salvar lista: {ex.Message}");
            }

            return RedirectToAction("Index");
        }

        // ─────────────────────────────────────────────────────
        // 4. EXCLUIR DECK — Remove um deck do banco pelo seu id
        // ─────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> ExcluirDeck(long deckId)
        {
            try
            {
                var client = _httpFactory.CreateClient("Supabase");

                // DELETE com filtro pelo id — remove o registro correspondente.
                await client.DeleteAsync($"/rest/v1/yugioh_decks?id=eq.{deckId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[YuGiOh] Erro ao excluir: {ex.Message}");
            }

            return RedirectToAction("Index");
        }

        // ────────────────────────────────────────────────────────────────────
        // 5. PARSEAR LISTA — Converte a string do banco para objetos YuGiOhCarta
        // ────────────────────────────────────────────────────────────────────
        //
        // Recebe: "3x Dark Magician|2x Blue-Eyes White Dragon|1x Pot of Greed"
        // Retorna: lista de YuGiOhCarta com Quantidade e Nome separados.
        private List<YuGiOhCarta> ParsearLista(string lista)
        {
            var cartas = new List<YuGiOhCarta>();

            // Divide a string armazenada pelo separador "|".
            foreach (var entry in lista.Split('|'))
            {
                var l = entry.Trim();
                if (string.IsNullOrEmpty(l)) continue; // Ignora entradas vazias

                // Divide pelo primeiro espaço em no máximo 2 partes:
                // Ex: "3x Dark Magician" → ["3x", "Dark Magician"]
                var partes = l.Split(' ', 2);

                // TrimEnd('x', 'X') remove o "x" ou "X" do final da quantidade.
                // Ex: "3x" → "3" → int.TryParse converte para 3.
                // int.TryParse retorna false (e não lança exceção) se a conversão falhar.
                if (partes.Length == 2 && int.TryParse(partes[0].TrimEnd('x', 'X'), out int qtd))
                {
                    cartas.Add(new YuGiOhCarta
                    {
                        Quantidade = qtd,
                        Nome = partes[1].Trim()
                    });
                }
            }

            return cartas;
        }

        // ──────────────────────────────────────────────────────────────────────
        // 6. CONVERTER PARA STORAGE — Converte texto do usuário para formato "|"
        // ──────────────────────────────────────────────────────────────────────
        // Recebe o texto do textarea (uma carta por linha):
        //   "3x Dark Magician
        //    2x Blue-Eyes White Dragon
        //    // Extras"     ← linhas iniciadas com "//" são comentários e serão ignoradas
        //
        // Retorna: "3x Dark Magician|2x Blue-Eyes White Dragon"
        private string ConverterParaStorage(string? listaTexto)
        {
            if (string.IsNullOrWhiteSpace(listaTexto)) return ""; // Proteção contra entrada nula

            var entradas = listaTexto
                .Split('\n')                              // Quebra o texto linha por linha
                .Select(l => l.Trim())                    // Remove espaços/\r de cada linha
                .Where(l => !string.IsNullOrEmpty(l)      // Filtra linhas vazias...
                         && !l.StartsWith("//"));         // ...e comentários estilo "//"

            // Une todas as linhas válidas com "|" para salvar como uma única string no banco.
            return string.Join("|", entradas);
        }
    }
}