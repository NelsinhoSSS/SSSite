using Microsoft.AspNetCore.Mvc;
using System;

namespace SeuProjeto.Controllers
{
    public class RandomController : Controller
    {
        // Action para escolher um animal
        public IActionResult Animais(string animalNome)
        {
            ViewBag.AnimalEscolhido = animalNome;
            return View();
        }



        // Action para sortear um número
        public IActionResult Numeros(bool sortear = false)
        {
            if (sortear)
            {
                Random rand = new Random();
                // Sorteia entre 1 e 100.000.000
                int resultado = rand.Next(1, 100000001);

                // .ToString() sem parâmetros remove os pontos de milhar
                ViewBag.NumeroSorteado = resultado.ToString();
            }
            else
            {
                ViewBag.NumeroSorteado = "--";
            }

            return View();
        }
    }
}