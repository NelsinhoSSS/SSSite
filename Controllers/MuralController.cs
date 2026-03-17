using Microsoft.AspNetCore.Mvc;
using SSSite.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json; // Importante para lidar com nomes de letras minúsculas

namespace SSSite.Controllers
{
    public class MuralController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl = "https://kwhejdwazofellckarpt.supabase.co";

        public MuralController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                //configuração para aceitar nomes de campos "bagunçados" (Maiúsculo ou Minúsculo)
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                //requisição para o servidor
                var resposta = await _httpClient.GetAsync(_apiUrl);

                if (resposta.IsSuccessStatusCode)
                {
                    // Converte o JSON do servidor para a nossa lista usando as opções acima
                    var mensagens = await resposta.Content.ReadFromJsonAsync<List<MuralMensagem>>(options);

                    // Retorna a lista para a View (se for nula, manda uma lista vazia)
                    return View(mensagens?.OrderByDescending(m => m.Data).ToList() ?? new List<MuralMensagem>());
                }

                // Se o servidor responder erro (ex: 404 ou 500)
                return View(new List<MuralMensagem>());
            }
            catch (Exception ex)
            {
                // Se houver erro de conexão total
                return View(new List<MuralMensagem>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> Postar(string autor, string conteudo)
        {
            if (!string.IsNullOrWhiteSpace(conteudo))
            {
                string[] cores = { "#00ff41", "#00d4ff", "#ff00ff", "#ffff00", "#ff4d4d", "#9d00ff", "#ff8c00" };

                var novaMsg = new MuralMensagem
                {
                    Conteudo = conteudo.Length > 200 ? conteudo.Substring(0, 200) : conteudo,
                    Autor = string.IsNullOrWhiteSpace(autor) ? "Anônimo" : autor,
                    Data = DateTime.Now,
                    CorNeon = cores[new Random().Next(cores.Length)]
                };

                try
                {
                    await _httpClient.PostAsJsonAsync(_apiUrl, novaMsg);
                }
                catch { /* Silencia erro no post */ }
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
            catch { /* Silencia erro no delete */ }

            return RedirectToAction("Index");
        }
    }
}