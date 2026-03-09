namespace SSSite.Models
{
    public class PersonagemArena
    {
        // --- ATRIBUTOS DE IDENTIDADE ---
        public string Nome { get; set; } // Chave usada no JavaScript para definir o comportamento das skills (Z e X)
        public string Cor { get; set; } // Define a cor da "aura" (glow) e do rastro do player
        public string Icone { get; set; } // Emoji/Símbolo exibido nos cards de seleção

        // --- ATRIBUTOS DE COMBATE ---
        public int Vida { get; set; } // HP inicial (O Guerreiro tem o dobro do Mago, por exemplo)
        public float Velocidade { get; set; } // Multiplicador de movimento por frame
        public int DanoZ { get; set; } // Valor base de dano da habilidade primária

        // --- ATRIBUTOS DE TEMPO (EQUILÍBRIO) ---
        // Usamos double/float aqui porque o JS converte isso para milissegundos no front-end
        public double CooldownZ { get; set; } // Tempo de recarga do ataque básico
        public double CooldownX { get; set; } // Tempo de recarga da habilidade especial (Dash, Teleporte, Escudo)

        /// <summary>
        /// O "Catálogo de Heróis". Aqui você define o balanceamento do jogo.
        /// </summary>
        public static List<PersonagemArena> ObterClasses() => new List<PersonagemArena> {
            
            // O TANQUE: Muita vida, ataque lento em área (cone) e escudo defensivo.
            new PersonagemArena { Nome = "Guerreiro", Cor = "#FF2400", Icone = "⚔️", Vida = 200, Velocidade = 4.2f, DanoZ = 60, CooldownZ = 0.6, CooldownX = 1.5 },
            
            // O ATIRADOR: Rápido e frágil. Dano baixo por flecha, mas o CooldownZ (0.2) permite uma "metralhadora".
            new PersonagemArena { Nome = "Arqueiro", Cor = "#00FF00", Icone = "🏹", Vida = 120, Velocidade = 6.0f, DanoZ = 15, CooldownZ = 0.2, CooldownX = 1.2 },
            
            // O CANHÃO DE VIDRO: Vida mínima, mas o maior dano do jogo (Fireball). Teleporte (X) é vital para fugir.
            new PersonagemArena { Nome = "Mago", Cor = "#4169E1", Icone = "🧙", Vida = 100, Velocidade = 3.5f, DanoZ = 120, CooldownZ = 1.5, CooldownX = 4 },
            
            // O ESTRATEGISTA: Velocidade equilibrada e habilidade especial (Frenzy) que dobra sua velocidade.
            new PersonagemArena { Nome = "Assassino", Cor = "#9D00FF", Icone = "🎭", Vida = 110, Velocidade = 5.5f, DanoZ = 20, CooldownZ = 0.4, CooldownX = 6 }
        };
    }
}