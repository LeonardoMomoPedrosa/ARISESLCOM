using ARISESLCOM.Models.Entities;

namespace ARISESLCOM.Models
{
    public class OrderViewModel
    {
        public OrderSummaryViewModel OrderSummaryViewModel { get; set; }
        public List<OrderItemViewModel> OrderItemViewModelList { get; set; }
        public CustomerAddressViewModel CustomerAddressViewModel { get; set; }
        public AirportViewModel AirportViewModel { get; set; }
        public BuslogModel BuslogModel { get; set; }

        public decimal Get_1_TotalOrderAmount()
        {
            return OrderItemViewModelList.Sum(s => s.GetTotalItemPrice());
        }

        public decimal GetOrderDiscountAmount()
        {
            return Get_2_OrderAmountWithCredit() * (decimal)OrderSummaryViewModel.Desconto / 100;
        }

        public int GetTotalWeight()
        {
            return OrderItemViewModelList.Sum(s => s.GetTotalWeight());
        }

        public decimal Get_2_OrderAmountWithCredit()
        {
            decimal amt = Get_1_TotalOrderAmount();
            amt -= (decimal)OrderSummaryViewModel.Credito;

            return amt < 0 ? 0 : amt;
        }

        public decimal Get_3_OrderAmountWithDiscount()
        {
            decimal amt1 = Get_2_OrderAmountWithCredit();
            decimal amt2 = GetOrderDiscountAmount();
            return amt1 - amt2;
        }

        public decimal GetFinalAmount()
        {
            var osum = OrderSummaryViewModel;
            if (osum.Parc > 1)
            {
                return (decimal)(osum.ParcVal * osum.Parc) + (decimal)osum.Frete;
            }
            else
            {
                return Get_3_OrderAmountWithDiscount() + (decimal)osum.Frete;
            }
        }

        public String GetTotalWeightViewStr()
        {
            var retVal = "";

            var totW = GetTotalWeight();

            if (totW <= 1000)
            {
                retVal = "1 kg";
            }
            else
            {
                retVal = String.Format("{0} Kg", ((decimal)totW / 1000).ToString("F"));
            }

            return retVal;
        }
    }
}
