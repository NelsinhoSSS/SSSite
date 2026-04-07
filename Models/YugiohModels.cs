using System.Text.Json.Serialization;

namespace SSSite.Models
{
    public class YuGiOhDeck
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("nome")]
        public string Nome { get; set; } = "";

        [JsonPropertyName("lista")]
        public string? Lista { get; set; }

        [JsonPropertyName("criado_em")]
        public DateTime CriadoEm { get; set; }

        public List<YuGiOhCarta> Cartas { get; set; } = new();
    }

    public class YuGiOhCarta
    {
        public int Quantidade { get; set; }
        public string Nome { get; set; } = "";
    }

    public class YuGiOhViewModel
    {
        public List<YuGiOhDeck> Decks { get; set; } = new();
    }
}