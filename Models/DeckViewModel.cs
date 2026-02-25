namespace SSSite.Models
{
    public class CartaNoDeck
    {
        public string Quantidade { get; set; }
        public string Nome { get; set; }
        public string Tipo { get; set; } // Criatura, Terreno, etc.
    }

    public class DeckModel
    {
        public string NomeDeck { get; set; }
        public List<CartaNoDeck> Cartas { get; set; } = new List<CartaNoDeck>();
    }
}