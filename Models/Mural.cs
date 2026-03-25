using Postgrest.Attributes; // Importante: vem com o pacote do Supabase
using Postgrest.Models;

namespace SSSite.Models
{
    [Table("MuralMensagem")] // Nome exato da tabela no Supabase
    public class MuralMensagem : BaseModel
    {
        [PrimaryKey("id", false)] // O 'false' indica que o banco gera o ID sozinho
        public int id { get; set; }

        [Column("conteudo")]
        public string conteudo { get; set; }

        [Column("autor")]
        public string autor { get; set; }

        [Column("data")]
        public DateTime data { get; set; }

        [Column("corneon")]
        public string corneon { get; set; }
    }
}