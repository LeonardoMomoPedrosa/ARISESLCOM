using ARISESLCOM.Models.Reports;

namespace ARISESLCOM.Models.Domains.interfaces
{
    public interface IReportDomainModel : IDomainModel
    {
        public Task<List<DayReportModel>> GetDayReportModel(DateModel date);
        public Task<List<MonthReportModel>> GetMonthReportModel(DateModel date);
        public Task<List<GroupItemReportModel>> GetGroupItemReportModel(DateModel date);
        public Task<List<GroupItemReportModel>> GetGroupDetailReportModel(int id, DateModel date);
        public Task<List<YearReportModel>> GetYearReportModel();
        public Task<List<AviseMeReportModel>> GetAviseMeReportModel();
        public Task<List<DayReportModel>> GetCupomReportModel(string cupom, DateModel date);
    }
}
