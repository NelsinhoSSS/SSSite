using Microsoft.AspNetCore.Mvc;

namespace SSSite.Controllers
{
    public class CurriculoController : Controller
    {
        public IActionResult Index()
        {
            // Definindo o nome exato do seu arquivo
            string nomeArquivo = "Curriculo_Nelson_Abreu_Freitas.pdf";

            // Passando o caminho para a View (assumindo que está em wwwroot/curriculos/)
            ViewBag.CaminhoPdf = $"/curriculos/{nomeArquivo}";

            return View();
        }
    }
}