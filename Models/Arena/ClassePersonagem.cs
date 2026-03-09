namespace SSSite.Models
{
    public class ClassePersonagem
    {
        public string Nome { get; set; } = "Guerreiro";
        public string Cor { get; set; } = "#ff0000"; // Vermelho
        public string Icone { get; set; } = "⚔️";
        public int Vida { get; set; } = 150;
        public int Velocidade { get; set; } = 6;
    }

    public class InimigoArena
    {
        public string Nome { get; set; } = "Glitch";
        public string Cor { get; set; } = "#808080"; // Cinza
        public int Raio { get; set; } = 40;          // Bola grande
        public int Dano { get; set; } = 20;
        public float VelocidadeInicial { get; set; } = 3.5f;
    }
}