using ARISESLCOM.Models.Entities;
using ARISESLCOM.Models.Reports;

namespace ARISESLCOM.Models
{
    public class HomeViewModel(List<OrderStatusReportModel> statusModelList,
                                List<OrderStatusTodayReportModel> statusTodayModelList,
                                UserModel user)
    {
        public List<OrderStatusReportModel> OrderStatusReportList { get; set; } = statusModelList;
        public List<OrderStatusTodayReportModel> OrderStatusTodayReportList { get; set; } = statusTodayModelList;
        public UserModel User { get; set; } = user;
        public string test {  get; set; }
    }
}
