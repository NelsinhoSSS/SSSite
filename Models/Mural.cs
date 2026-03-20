using Postgrest.Attributes;
using Postgrest.Models;

namespace SSSite.Models
{
    [Table("MuralMensagem")]
    public class MuralMensagem : BaseModel
    {
        public int id { get; set; }
        public string conteudo { get; set; } = "";
        public string autor { get; set; } = "";
        public DateTime data { get; set; } = DateTime.Now;
        public string corneon { get; set; } = "#00ff41";
    }
}