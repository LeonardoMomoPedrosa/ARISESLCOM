using SLCOMLIB.Dtos.Site.Request;

namespace ARISESLCOM.Services
{
    public class BaseCacheServices
    {
        public static readonly int FRAUD_CACHE_MINUTES = 2;
        public static readonly int CUSTOMER_PROFILE_MINUTES = 180;
        public static readonly int ORDER_MINUTES = 1;
        public static readonly int ORDER_STATUS_REPORT_MINUTES = 10;
        public static readonly int ORDER_STATUS_TODAY_REPORT_MINUTES = 2;
        public static readonly int DAY_REPORT_MINUTES = 5;
        public static readonly int YEAR_REPORT_MINUTES = 60;
        public static readonly int TOKEN_MINUTES = 50;
        public static readonly int TRACKING_LIST_HOURS = 12;

        public static string GetSiteAuthKey() => "SITE_AUTH_KEY";

        public static string GetFraudRedisKey(int orderId)
        {
            return $"Fraud:{orderId}";
        }

        public static string GetCustProfileReditKey(int customerId)
        {
            return $"CustProfile:{customerId}";
        }

        public static string GetOrderReditKey(int orderId)
        {
            return $"Order:{orderId}";
        }

        public static string GetOrderStatusReportKey()
        {
            return "OrderStatusReport";
        }

        public static string GetOrderStatusTodayReportKey()
        {
            return "OrderStatusTodayReport";
        }

        public static string GetDayReportKey(string date)
        {
            return $"DayReport:{date}";
        }

        public static string GetMonthReportKey(string month)
        {
            return $"MonthReport:{month}";
        }

        public static string GetGroupItemKey(string month)
        {
            return $"GroupItem:{month}";
        }

        public static string GetGroupDetailKey(string month, int id)
        {
            return $"GroupDetail:{month}:{id}";
        }

        internal static string GetYearReportKey()
        {
            return "YearReport";
        }

        internal static string GetProductTypeListKey()
        {
            return "ProductTypeList";
        }

        public static string GetCorreiosCacheKey()
        {
            return "CorreioKey";
        }

        public static string GetRedeErrorsKey(string errorCode)
        {
            return $"RedeError:{errorCode}";
        }

        internal static string GetProductSubTypeListKey(int typeId)
        {
            return $"ProductSubTypeList:{typeId}";
        }

        internal static string GetAviseMeReportKey()
        {
            return "AviseMeReport";
        }

        public static string GetTrackingListKey()
        {
            return "TrackingList";
        }
    }
}
