using Microsoft.AspNetCore.Mvc;
using SSSite.Models;
using Supabase;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SSSite.Controllers.Mural
{
    public class MuralController : Controller
    {
        private readonly Supabase.Client _supabase;

        public MuralController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        // 1. LISTAR MENSAGENS
        public async Task<IActionResult> Index()
        {
            try
            {
                var response = await _supabase.From<MuralMensagem>().Get();
                var mensagens = response.Models;

                // Ordena pelas mais recentes
                return View(mensagens.OrderByDescending(m => m.data).ToList());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao carregar mural: " + ex.Message);
                return View(new List<MuralMensagem>());
            }
        }

        // 2. CRIAR MENSAGEM
        [HttpPost]
        public async Task<IActionResult> Postar(string autor, string conteudo)
        {
            if (!string.IsNullOrWhiteSpace(conteudo))
            {
                string[] cores = { "#00ff41", "#00d4ff", "#ff00ff", "#ffff00", "#ff4d4d", "#9d00ff" };

                var novaMsg = new MuralMensagem
                {
                    conteudo = conteudo.Length > 200 ? conteudo.Substring(0, 200) : conteudo,
                    autor = string.IsNullOrWhiteSpace(autor) ? "Anônimo" : autor,
                    data = DateTime.Now,
                    corneon = cores[new Random().Next(cores.Length)]
                };

                try
                {
                    await _supabase.From<MuralMensagem>().Insert(novaMsg);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao postar: {ex.Message}");
                }
            }
            return RedirectToAction("Index");
        }

        // 3. EXCLUIR MENSAGEM (A que faltava!)
        [HttpPost]
        public async Task<IActionResult> Excluir(int id)
        {
            try
            {
                // O Supabase usa LINQ para filtrar o que deve ser apagado
                await _supabase
                    .From<MuralMensagem>()
                    .Where(x => x.id == id)
                    .Delete();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao excluir post-it {id}: {ex.Message}");
            }

            return RedirectToAction("Index");
        }
    }
}