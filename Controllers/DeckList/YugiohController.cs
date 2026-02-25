using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SSSite.Models;
using SSSite.Data;

namespace SSSite.Controllers
{
    public class YugiohController : Controller
    {
        private readonly AppDbContext _context;
        public YugiohController(AppDbContext context) { _context = context; }

        public IActionResult Yugioh()
        {
            var decks = _context.YugiohDecks.Include(d => d.Cartas).ToList();
            return View("~/Views/DeckList/Yugioh.cshtml", decks);
        }

        [HttpPost]
        public IActionResult SalvarDeck(int? id, string nome, string cartasRaw)
        {
            if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(cartasRaw)) return RedirectToAction("Yugioh");

            var contador = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var linha in cartasRaw.Split('\n'))
            {
                var partes = linha.Trim().Split(' ', 2);
                if (partes.Length == 2 && int.TryParse(partes[0], out int qtd))
                {
                    string nomeC = partes[1].Trim();
                    if (contador.ContainsKey(nomeC)) contador[nomeC] += qtd;
                    else contador[nomeC] = qtd;
                }
            }
            var novasCartas = contador.Select(x => new YugiohCarta { Nome = x.Key, Quantidade = x.Value.ToString() }).ToList();

            if (id.HasValue && id > 0)
            {
                var deck = _context.YugiohDecks.Include(d => d.Cartas).FirstOrDefault(d => d.Id == id);
                if (deck != null)
                {
                    deck.Nome = nome.Trim();
                    _context.YugiohCartas.RemoveRange(deck.Cartas);
                    deck.Cartas = novasCartas;
                }
            }
            else
            {
                _context.YugiohDecks.Add(new YugiohDeck { Nome = nome.Trim(), Cartas = novasCartas });
            }
            _context.SaveChanges();
            return RedirectToAction("Yugioh");
        }

        [HttpPost]
        public IActionResult ExcluirDeck(int id)
        {
            var d = _context.YugiohDecks.Find(id);
            if (d != null) { _context.YugiohDecks.Remove(d); _context.SaveChanges(); }
            return RedirectToAction("Yugioh");
        }

        [HttpGet]
        public IActionResult ObterDadosEdicao(int id)
        {
            var d = _context.YugiohDecks.Include(c => c.Cartas).FirstOrDefault(x => x.Id == id);
            return Json(new { id = d.Id, nome = d.Nome, cartas = string.Join("\n", d.Cartas.Select(c => $"{c.Quantidade} {c.Nome}")) });
        }
    }
}