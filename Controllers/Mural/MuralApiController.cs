using Microsoft.AspNetCore.Mvc;
using SSSite.Models;
using Supabase;

namespace SSSite.Controllers
{
    [Route("api/mural")]
    [ApiController]
    public class MuralApiController : ControllerBase
    {
        private readonly Client _supabase;

        public MuralApiController(Client supabase)
        {
            _supabase = supabase;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var resultado = await _supabase.From<MuralMensagem>().Get();
            return Ok(resultado.Models);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] MuralMensagem msg)
        {
            await _supabase.From<MuralMensagem>().Insert(msg);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _supabase.From<MuralMensagem>().Where(x => x.Id == id).Delete();
            return Ok();
        }
    }
}