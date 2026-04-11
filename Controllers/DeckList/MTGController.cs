using Microsoft.AspNetCore.Mvc;
using SSSite.Models;
using System.Text;
using System.Text.Json;

namespace SSSite.Controllers
{
    // Controller responsável por toda a lógica de decks de Magic: The Gathering.
    // Herda de "Controller", que é a classe base do ASP.NET MVC e fornece
    // métodos como View(), RedirectToAction(), etc.
    public class MTGController : Controller
    {
        // IHttpClientFactory é uma fábrica que cria instâncias de HttpClient de forma
        // segura e reutilizável. Evita o problema de "socket exhaustion" que ocorre
        // ao instanciar HttpClient diretamente (new HttpClient()).
        private readonly IHttpClientFactory _httpFactory;

        // Opções de desserialização JSON: ignora diferenças entre maiúsculas e
        // minúsculas nos nomes das propriedades.
        // Ex: "nome" no JSON bate com "Nome" no modelo C#.
        private readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        // Construtor com injeção de dependência.
        // O ASP.NET automaticamente passa o IHttpClientFactory aqui (configurado no Program.cs).
        public MTGController(IHttpClientFactory httpFactory)
        {
            _httpFactory = httpFactory;
        }

        // ─────────────────────────────────────────────
        // 1. INDEX — Página principal com todos os decks
        // ─────────────────────────────────────────────
        // "async Task<IActionResult>" significa que o método é assíncrono:
        // ele não trava a thread enquanto espera a resposta do banco.
        public async Task<IActionResult> Index()
        {
            // Cria um ViewModel vazio para passar dados à View.
            // ViewModel é um objeto intermediário entre o Controller e a View.
            var vm = new MtgViewModel();

            try
            {
                // Cria um HttpClient nomeado "Supabase" (pré-configurado no Program.cs
                // com a URL base e os headers de autenticação do Supabase).
                var client = _httpFactory.CreateClient("Supabase");

                // Faz uma requisição GET à API REST do Supabase.
                // - "select=*"         → traz todas as colunas
                // - "order=criado_em.desc" → ordena do mais recente para o mais antigo
                var res = await client.GetAsync("/rest/v1/mtg_decks?select=*&order=criado_em.desc");

                if (res.IsSuccessStatusCode) // Verifica se a resposta foi 2xx (sucesso)
                {
                    // Lê o corpo da resposta HTTP como string (JSON cru).
                    var body = await res.Content.ReadAsStringAsync();

                    // Desserializa o JSON para uma lista de objetos MtgDeck.
                    // "?? new()" garante que nunca seja null — retorna lista vazia se falhar.
                    var decks = JsonSerializer.Deserialize<List<MtgDeck>>(body, _jsonOpts) ?? new();

                    // Para cada deck retornado, converte a lista armazenada (formato "|")
                    // em uma lista de objetos MtgCarta usável pela View.
                    foreach (var deck in decks)
                        if (!string.IsNullOrEmpty(deck.Lista))
                            deck.Cartas = ParsearLista(deck.Lista);

                    vm.Decks = decks; // Passa os decks para o ViewModel
                }
            }
            catch (Exception ex)
            {
                // Loga o erro no console. Em produção, seria ideal usar ILogger.
                Console.WriteLine($"[MTG] Erro: {ex.Message}");
            }

            // Renderiza a View passando o ViewModel.
            // O caminho explícito é necessário porque a View não está na pasta padrão
            // (que seria Views/MTG/), e sim em Views/Decklist/.
            return View("~/Views/Decklist/MTG.cshtml", vm);
        }

