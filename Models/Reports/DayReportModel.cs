namespace ARISESLCOM.Models.Reports
{
    public class DayReportModel
    {
        public string Data { get; set; }
        public string Datamdst { get; set; }
        public int Pkid { get; set; }
        public int Lionorderid { get; set; }
        public string Notafiscal { get; set; }
        public string Nome { get; set; }
        public string Cidade { get; set; }
        public string Estado { get; set; }
        public decimal Valorpago { get; set; }
        public decimal Frete { get; set; }
        public string Fretetp { get; set; }
        public decimal Juros { get; set; }
        public string Modopagto { get; set; }
    }

}
