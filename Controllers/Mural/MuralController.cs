using Microsoft.AspNetCore.Mvc;
using SSSite.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SSSite.Controllers.Mural
{
    public class MuralController : Controller
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public MuralController(IHttpClientFactory httpFactory)
        {
            _httpFactory = httpFactory;
        }

        // 1. LISTAR MENSAGENS
        public async Task<IActionResult> Index()
        {
            try
            {
                var client = _httpFactory.CreateClient("Supabase");
                var res = await client.GetAsync("/rest/v1/MuralMensagem?select=*&order=data.desc");
                res.EnsureSuccessStatusCode();

                var body = await res.Content.ReadAsStringAsync();
                var mensagens = JsonSerializer.Deserialize<List<MuralMensagem>>(body, _jsonOpts) ?? new();
                return View(mensagens);
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

                var novaMsg = new
                {
                    conteudo = conteudo.Length > 200 ? conteudo[..200] : conteudo,
                    autor = string.IsNullOrWhiteSpace(autor) ? "Anônimo" : autor,
                    data = DateTime.UtcNow,
                    corneon = cores[System.Random.Shared.Next(cores.Length)]
                };

                try
                {
                    var client = _httpFactory.CreateClient("Supabase");
                    var json = JsonSerializer.Serialize(novaMsg);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var res = await client.PostAsync("/rest/v1/MuralMensagem", content);
                    res.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao postar: {ex.Message}");
                }
            }
            return RedirectToAction("Index");
        }

        // 3. EXCLUIR MENSAGEM
        [HttpPost]
        public async Task<IActionResult> Excluir(int id)
        {
            try
            {
                var client = _httpFactory.CreateClient("Supabase");
                var res = await client.DeleteAsync($"/rest/v1/MuralMensagem?id=eq.{id}");
                res.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao excluir post-it {id}: {ex.Message}");
            }
            return RedirectToAction("Index");
        }
    }
}