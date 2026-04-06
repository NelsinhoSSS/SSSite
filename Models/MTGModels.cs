using System.Text.Json.Serialization;

namespace SSSite.Models
{
    public class MtgDeck
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("nome")]
        public string Nome { get; set; } = "";

        [JsonPropertyName("comandante")]
        public string? Comandante { get; set; }

        [JsonPropertyName("lista")]
        public string? Lista { get; set; }

        [JsonPropertyName("criado_em")]
        public DateTime CriadoEm { get; set; }

        public List<MtgCarta> Cartas { get; set; } = new();
    }

    public class MtgCarta
    {
        public int Quantidade { get; set; }
        public string Nome { get; set; } = "";
    }

    public class MtgViewModel
    {
        public List<MtgDeck> Decks { get; set; } = new();
    }
}