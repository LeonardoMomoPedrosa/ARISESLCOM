
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ARISESLCOM.Models
{
    public class ProductViewModel
    {
        [Required(ErrorMessage = "Tipo � obrigat�rio")]
        [BindProperty]
        public int Tipo { get; set; }

        public int PKId { get; set; }

        [BindProperty]
        public int SubTipo { get; set; }

        [Required(ErrorMessage = "Nome � obrigat�rio")]
        [BindProperty]
        public string Nome { get; set; }

        public string? MetaNome { get; set; }
        public string? MetaKey { get; set; }

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

        [Required(ErrorMessage = "Dias � obrigat�rio")]
        [BindProperty]
        public int? DiasDisp { get; set; }

        [BindProperty]
        public string Estoque { get; set; }
        [BindProperty]
        public string Ativo { get; set; }
        [BindProperty]
        public string NoPac { get; set; }
        [BindProperty]
        public string Promocao { get; set; }
        [BindProperty]
        public string Recomenda { get; set; }
        public int DisplayOrder { get; set; }
        public decimal PrecoAnt { get; set; }
        public int MinParcJuros { get; set; }
        public int IdFornecedor { get; set; }
        public String Video { get; set; }
        public int ERPId { get; set; }
        public int ERPStockMin { get; set; }

        public string? NomeFoto { get; set; }

        public decimal GetPrecoFinal()
        {
            return Math.Round(PrecoAtacado * (1 + Lucro / 100), 2);
        }
    }
}
