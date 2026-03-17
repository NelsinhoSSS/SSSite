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

        // URL oficial do seu servidor no Render que faz a ponte com o Supabase
        private readonly string _apiUrl = "https://sssitesss.onrender.com/api/mural";

        public MuralController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        // 1. CARREGAR MENSAGENS
        public async Task<IActionResult> Index()
        {
            try
            {
                // Configuração para aceitar o JSON do servidor sem erro de maiúsculas/minúsculas
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                // O site tenta buscar a lista de mensagens no Render
                var mensagens = await _httpClient.GetFromJsonAsync<List<MuralMensagem>>(_apiUrl, options);

                // Retorna a lista para a View (se vier nulo, manda uma lista vazia)
                return View(mensagens?.OrderByDescending(m => m.Data).ToList() ?? new List<MuralMensagem>());
            }
            catch (Exception ex)
            {
                // Log de erro no console do Visual Studio para debug
                Console.WriteLine($"Erro ao conectar com a API no Render: {ex.Message}");
                return View(new List<MuralMensagem>());
            }
        }

        // 2. CRIAR MENSAGEM
        [HttpPost]
        public async Task<IActionResult> Postar(string autor, string conteudo)
        {
            if (!string.IsNullOrWhiteSpace(conteudo))
            {
                // Cores neon aleatórias para o estilo Cyberpunk
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
                    // Envia os dados para a API salvar no Supabase
                    await _httpClient.PostAsJsonAsync(_apiUrl, novaMsg);
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
                // Avisa o Render para deletar no banco de dados
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