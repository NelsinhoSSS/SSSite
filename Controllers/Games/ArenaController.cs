using Microsoft.AspNetCore.Mvc;

namespace SSSite.Controllers.Games
{
    // Localizado em Controllers/Games/
    public class ArenaController : Controller
    {
        public IActionResult Arena()
        {
            ViewData["Title"] = "ARENA";

            // Como a View não segue o padrão (Views/Arena/Index.cshtml), 
            // informamos o caminho exato dentro da pasta Jogos.
            return View("~/Views/Jogos/Arena.cshtml");
        }

        // Exemplo de como você pode receber ações do jogo futuramente
        [HttpPost]
        public IActionResult Acao(string tipo)
        {
            // Lógica de sorteio de dano ou defesa
            return Json(new { status = "sucesso", acao = tipo });
        }
    }
}