using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace SSSite.Controllers
{
    public class AdmController : Controller
    {
        [HttpPost]
        public IActionResult Validar(string senha)
        {
            if (senha == "newgods") // Mude sua senha aqui
            {
                HttpContext.Session.SetString("ModoAdm", "Ativo");
            }
            // Volta para a página anterior (MTG ou Home)
            return Redirect(Request.Headers["Referer"].ToString() ?? "/");
        }
    }
}