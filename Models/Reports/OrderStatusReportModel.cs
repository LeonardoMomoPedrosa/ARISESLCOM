using System;

namespace ARISESLCOM.Models.Reports
{
    public class OrderStatusReportModel
    {
        public int Pkid { get; set; }
        public string Status { get; set; }
        public string Data { get; set; }
        public int Diff { get; set; }
        public decimal Total { get; set; }
    }
}
