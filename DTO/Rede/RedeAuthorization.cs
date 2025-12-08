namespace ARISESLCOM.DTO.Rede
{
    public class RedeAuthorization
    {
        public DateTime DateTime { get; set; }
        public string ReturnCode { get; set; } = string.Empty;
        public string ReturnMessage { get; set; } = string.Empty;
        public int Affiliation { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string Tid { get; set; } = string.Empty;
        public string Nsu { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public int Amount { get; set; }
        public int Installments { get; set; }
        public string CardBin { get; set; } = string.Empty;
        public string Last4 { get; set; } = string.Empty;
        public string SoftDescriptor { get; set; } = string.Empty;
        public int Origin { get; set; }
        public bool Subscription { get; set; }
        public RedeBrand Brand { get; set; } = new RedeBrand();
    }
}
