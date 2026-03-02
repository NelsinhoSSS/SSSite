using Microsoft.AspNetCore.Mvc;
using SSSite.Data;
using SSSite.Models;
using System;
using System.Linq;

namespace SSSite.Controllers
{
    public class MuralController : Controller
    {
        // Injeção do banco de dados
        private readonly AppDbContext _context;

        public MuralController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Define o caminho da imagem de fundo para a View
            ViewBag.BackgroundUrl = "/Imagens/QuadroPinos.jpg";

            // Busca as mensagens no banco, ordenando da mais recente para a mais antiga
            var mensagens = _context.Mural.OrderByDescending(m => m.Data).ToList();

            return View(mensagens);
        }

        [HttpPost] // Indica que este método recebe dados de um formulário
        public IActionResult Postar(string autor, string conteudo)
        {
            //Verifica se o mural já atingiu o limite máximo de 20 mensagens
            if (_context.Mural.Count() >= 20)
            {
                TempData["Erro"] = "O mural está cheio!";
                return RedirectToAction("Index");
            }

            // Ve se o conteúdo da mensagem não está vazio
            if (!string.IsNullOrWhiteSpace(conteudo))
            {
                // Array de cores para garantir o visual aleatório de cada post
                string[] cores = { "#00ff41", "#00d4ff", "#ff00ff", "#ffff00", "#ff4d4d", "#9d00ff" };

                var novaMsg = new MuralMensagem
                {
                    // Garante que o texto tenha no máximo 200 caracteres (corta se for maior to com preguisa de mudar pra travar no max)
                    Conteudo = conteudo.Length > 200 ? conteudo.Substring(0, 200) : conteudo,

                    // Se o autor estiver vazio, define como "Anônimo"
                    Autor = string.IsNullOrWhiteSpace(autor) ? "Anônimo" : autor,

                    Data = DateTime.Now,

                    // Pega uma cor aleatoria do array para cada nova mensagem
                    CorNeon = cores[new Random().Next(cores.Length)]
                };

                // Adiciona o objeto ao rastreamento do Entity Framework
                _context.Mural.Add(novaMsg);

                //Salva as alterações no banco de dados
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Excluir(int id)
        {
            // Busca a mensagem específica pelo ID no banco
            var msg = _context.Mural.Find(id);

            if (msg != null)
            {
                // Remove a mensagem encontrada
                _context.Mural.Remove(msg);

                // Salva a remoção no banco de dados
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }
    }
}