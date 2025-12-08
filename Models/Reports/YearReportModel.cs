using ARISESLCOM.Models.Domains.DB;

namespace ARISESLCOM.Models.Reports
{
    public class YearReportModel
    {

        public int Mes { get; set; }
        public int Ano { get; set; }
        public decimal ValorVenda { get; set; }
        public decimal Credito { get; set; }
        public decimal Juros { get; set; }
        public decimal Frete { get; set; }
        public int Num_ped { get; set; }
        public int Days { get; set; }
    }
}
