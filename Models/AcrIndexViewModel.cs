namespace ARISESLCOM.Models
{
    public class AcrIndexViewModel
    {
        public List<AcrOrderViewModel> Orders { get; set; } = new List<AcrOrderViewModel>();
    }

    public class AcrOrderViewModel
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal ShippingAmount { get; set; }
        public int Installments { get; set; }
        public DateTime OrderDate { get; set; }
        
        // Status do pagamento do produto
        public string PaymentStatus { get; set; } = string.Empty;
        public string PaymentStatusDescription { get; set; } = string.Empty;
        public string PaymentTid { get; set; } = string.Empty;
        public string PaymentAuthCode { get; set; } = string.Empty;
        public int PaymentStep { get; set; }
        
        // Status do pagamento do frete
        public string ShippingStatus { get; set; } = string.Empty;
        public string ShippingStatusDescription { get; set; } = string.Empty;
        public string ShippingTid { get; set; } = string.Empty;
        public string ShippingAuthCode { get; set; } = string.Empty;
        public int ShippingStep { get; set; }

        // Propriedades calculadas
        public decimal TotalAmount => Amount + ShippingAmount;
        public bool IsMultiInstallment => Installments > 1;
        public bool CanProcessPayment => string.IsNullOrEmpty(PaymentStatus) || PaymentStatus.Contains("ERRO");
        public bool CanProcessShipping => string.IsNullOrEmpty(ShippingStatus) || ShippingStatus.Contains("ERRO");
        public bool CanProcessFull => Installments == 1 && CanProcessPayment;

        public string GetStatusColor(string status)
        {
            if (string.IsNullOrEmpty(status))
                return "text-muted";
            
            if (status.ToUpper().Contains("ERRO"))
                return "text-danger";
            else if (status.ToUpper().Contains("CONFIRMADO"))
                return "text-success";
            else if (status.ToUpper().Contains("AUTORIZADO"))
                return "text-info";
            
            return "text-warning";
        }
    }
}
