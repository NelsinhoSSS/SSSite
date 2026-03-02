using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SSSite.Models;
using SSSite.Data;
using System.Collections.Generic;
using System.Linq;

namespace SSSite.Controllers
{
    public class MTGController : Controller
    {
        // Contexto do banco de dados para operações CRUD
        private readonly AppDbContext _context;

        public MTGController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult MTG()
        {
            // --- LÓGICA DE LEITURA ---
            // Inclui a lista de 'Cartas' em cada 'Deck' para carregar tudo de uma vez
            var decks = _context.Decks.Include(d => d.Cartas).ToList();
            return View("~/Views/DeckList/MTG.cshtml", decks);
        }

        [HttpPost]
        public IActionResult SalvarDeck(int? id, string nome, string cartasRaw)
        {
            // Validação básica: não salva se faltar nome ou conteúdo
            if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(cartasRaw))
                return RedirectToAction("MTG");

            // --- LÓGICA DE SOMAR DUPLICADAS (PARSING) ---
            // Usamos um dicionário: a Chave é o Nome da Carta e o Valor é a Quantidade acumulada
            var contador = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // Divide o texto cru por linhas
            var linhas = cartasRaw.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var linha in linhas)
            {
                // Tenta separar "4 Lightning Bolt" em [4] e [Lightning Bolt]
                var partes = linha.Trim().Split(' ', 2);

                if (partes.Length == 2 && int.TryParse(partes[0], out int qtd))
                {
                    string nomeCarta = partes[1].Trim();

                    // Agrupa as cartas pelo nome, somando as quantidades
                    if (contador.ContainsKey(nomeCarta))
                        contador[nomeCarta] += qtd;
                    else
                        contador[nomeCarta] = qtd;
                }
            }

            // Transformamos o dicionário de volta em objetos do tipo "Carta" para o banco
            var listaDeCartasProcessada = contador.Select(x => new Carta
            {
                Nome = x.Key,
                Quantidade = x.Value.ToString() // O modelo espera string para quantidade
            }).OrderBy(x => x.Nome).ToList();


            // --- SALVAR NO BANCO DE DADOS (CRIAÇÃO OU EDIÇÃO) ---
            if (id.HasValue && id > 0)
            {
                // MODO EDIÇÃO: Busca o deck e suas cartas atuais no banco
                var deckExistente = _context.Decks.Include(d => d.Cartas)
                                                .FirstOrDefault(d => d.Id == id);

                if (deckExistente != null)
                {
                    deckExistente.Nome = nome.Trim();

                    // EF Core: Remove as cartas antigas relacionadas ao deck
                    _context.Cartas.RemoveRange(deckExistente.Cartas);

                    // EF Core: Adiciona a nova lista processada (com somas atualizadas)
                    deckExistente.Cartas = listaDeCartasProcessada;

                    _context.Update(deckExistente);
                }
            }
            else
            {
                // MODO CRIAÇÃO: Cria um novo objeto Deck e vincula as cartas
                var novoDeck = new Deck
                {
                    Nome = nome.Trim(),
                    Cartas = listaDeCartasProcessada
                };
                _context.Decks.Add(novoDeck);
            }

            // --- EFETIVAÇÃO ---
            // Salva todas as alterações (inserções/remoções) no arquivo .db
            _context.SaveChanges();

            return RedirectToAction("MTG");
        }

        [HttpPost]
        public IActionResult ExcluirDeck(int id)
        {
            // Busca o deck e o remove do rastreamento do EF
            var deck = _context.Decks.FirstOrDefault(d => d.Id == id);
            if (deck != null)
            {
                _context.Decks.Remove(deck);
                // Salva a exclusão no banco
                _context.SaveChanges();
            }
            return RedirectToAction("MTG");
        }

        [HttpGet]
        public IActionResult ObterDadosEdicao(int id)
        {
            // Busca os dados do deck para preencher o formulário de edição
            var deck = _context.Decks.Include(d => d.Cartas).FirstOrDefault(d => d.Id == id);
            if (deck == null) return NotFound();

            // Reconverte a lista de objetos Carta para o formato de texto cru (Quantidade Nome)
            var cartasTexto = string.Join("\n", deck.Cartas.Select(c => $"{c.Quantidade} {c.Nome}"));

            // Retorna um JSON para o frontend processar
            return Json(new { id = deck.Id, nome = deck.Nome, cartas = cartasTexto });
        }
    }
}