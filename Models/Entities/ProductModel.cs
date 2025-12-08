
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ARISESLCOM.Models.Entities
{
    public class ProductModel
    {
        [Required(ErrorMessage = "Tipo � obrigat�rio")]
        [BindProperty]
        public int Tipo { get; set; }

        [Required(ErrorMessage = "Sub Tipo � obrigat�rio")]
        [BindProperty]
        public int SubTipo { get; set; }

        public int SubSubTipo { get; set; }

        [Required(ErrorMessage = "Nome � obrigat�rio")]
        [BindProperty]
        public string Nome { get; set; }

        [Required(ErrorMessage = "Meta Nome � obrigat�rio")]
        [BindProperty]
        public string MetaNome { get; set; }
        
        [Required(ErrorMessage = "Meta Key � obrigat�rio")]
        [BindProperty]
        public string MetaKey { get; set; }

        [Required(ErrorMessage = "Descri��o � obrigat�rio")]
        [BindProperty]
        public string Descricao { get; set; }

        [Required(ErrorMessage = "Pre�o � obrigat�rio")]
        [BindProperty]
        public decimal PrecoAtacado { get; set; }

        [Required(ErrorMessage = "Lucro � obrigat�rio")]
        [BindProperty]
        public decimal Lucro { get; set; }

        [Required(ErrorMessage = "Peso � obrigat�rio")]
        [BindProperty]
        public int Peso { get; set; }

        public bool Estoque { get; set; }
        public bool Promocao { get; set; }

        [Required(ErrorMessage = "Dias � obrigat�rio")]
        [BindProperty]
        public int? DiasDisp { get; set; }

        public int PKId { get; set; }
        public bool Ativo { get; set; }
        public int DisplayOrder { get; set; }
        public bool NoPac { get; set; }
        public bool Recomenda { get; set; }
        public decimal PrecoAnt { get; set; }
        public int MinParcJuros { get; set; }
        public int IdFornecedor { get; set; }
        public String Video { get; set; }
        public int EPS6Id { get; set; }
        public int EPS6StockMin { get; set; }

        public string? NomeFoto { get; set; }

        public decimal PrecoFinal => PrecoAtacado * (1 + Lucro / 100);
    }
}
