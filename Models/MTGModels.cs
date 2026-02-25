public class Deck
{
    public int Id { get; set; } // O banco usará isso para identificar o deck
    public string Nome { get; set; }
    public List<Carta> Cartas { get; set; } = new List<Carta>();
}

public class Carta
{
    public int Id { get; set; } // O banco usará isso para identificar a carta
    public string Quantidade { get; set; }
    public string Nome { get; set; }
    public int DeckId { get; set; } // Elo de ligação com o Deck
}