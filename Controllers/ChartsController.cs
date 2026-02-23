using Microsoft.AspNetCore.Mvc;

namespace SeuProjeto.Controllers
{
    public class ChartsController : Controller
    {
        public IActionResult Pizza()
        {
            return View();
        }

        public IActionResult Column()
        {
            return View();
        }
    }
}