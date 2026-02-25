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
        private readonly AppDbContext _context;

        public MTGController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult MTG()
        {
            // Carrega os decks do banco incluindo as listas de cartas
            var decks = _context.Decks.Include(d => d.Cartas).ToList();
            return View("~/Views/DeckList/MTG.cshtml", decks);
        }

        [HttpPost]
        public IActionResult SalvarDeck(int? id, string nome, string cartasRaw)
        {
            if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(cartasRaw))
                return RedirectToAction("MTG");

            // --- LÓGICA DE SOMAR DUPLICADAS ---
            // Usamos um dicionário: a Chave é o Nome da Carta e o Valor é a Quantidade
            var contador = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            var linhas = cartasRaw.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var linha in linhas)
            {
                // Tenta separar "4 Lightning Bolt" em [4] e [Lightning Bolt]
                var partes = linha.Trim().Split(' ', 2);

                if (partes.Length == 2 && int.TryParse(partes[0], out int qtd))
                {
                    string nomeCarta = partes[1].Trim();

                    if (contador.ContainsKey(nomeCarta))
                        contador[nomeCarta] += qtd; // Se já existe, soma a quantidade
                    else
                        contador[nomeCarta] = qtd; // Se é nova, adiciona ao dicionário
                }
            }

            // Transformamos o dicionário de volta em objetos do tipo "Carta"
            var listaDeCartasProcessada = contador.Select(x => new Carta
            {
                Nome = x.Key,
                Quantidade = x.Value.ToString()
            }).OrderBy(x => x.Nome).ToList();


            // --- SALVAR NO BANCO DE DADOS ---
            if (id.HasValue && id > 0)
            {
                // MODO EDIÇÃO: Busca o deck e suas cartas atuais
                var deckExistente = _context.Decks.Include(d => d.Cartas)
                                            .FirstOrDefault(d => d.Id == id);

                if (deckExistente != null)
                {
                    deckExistente.Nome = nome.Trim();

                    // 1. Remove as cartas antigas vinculadas a este deck
                    _context.Cartas.RemoveRange(deckExistente.Cartas);

                    // 2. Adiciona a nova lista processada (com somas e sem duplicatas)
                    deckExistente.Cartas = listaDeCartasProcessada;

                    _context.Update(deckExistente);
                }
            }
            else
            {
                // MODO CRIAÇÃO: Cria um novo objeto Deck
                var novoDeck = new Deck
                {
                    Nome = nome.Trim(),
                    Cartas = listaDeCartasProcessada
                };
                _context.Decks.Add(novoDeck);
            }

            // Salva todas as alterações no arquivo .db
            _context.SaveChanges();

            return RedirectToAction("MTG");
        }

        [HttpPost]
        public IActionResult ExcluirDeck(int id)
        {
            var deck = _context.Decks.FirstOrDefault(d => d.Id == id);
            if (deck != null)
            {
                _context.Decks.Remove(deck);
                _context.SaveChanges();
            }
            return RedirectToAction("MTG");
        }

        [HttpGet]
        public IActionResult ObterDadosEdicao(int id)
        {
            var deck = _context.Decks.Include(d => d.Cartas).FirstOrDefault(d => d.Id == id);
            if (deck == null) return NotFound();

            var cartasTexto = string.Join("\n", deck.Cartas.Select(c => $"{c.Quantidade} {c.Nome}"));

            return Json(new { id = deck.Id, nome = deck.Nome, cartas = cartasTexto });
        }
    }
}