using Microsoft.AspNetCore.Mvc;
using SSSite.Models;
using Supabase;

namespace SSSite.Controllers
{
    [Route("api/Mural")]
    [ApiController]
    public class MuralApiController : ControllerBase
    {
        private readonly Supabase.Client _supabase;

        public MuralApiController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        [HttpGet]
        public async Task<IActionResult> GetMensagens()
        {
            try
            {
                // Busca da tabela MuralMensagem usando o Model com letras minúsculas
                var response = await _supabase.From<MuralMensagem>().Get();

                // Retorna a lista de mensagens. Se estiver vazio, retorna lista limpa [].
                var mensagens = response.Models ?? new List<MuralMensagem>();

                return Ok(mensagens);
            }
            catch (Exception ex)
            {
                // Log de erro básico para o console do Render
                Console.WriteLine($"Erro na API: {ex.Message}");
                return StatusCode(500, "Erro ao acessar o banco de dados.");
            }
        }
    }
}