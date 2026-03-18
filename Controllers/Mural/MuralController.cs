using Microsoft.AspNetCore.Mvc;
using SSSite.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json;

namespace SSSite.Controllers.Mural
{
    public class MuralController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl = "https://sssitesss.onrender.com/api/mural";

        public MuralController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // O segredo para o 'Total: 0' virar 'Total: X'
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                // Busca os dados lá no Render
                var mensagens = await _httpClient.GetFromJsonAsync<List<MuralMensagem>>(_apiUrl, options);

                if (mensagens == null || mensagens.Count == 0)
                {
                    // Se cair aqui, a API respondeu mas não veio nada
                    return View(new List<MuralMensagem>());
                }

                return View(mensagens.OrderByDescending(m => m.Data).ToList());
            }
            catch (Exception ex)
            {
                // Se cair aqui, o site nem conseguiu falar com o Render
                Console.WriteLine($"Erro de conexão: {ex.Message}");
                return View(new List<MuralMensagem>());
            }
        }
        [HttpPost]
        public async Task<IActionResult> Postar(string autor, string conteudo)
        {
            if (!string.IsNullOrWhiteSpace(conteudo))
            {
                string[] cores = { "#00ff41", "#00d4ff", "#ff00ff", "#ffff00", "#ff4d4d", "#9d00ff" };

                var novaMsg = new MuralMensagem
                {
                    Conteudo = conteudo.Length > 200 ? conteudo.Substring(0, 200) : conteudo,
                    Autor = string.IsNullOrWhiteSpace(autor) ? "Anônimo" : autor,
                    Data = DateTime.Now,
                    CorNeon = cores[new Random().Next(cores.Length)]
                };

                try
                {
                    // Criamos um objeto limpo apenas com o que o banco precisa
                    var dadosLimpos = new
                    {
                        Conteudo = novaMsg.Conteudo,
                        Autor = novaMsg.Autor,
                        Data = novaMsg.Data,
                        CorNeon = novaMsg.CorNeon
                    };

                    // Enviamos esse objeto limpo para a API
                    await _httpClient.PostAsJsonAsync(_apiUrl, dadosLimpos);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao postar: {ex.Message}");
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Excluir(int id)
        {
            try
            {
                await _httpClient.DeleteAsync($"{_apiUrl}/{id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao excluir: {ex.Message}");
            }
            return RedirectToAction("Index");
        }
    }
}