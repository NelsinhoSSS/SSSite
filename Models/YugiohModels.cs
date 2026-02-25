namespace SSSite.Models
{
    public class YugiohDeck
    {
        public int Id { get; set; }
        public string Nome { get; set; } = "";
        public List<YugiohCarta> Cartas { get; set; } = new List<YugiohCarta>();
    }

    public class YugiohCarta
    {
        public int Id { get; set; }
        public string Nome { get; set; } = "";
        public string Quantidade { get; set; } = "";
        public int YugiohDeckId { get; set; } // Chave estrangeira
    }
}