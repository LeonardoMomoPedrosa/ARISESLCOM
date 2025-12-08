namespace ARISESLCOM.DTO.Rede
{
    public class RedeAuthorizationResult
    {
        public bool Success { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Tid { get; set; } = string.Empty;
        public string Nsu { get; set; } = string.Empty;
        public string AuthorizationCode { get; set; } = string.Empty;
        public string BrandTid { get; set; } = string.Empty;
        public DateTime DateTime { get; set; }
        public int Amount { get; set; }
        public int Installments { get; set; }
        public string CardBin { get; set; } = string.Empty;
        public string Last4 { get; set; } = string.Empty;
        public string ReturnCode { get; set; } = string.Empty;
        public string ReturnMessage { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
