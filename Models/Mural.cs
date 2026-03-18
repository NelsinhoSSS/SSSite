using Postgrest.Attributes;
using Postgrest.Models;

namespace SSSite.Models
{
    [Table("MuralMensagem")]
    public class MuralMensagem : BaseModel
    {
        [PrimaryKey("Id", false)]
        public int Id { get; set; }

        [Column("Conteudo")] 
        public string Conteudo { get; set; } = "";

        [Column("Autor")]
        public string Autor { get; set; } = "Anônimo";

        [Column("Data")]
        public DateTime Data { get; set; }

        [Column("CorNeon")]
        public string CorNeon { get; set; } = "#00ff41";
    }
}