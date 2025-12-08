namespace ARISESLCOM.Models.Entities
{
    public class OrderSummaryModel
    {
        public int PKId { get; set; }
        public int PKIdUsuario { get; set; }
        public int IdDados { get; set; }
        public String CustomerName { get; set; }
        public String CustomerDName { get; set; }
        public string Email { get; set; }
        public String ReceiverName { get; set; }
        public String ReceiverLastName { get; set; }
        public DateTime Data { get; set; }
        public DateTime DataMdSt { get; set; }
        public string Status { get; set; }
        public int LionOrderId { get; set; }
        public int Tipo { get; set; }
        public string ModoPagto { get; set; }
        public int Parc { get; set; }
        public double Frete { get; set; }
        public string Via { get; set; }
        public string Track { get; set; }
        public double Desconto { get; set; }   
        public double TaxaJuros { get; set; }
        public int MinParcJuros { get; set; }
        public double Credito { get; set; }
        public string Fretetp { get; set; }
        public double ParcVal { get; set; }
        public int IdAeroporto { get; set; }
        public string  Lcidade { get; set; }
        public string Cidade { get; set; }
        public string Estado { get; set; }
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


    }
}
