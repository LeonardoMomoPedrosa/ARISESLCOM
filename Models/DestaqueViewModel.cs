using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ARISESLCOM.Models
{
    public class DestaqueViewModel
    {
        [Required(ErrorMessage = "Tipo é obrigatório")]
        [BindProperty]
        public int Tipo { get; set; }

        public int PKId { get; set; }

        [Required(ErrorMessage = "Arquivo é obrigatório")]
        [BindProperty]
        public string Arquivo { get; set; }

        [BindProperty]
        public string? Link { get; set; }

        [BindProperty]
        public decimal? Frequencia3 { get; set; }

        // For display purposes
        public string? ImageUrl { get; set; }
    }
}
