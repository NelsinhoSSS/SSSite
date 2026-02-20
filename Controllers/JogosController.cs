using Microsoft.AspNetCore.Mvc;

namespace SSSite.Controllers // Ajuste o nome conforme o seu projeto
{
    public class JogosController : Controller
    {

        // Action para o Galaxy Defender
        public IActionResult GalaxyDefender()
        {
            return View();
        }

        // Action para o Shuffle Cups
        public IActionResult ShuffleCups()
        {
            return View();
        }
    }
}