using Microsoft.AspNetCore.Mvc;
using SSSite.Models;
using Supabase;

namespace SSSite.Controllers
{
    [Route("api/mural")] // Define a URL como https://sssitesss.onrender.com/api/mural
    [ApiController]
    public class MuralApiController : ControllerBase
    {
        private readonly Client _supabase;

        public MuralApiController(Client supabase)
        {
            _supabase = supabase;
        }

        // GET: api/mural (Busca as mensagens)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MuralMensagem>>> GetMensagens()
        {
            try
            {
                // MUDANÇA AQUI: Busca da tabela MuralMensagem
                var resultado = await _supabase.From<MuralMensagem>().Get();
                return Ok(resultado.Models);
            }
            catch (Exception ex)
            {
                return BadRequest(new { erro = ex.Message });
            }
        }

        // POST: api/mural (Cria nova mensagem)
        [HttpPost]
        public async Task<IActionResult> PostMensagem([FromBody] MuralMensagem mensagem)
        {
            try
            {
                // Garante que a data seja gravada agora se vier vazia
                if (mensagem.Data == default) mensagem.Data = DateTime.UtcNow;

                await _supabase.From<MuralMensagem>().Insert(mensagem);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { erro = ex.Message });
            }
        }

        // DELETE: api/mural/{id} (Exclui mensagem)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMensagem(int id)
        {
            try
            {
                await _supabase.From<MuralMensagem>().Where(x => x.Id == id).Delete();
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { erro = ex.Message });
            }
        }
    }
}