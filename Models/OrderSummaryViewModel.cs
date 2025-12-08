using ARISESLCOM.Models.Entities;

namespace ARISESLCOM.Models
{
    public class OrderSummaryViewModel
    {
        public int PKId { get; set; }
        public String CustomerName { get; set; }
        public String CustomerDName { get; set; }
        public String ReceiverName { get; set; }
        public String ReceiverLastName { get; set; }
        public string ReceiverFullName
        {
            get => ReceiverName + " " + ReceiverLastName;
        }
        public String Data { get; set; }
        public String DataMdSt { get; set; }
        public int Tipo { get; set; }
        public string Lcidade { get; set; } = string.Empty;
        public string Cidade { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public double Desconto { get; set; }
        public double Frete { get; internal set; }
        public string FreteTp { get; set; }
        public int IdDados { get; set; }
        public string Email { get; internal set; }
        public int CustomerId { get; internal set; }

        public int IdAeroporto { get; set; }
        public int LionOrderId { get; set; }
        public string ModoPagto { get; set; }
        public int Parc { get; internal set; }
        public double ParcVal { get; set; }
        public string Status { get; set; }
        public string Via { get; set; }
        public string Track { get; set; }
        public double Credito { get; internal set; }
        public bool SendAction { get; set; }

        public string TID { get; set; }
        public string TID_SHIP { get; set; }
        public string NSU { get; set; }
        public string NSU_SHIP { get; set; }
        public string AUTHCODE { get; set; }
        public string AUTHCODE_SHIP { get; set; }
        public string REDESTATUS { get; set; }
        public string REDESTATUS_SHIP { get; set; }
        public string REDESTATUSDESC { get; set; }
        public string REDESTATUSDESC_SHIP { get; set; }
        public string First6 { get; set; }
        public string Last4 { get; set; }
        public string NomeCC { get; set; }

        public decimal GetAmountParc()
        {
            return Parc * (decimal)ParcVal;
        }

    }
}
