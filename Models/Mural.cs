using Postgrest.Attributes;
using Postgrest.Models;

namespace SSSite.Models
{
    [Table("MuralMensagem")] // Força o nome exato da tabela no Supabase
    public class MuralMensagem : BaseModel
    {
        [PrimaryKey("Id", false)] // O false indica que o banco gera o ID (SERIAL)
        public int Id { get; set; }

        [Column("Conteudo")]
        public string Conteudo { get; set; }

        [Column("Autor")]
        public string Autor { get; set; }

        [Column("Data")]
        public DateTime Data { get; set; }

        [Column("CorNeon")]
        public string CorNeon { get; set; }
    }
}