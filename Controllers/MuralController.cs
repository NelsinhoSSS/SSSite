using Microsoft.AspNetCore.Mvc;
using SSSite.Data;
using SSSite.Models;
using System;
using System.Linq;

namespace SSSite.Controllers
{
    public class MuralController : Controller
    {
        // O Contexto é a ponte entre o código C# e o Banco de Dados (SQLite/SQL Server)
        private readonly AppDbContext _context;

        // Construtor: Injeta o banco de dados para ser usado no Controller
        public MuralController(AppDbContext context)
        {
            _context = context;
        }

        // Action principal: Carrega a lista de mensagens para exibir na tela
        public IActionResult Index()
        {
            // OrderByDescending garante que a mensagem mais nova apareça primeiro no topo
            var mensagens = _context.Mural.OrderByDescending(m => m.Data).ToList();
            return View(mensagens);
        }

        [HttpPost] // Atributo de segurança que indica que este método recebe dados (POST)
        public IActionResult Postar(string autor, string conteudo)
        {
            // VALIDAÇÃO DE CAPACIDADE: 
            // .Count() vai ao banco e conta os registros. Se houver 20, bloqueia novos envios.
            if (_context.Mural.Count() >= 20)
            {
                // TempData guarda uma mensagem temporária que pode ser exibida na View (alerta de erro)
                TempData["Erro"] = "O mural está cheio!";
                return RedirectToAction("Index");
            }

            // Verifica se a mensagem não está em branco ou só com espaços
            if (!string.IsNullOrWhiteSpace(conteudo))
            {
                // Pool de cores neon para dar o estilo Cyberpunk aleatório aos post-its
                string[] cores = { "#00ff41", "#00d4ff", "#ff00ff", "#ffff00", "#ff4d4d", "#9d00ff", "#ff8c00" };

                var novaMsg = new MuralMensagem
                {
                    // SEGURANÇA NO SERVIDOR:
                    // Mesmo que o JavaScript falhe ou seja burlado, o Substring garante 
                    // que o texto salvo no banco nunca passe de 200 caracteres.
                    Conteudo = conteudo.Length > 200 ? conteudo.Substring(0, 200) : conteudo,

                    // Operador ternário: se o autor for vazio, salva como "Anônimo"
                    Autor = string.IsNullOrWhiteSpace(autor) ? "Anônimo" : autor,

                    Data = DateTime.Now,

                    // Sorteia um índice aleatório dentro do array de cores
                    CorNeon = cores[new Random().Next(cores.Length)]
                };

                // Prepara a inclusão do objeto no rastreamento do banco
                _context.Mural.Add(novaMsg);

                // COMMIT: Salva efetivamente as mudanças no arquivo de banco de dados (.db)
                _context.SaveChanges();
            }

            // Redireciona para a Index para "limpar" o formulário e mostrar a nova mensagem
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Excluir(int id)
        {
            // .Find(id) busca diretamente pela Chave Primária, é a forma mais rápida de busca
            var msg = _context.Mural.Find(id);

            if (msg != null)
            {
                _context.Mural.Remove(msg);
                _context.SaveChanges(); // Confirma a exclusão no banco
            }

            return RedirectToAction("Index");
        }
    }
}