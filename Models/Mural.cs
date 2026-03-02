using System;
using System.ComponentModel.DataAnnotations;

namespace SSSite.Models
{
    public class MuralMensagem
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Conteudo { get; set; }

        [StringLength(30)]
        public string Autor { get; set; }

        public DateTime Data { get; set; }
        public string CorNeon { get; set; }
    }
}