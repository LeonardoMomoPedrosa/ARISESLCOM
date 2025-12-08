using ARISESLCOM.Models.Domains.DB;
using ARISESLCOM.Models.Domains.interfaces;
using ARISESLCOM.Models.Reports;
using ARISESLCOM.Services.interfaces;

namespace ARISESLCOM.Models.Domains
{
    public class ReportDomainModel(IRedisCacheService redis) : ReportDB(redis), IReportDomainModel
    {
        public async Task<List<DayReportModel>> GetCupomReportModel(string cupom, DateModel date)
        {
            return await GetCupomReportModelDBAsync(cupom, date);
        }
    }
}
