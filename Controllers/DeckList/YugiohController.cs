using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SSSite.Models;
using SSSite.Data;

namespace SSSite.Controllers
{
    public class YugiohController : Controller
    {
        // Conexão com o banco de dados via Entity Framework
        private readonly AppDbContext _context;
        public YugiohController(AppDbContext context) { _context = context; }

        public IActionResult Yugioh()
        {
            // Busca todos os decks no banco, incluindo a lista de cartas relacionada (Include)
            var decks = _context.YugiohDecks.Include(d => d.Cartas).ToList();
            return View("~/Views/DeckList/Yugioh.cshtml", decks);
        }

        [HttpPost]
        public IActionResult SalvarDeck(int? id, string nome, string cartasRaw)
        {
            // Validação: não processa se os campos obrigatórios estiverem vazios
            if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(cartasRaw)) return RedirectToAction("Yugioh");

            // --- PROCESSAMENTO E AGRUPAMENTO DE CARTAS ---
            var contador = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // Divide o texto em linhas para processar cada carta
            foreach (var linha in cartasRaw.Split('\n'))
            {
                // Espera o formato: "Quantidade NomeDaCarta"
                var partes = linha.Trim().Split(' ', 2);
                if (partes.Length == 2 && int.TryParse(partes[0], out int qtd))
                {
                    string nomeC = partes[1].Trim();
                    // Soma a quantidade se a carta já estiver no dicionário
                    if (contador.ContainsKey(nomeC)) contador[nomeC] += qtd;
                    else contador[nomeC] = qtd;
                }
            }
            // Cria os objetos YugiohCarta baseados no agrupamento do dicionário
            var novasCartas = contador.Select(x => new YugiohCarta { Nome = x.Key, Quantidade = x.Value.ToString() }).ToList();

            // --- LÓGICA DE PERSISTÊNCIA (CREATE/UPDATE) ---
            if (id.HasValue && id > 0)
            {
                // MODO EDIÇÃO: Busca o deck existente incluindo as cartas atuais
                var deck = _context.YugiohDecks.Include(d => d.Cartas).FirstOrDefault(d => d.Id == id);
                if (deck != null)
                {
                    deck.Nome = nome.Trim();
                    // Remove as cartas antigas do banco antes de adicionar as novas
                    _context.YugiohCartas.RemoveRange(deck.Cartas);
                    // Atualiza a referência das cartas
                    deck.Cartas = novasCartas;
                }
            }
            else
            {
                // MODO CRIAÇÃO: Adiciona um novo registro de deck ao banco
                _context.YugiohDecks.Add(new YugiohDeck { Nome = nome.Trim(), Cartas = novasCartas });
            }

            // Salva todas as mudanças no banco de dados
            _context.SaveChanges();

            return RedirectToAction("Yugioh");
        }

        [HttpPost]
        public IActionResult ExcluirDeck(int id)
        {
            // Localiza e remove o deck pelo ID
            var d = _context.YugiohDecks.Find(id);
            if (d != null) { _context.YugiohDecks.Remove(d); _context.SaveChanges(); }

            return RedirectToAction("Yugioh");
        }

        [HttpGet]
        public IActionResult ObterDadosEdicao(int id)
        {
            // Busca os dados do deck para preencher o formulário de edição no frontend
            var d = _context.YugiohDecks.Include(c => c.Cartas).FirstOrDefault(x => x.Id == id);
            if (d == null) return NotFound();

            // Retorna os dados como JSON, transformando a lista de cartas em texto novamente
            return Json(new { id = d.Id, nome = d.Nome, cartas = string.Join("\n", d.Cartas.Select(c => $"{c.Quantidade} {c.Nome}")) });
        }
    }
}