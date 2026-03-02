using Microsoft.EntityFrameworkCore;
using SSSite.Models;

namespace SSSite.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Deck> Decks { get; set; }
        public DbSet<Carta> Cartas { get; set; }

        public DbSet<YugiohDeck> YugiohDecks { get; set; }
        public DbSet<YugiohCarta> YugiohCartas { get; set; }

        public DbSet<MuralMensagem> Mural { get; set; }
    }
}