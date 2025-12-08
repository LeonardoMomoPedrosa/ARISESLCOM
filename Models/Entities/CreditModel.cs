namespace ARISESLCOM.Models.Entities
{
    public class CreditModel
    {
        public int PKId { get; set; }
        public int IdCompra { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public int IdUsuario { get; set; }

    }
}
