using Microsoft.EntityFrameworkCore;
using SSSite.Models;

namespace SSSite.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Se estas duas linhas não estiverem aqui, o comando não cria as tabelas!
        public DbSet<Deck> Decks { get; set; }
        public DbSet<Carta> Cartas { get; set; }

        public DbSet<YugiohDeck> YugiohDecks { get; set; }
        public DbSet<YugiohCarta> YugiohCartas { get; set; }
    }
}