using System.Text.Json.Serialization;

namespace SSSite.Models
{
    public class ContagemNumero
    {
        public int Numero { get; set; }
        public int TotalSorteios { get; set; }
        public DateTime? UltimoSorteio { get; set; }
    }

    public class UltimoSorteio
    {
        [JsonPropertyName("numero")]
        public int Numero { get; set; }

        [JsonPropertyName("sorteado_em")]
        public DateTime SorteadoEm { get; set; }
    }

    public class Top10ViewModel
    {
        public List<ContagemNumero> Contagens { get; set; } = new();
        public UltimoSorteio? Ultimo { get; set; }
    }
}