using System.Text.Json.Serialization;

namespace ARISESLCOM.DTO.Rede
{
    public class RedeAuthorizationRequest
    {
        public bool Capture { get; set; }
        public string Kind { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public int Amount { get; set; }
        [JsonIgnore]
        public string Affiliation { get; set; } = string.Empty;
        public int Installments { get; set; }
        public string CardholderName { get; set; } = string.Empty;
        public string CardNumber { get; set; } = string.Empty;
        public int ExpirationMonth { get; set; }
        public int ExpirationYear { get; set; }
        public string SecurityCode { get; set; } = string.Empty;
        public string SoftDescriptor { get; set; } = string.Empty;
        public bool Subscription { get; set; }
        public int Origin { get; set; }
        [JsonIgnore]
        public int DistributorAffiliation { get; set; }
        public string BrandTid { get; set; } = string.Empty;
        public string StorageCard { get; set; } = string.Empty;
        [JsonIgnore]
        public RedeTransactionCredentials TransactionCredentials { get; set; } = new RedeTransactionCredentials();
    }
}
