using Microsoft.AspNetCore.Mvc;
using SSSite.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

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
                // Ele vai buscar o JSON com 'conteudo', 'corneon', etc.
                var mensagens = await _httpClient.GetFromJsonAsync<List<MuralMensagem>>(_apiUrl);

                // Se a API retornar algo, ele manda pra View. Se não, manda lista vazia.
                return View(mensagens?.OrderByDescending(m => m.data).ToList() ?? new List<MuralMensagem>());
            }
            catch (Exception ex)
            {
                // Se der erro de conexão, você verá aqui no Debug
                System.Diagnostics.Debug.WriteLine("Erro ao conectar na API: " + ex.Message);
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
                    conteudo = conteudo.Length > 200 ? conteudo.Substring(0, 200) : conteudo,
                    autor = string.IsNullOrWhiteSpace(autor) ? "Anônimo" : autor,
                    data = DateTime.Now,
                    corneon = cores[new Random().Next(cores.Length)]
                };

                try
                {
                    // Criamos um objeto limpo apenas com o que o banco precisa
                    var dadosLimpos = new
                    {
                        Conteudo = novaMsg.conteudo,
                        Autor = novaMsg.autor,
                        Data = novaMsg.data,
                        CorNeon = novaMsg.corneon
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