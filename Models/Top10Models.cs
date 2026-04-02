
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace SSSite.Models
{
    public class ContagemNumero
    {
        public int Numero { get; set; }
        public int TotalSorteios { get; set; }
        public DateTime? UltimoSorteio { get; set; }
    }

    public class UltimoSorteio
    {
        public int Numero { get; set; }
        public DateTime SorteadoEm { get; set; }
    }

    public class Top10ViewModel
    {
        public List<ContagemNumero> Contagens { get; set; } = new();
        public UltimoSorteio? Ultimo { get; set; }
    }
}