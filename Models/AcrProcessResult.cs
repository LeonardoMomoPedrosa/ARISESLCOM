namespace ARISESLCOM.Models
{
    public class AcrProcessResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class AcrOrderInfo
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CardData { get; set; } = string.Empty;
        public string CardExpiry { get; set; } = string.Empty;
        public decimal ParcVal { get; set; }
        public decimal ShippingAmount { get; set; }
        public int Installments { get; set; }
        
        // Status do pagamento
        public string PaymentStatus { get; set; } = string.Empty;
        public string PaymentStatusDescription { get; set; } = string.Empty;
        public string PaymentTid { get; set; } = string.Empty;
        public string PaymentAuthCode { get; set; } = string.Empty;
        public int PaymentStep { get; set; }
        
        // Status do frete
        public string ShippingStatus { get; set; } = string.Empty;
        public string ShippingStatusDescription { get; set; } = string.Empty;
        public string ShippingTid { get; set; } = string.Empty;
        public string ShippingAuthCode { get; set; } = string.Empty;
        public int ShippingStep { get; set; }
    }
}
