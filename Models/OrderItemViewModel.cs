namespace ARISESLCOM.Models
{
    public class OrderItemViewModel
    {
        public int PKId { get; set; }
        public int ErpId { get; set; }
        public int SiteId { get; set; }
        public int SubTipoId { get; set; }
        public DateTime CreatioDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public string ProductName { get; set; }
        public int ProductWeight { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public int Weight { get; set; }
        public int Estoque { get; set; }
        public int MinParcJuros { get; set; }
        public int GetTotalWeight() { return Weight * Quantity; }
        public decimal GetTotalItemPrice() {  return UnitPrice * Quantity; }
    }
}
