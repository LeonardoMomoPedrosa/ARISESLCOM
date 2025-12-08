namespace ARISESLCOM.DTO.Rede
{
    public class RedeGetTransactionResponse
    {
        public DateTime RequestDateTime { get; set; }
        public RedeGetTransactionAuthorization Authorization { get; set; } = new RedeGetTransactionAuthorization();
        public RedeGetTransactionCapture Capture { get; set; } = new RedeGetTransactionCapture();
        public List<RedeGetTransactionLink> Links { get; set; } = new List<RedeGetTransactionLink>();
        public string ReturnCode { get; set; } = string.Empty;
        public string ReturnMessage { get; set; } = string.Empty;
    }

    public class RedeGetTransactionAuthorization
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
        public string BrandTid { get; set; } = string.Empty;
        public RedeGetTransactionBrand Brand { get; set; } = new RedeGetTransactionBrand();
    }

    public class RedeGetTransactionBrand
    {
        public string Name { get; set; } = string.Empty;
        public string ReturnMessage { get; set; } = string.Empty;
        public string ReturnCode { get; set; } = string.Empty;
        public string BrandTid { get; set; } = string.Empty;
        public string AuthorizationCode { get; set; } = string.Empty;
    }

    public class RedeGetTransactionCapture
    {
        public DateTime DateTime { get; set; }
        public string Nsu { get; set; } = string.Empty;
        public int Amount { get; set; }
    }

    public class RedeGetTransactionLink
    {
        public string Method { get; set; } = string.Empty;
        public string Rel { get; set; } = string.Empty;
        public string Href { get; set; } = string.Empty;
    }
}