        // ─────────────────────────────────────────────────────
        // 2. CRIAR DECK COM LISTA — Cria um novo deck no banco
        // ─────────────────────────────────────────────────────
        // [HttpPost] significa que esta action só responde a requisições POST
        // (vindas de um <form method="post"> no HTML, por exemplo).
        [HttpPost]
        public async Task<IActionResult> CriarDeckComLista(string nome, string? comandante, string? listaTexto)
        {
            // Validação básica: se o nome estiver vazio, volta para a Index sem fazer nada.
            if (string.IsNullOrWhiteSpace(nome)) return RedirectToAction("Index");

            try
            {
                var client = _httpFactory.CreateClient("Supabase");

                // Converte o texto da lista (uma carta por linha) para o formato de storage (separado por "|").
                var listaFormatada = ConverterParaStorage(listaTexto);

                // Cria um objeto anônimo com os dados do deck.
                // Objetos anônimos são úteis para montar payloads JSON rapidamente.
                var payload = new { nome = nome.Trim(), comandante = comandante?.Trim(), lista = listaFormatada };

                // Serializa o objeto para JSON e empacota no corpo da requisição HTTP.
                // Encoding.UTF8 garante que caracteres especiais sejam tratados corretamente.
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                // Envia POST para o Supabase, que insere a linha na tabela "mtg_decks".
                await client.PostAsync("/rest/v1/mtg_decks", content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MTG] Erro ao criar deck: {ex.Message}");
            }

            // Redireciona o usuário de volta para a Index após salvar.
            // RedirectToAction evita que o form seja reenviado ao atualizar a página (padrão PRG).
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

                // Monta apenas o campo que será atualizado.
                var payload = new { lista = listaFormatada };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                // PATCH atualiza apenas os campos enviados (diferente do PUT que substitui tudo).
                // "?id=eq.{deckId}" é a sintaxe do Supabase para filtrar por id (equivale a WHERE id = deckId).
                await client.PatchAsync($"/rest/v1/mtg_decks?id=eq.{deckId}", content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MTG] Erro ao salvar lista: {ex.Message}");
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

                // DELETE com filtro pelo id.
                // Novamente usa a sintaxe de query do Supabase REST API.
                await client.DeleteAsync($"/rest/v1/mtg_decks?id=eq.{deckId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MTG] Erro ao excluir: {ex.Message}");
            }

            return RedirectToAction("Index");
        }

        // ──────────────────────────────────────────────────────────────────
        // 5. PARSEAR LISTA — Converte a string do banco para objetos MtgCarta
        // ──────────────────────────────────────────────────────────────────
        // Método privado: só é usado internamente por este controller.
        // Recebe: "4 Lightning Bolt|2 Counterspell|1 Black Lotus"
        // Retorna: lista de MtgCarta com Quantidade e Nome separados.
        private List<MtgCarta> ParsearLista(string lista)
        {
            var cartas = new List<MtgCarta>();

            // Divide a string pelo separador "|", resultando em cada entrada de carta.
            foreach (var entry in lista.Split('|'))
            {
                var l = entry.Trim(); // Remove espaços nas pontas
                if (string.IsNullOrEmpty(l)) continue; // Ignora entradas vazias

                // Divide pelo primeiro espaço em no máximo 2 partes:
                // Ex: "4 Lightning Bolt" → ["4", "Lightning Bolt"]
                var partes = l.Split(' ', 2);

                // Verifica se tem 2 partes e se a primeira é um número válido.
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

        // ─────────────────────────────────────────────────────────────────────
        // 6. CONVERTER PARA STORAGE — Converte texto do usuário para formato "|"
        // ─────────────────────────────────────────────────────────────────────
        // Recebe o texto do textarea (uma carta por linha):
        //   "4 Lightning Bolt
        //    2 Counterspell
        //    // Terrenos"   ← linhas com "//" são comentários e serão ignoradas
        //
        // Retorna: "4 Lightning Bolt|2 Counterspell"
        private string ConverterParaStorage(string? listaTexto)
        {
            if (string.IsNullOrWhiteSpace(listaTexto)) return ""; // Proteção contra entrada nula

            var entradas = listaTexto
                .Split('\n')                              // Quebra o texto linha por linha
                .Select(l => l.Trim())                    // Remove espaços/\r de cada linha
                .Where(l => !string.IsNullOrEmpty(l)      // Filtra linhas vazias...
                         && !l.StartsWith("//"));         // ...e comentários estilo "//"

            // Junta todas as linhas válidas com "|" como separador para salvar no banco.
            return string.Join("|", entradas);
        }
    }
}