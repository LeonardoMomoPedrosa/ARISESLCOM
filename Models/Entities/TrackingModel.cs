using System.ComponentModel.DataAnnotations;

namespace ARISESLCOM.Models.Entities
{
    public class TrackingModel
    {
        [Required(ErrorMessage = "Num. Pedido � necess�rio.")]
        public int OrderId { get; set; }

        [Required(ErrorMessage = "Digite o Rastreamento")]
        public string TrackNo { get; set; }

        [Required(ErrorMessage = "Selecione a Origem")]
        public string Source { get; set; } // "E" = E-Commerce, "L" = Lion/ERP

        [Required(ErrorMessage = "Selecione a Via")]
        public string Via { get; set; }

        public DateTime InsertedAt { get; set; }
    }
}
