using System;
using System.Collections.Generic;

namespace SSSite.Models
{
    public class InimigoArena
    {
        // --- PROPRIEDADES BÁSICAS (DNA Visual e Físico) ---
        public string Nome { get; set; }
        public int VidaMax { get; set; }
        public int Raio { get; set; } // Define o tamanho no Canvas e a área de colisão
        public string Cor { get; set; } // Código Hexadecimal enviado diretamente para o ctx.fillStyle
        public string IATipo { get; set; } // O "Modo de Voo": BOUNCE, DASH, TRACKER ou FINAL_BOSS
        public string Forma { get; set; } // R = Retângulo, C = Círculo, T = Triângulo, H = Círculo com Borda (Hollow)

        // --- MECÂNICAS DE COMPORTAMENTO ---
        public bool Multiplo { get; set; } // Define se o inimigo se divide em menores ao morrer (Void Splitter)
        public int Quantidade { get; set; } // Quantos desse tipo nascem ao iniciar a fase
        public float DanoContato { get; set; } // Dano por frame/segundo se encostar no player
        public float Velocidade { get; set; } // O "passo" do inimigo a cada frame

        // --- MÓDULO DE LÂMINAS (Blade Master) ---
        public bool TemEspadas { get; set; }
        public int QtdEspadas { get; set; }
        public int RaioOrbita { get; set; } // Distância das espadas em relação ao centro do boss
        public float VelRotacao { get; set; } // Velocidade angular (radianos por frame)

        // --- MÓDULO DE TIRO (Plasma Core / Final Boss) ---
        public bool DisparaRaios { get; set; }
        public float IntervaloTiroSeg { get; set; } // Delay entre ataques
        public int DanoRaio { get; set; }
        public float VelRaio { get; set; }

        // --- MÓDULO DE HABILIDADES ESPECIAIS ---
        public float TempoDashSeg { get; set; } // Tempo de recarga para o avanço rápido (Phantom Striker)
        public float TempoBlackHoleSeg { get; set; } // Exclusivo para o Final Boss

        /// <summary>
        /// Este método estático é a sua "Playlist de Combate". 
        /// Cada item da lista é uma fase/nível diferente da arena.
        /// </summary>
        public static List<InimigoArena> CriarCampanha()
        {
            return new List<InimigoArena>
            {
                // FASE 1: O tutorial. IA simples de ricochete.
                new InimigoArena { Nome = "SENTINEL UNIT", VidaMax = 150, Raio = 25, Cor = "#555", IATipo = "BOUNCE", Forma = "R", Quantidade = 1, DanoContato = 30f, Velocidade = 3.5f },

                // FASE 2: Introdução a projéteis.
                new InimigoArena { Nome = "PLASMA CORE", VidaMax = 400, Raio = 35, Cor = "#e67e22", IATipo = "BOUNCE", Forma = "C", Quantidade = 1, DisparaRaios = true, IntervaloTiroSeg = 0.8f, VelRaio = 4.5f, DanoRaio = 15, DanoContato = 40f, Velocidade = 2.8f },

                // FASE 3: O teste de esquiva circular (Bullet Hell leve).
                new InimigoArena { Nome = "BLADE MASTER", VidaMax = 700, Raio = 40, Cor = "#9b59b6", IATipo = "BOUNCE", Forma = "C", Quantidade = 1, TemEspadas = true, QtdEspadas = 4, RaioOrbita = 110, VelRotacao = 0.04f, DanoContato = 50f, Velocidade = 3.0f },

                // FASE 4: Caos. Velocidade alta e vários alvos.
                new InimigoArena { Nome = "TRIANGLE FRENZY", VidaMax = 500, Raio = 45, Cor = "#f1c40f", IATipo = "BOUNCE", Forma = "T", Quantidade = 3, DanoContato = 40f, Velocidade = 7.5f },

                // FASE 5: Mecânica de divisão. Ao morrer, ele spawna cópias menores (lógica no JS).
                new InimigoArena { Nome = "THE VOID SPLITTER", VidaMax = 1200, Raio = 60, Cor = "#ffffff", IATipo = "BOUNCE", Forma = "H", Multiplo = true, Quantidade = 1, DanoContato = 60f, Velocidade = 2.0f },

                // FASE 6: Predador. Ele para e dá um dash na direção do player.
                new InimigoArena { Nome = "PHANTOM STRIKER", VidaMax = 2000, Raio = 35, Cor = "#00ffff", IATipo = "DASH", Forma = "C", Quantidade = 1, TempoDashSeg = 1.0f, DanoContato = 80f, Velocidade = 1.5f },

                // FASE 7: O ápice. Combina projéteis rápidos com a mecânica de Buraco Negro.
                new InimigoArena { Nome = "THE SINGULARITY", VidaMax = 5500, Raio = 70, Cor = "#6200ea", IATipo = "FINAL_BOSS", Forma = "C", Quantidade = 1, DisparaRaios = true, IntervaloTiroSeg = 0.15f, VelRaio = 5.5f, DanoRaio = 10, TempoBlackHoleSeg = 7.0f, DanoContato = 100f, Velocidade = 1.0f }
            };
        }
    }
}