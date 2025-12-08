namespace ARISESLCOM.Models
{
    public class FraudModel
    {
        public FraudModel()
        {
            FraudIPList = [];
            FraudCEPList = [];
            FraudPWDList = [];
        }
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public byte? Confiavel { get; set; }
        public bool SameIPlInd { get; set; }
        public bool SameCEPInd { get; set; }
        public bool SamePWDInd { get; set; }

        public List<FraudIP> FraudIPList { get; set; }
        public List<FraudPWD> FraudPWDList { get; set; }
        public List<FraudCEP> FraudCEPList { get; set; }
    }

    public class FraudIP
    {
        public int IdUsuario { get; set; }
        public string Nome { get; set; }
        public string email { get; set; }
        public DateTime UltimoAcesso { get; set; }
        public string IP { get; set; }
    }

    public class FraudPWD
    {
        public int IdUsuario { get; set; }
        public string Nome { get; set; }
        public string email { get; set; }
        public DateTime UltimoAcesso { get; set; }
        public string IP { get; set; }
    }

    public class FraudCEP
    {
        public int IdUsuario { get; set; }
        public string Nome { get; set; }
        public string email { get; set; }
        public DateTime UltimoAcesso { get; set; }
        public string IP { get; set; }
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public string CEP { get; set; }
        public string Ruav { get; set; }
    }
}
