using ARISESLCOM.Helpers;
using ARISESLCOM.Models.Entities;
using ARISESLCOM.Models.Mappers.interfaces;

namespace ARISESLCOM.Models.Mappers
{
    public class OrderViewMapper(ICustomerViewMapper customerViewMapper,
                                    IAirportViewMapper airportViewMapper) : IOrderViewMapper
    {
        private readonly ICustomerViewMapper _customerViewMapper = customerViewMapper;
        private readonly IAirportViewMapper _airportViewMapper = airportViewMapper;
        public OrderSummaryViewModel MapOrderSummary(OrderSummaryModel orderModel)
        {
            return new OrderSummaryViewModel()
            {
                PKId = orderModel.PKId,
                Data = orderModel.Data.ToString(Consts.COMMON_DATE_FORMAT),
                DataMdSt = orderModel.DataMdSt > DateTime.MinValue ? orderModel.DataMdSt.ToString(Consts.COMMON_DATE_FORMAT) : "",
                ReceiverLastName = orderModel.ReceiverLastName,
                ReceiverName = orderModel.ReceiverName,
                Tipo = orderModel.Tipo,
                CustomerName = orderModel.CustomerName,
                CustomerDName = orderModel.CustomerDName,
                CustomerId = orderModel.PKIdUsuario,
                Email = orderModel.Email,
                Lcidade = orderModel.Lcidade,
                Cidade = orderModel.Cidade,
                Estado = orderModel.Estado,
                Desconto = orderModel.Desconto,
                Frete = orderModel.Frete,
                FreteTp = orderModel.Fretetp,
                IdDados = orderModel.IdDados,
                IdAeroporto = orderModel.IdAeroporto,
                LionOrderId = orderModel.LionOrderId,
                ModoPagto = orderModel.ModoPagto,
                Parc = orderModel.Parc,
                ParcVal = orderModel.ParcVal,
                Status = orderModel.Status,
                Via = orderModel.Via,
                Track = orderModel.Track,
                Credito = orderModel.Credito,
                TID = orderModel.TID,
                TID_SHIP = orderModel.TID_SHIP,
                NSU = orderModel.NSU,
                NSU_SHIP = orderModel.NSU_SHIP,
                AUTHCODE = orderModel.AUTHCODE,
                AUTHCODE_SHIP = orderModel.AUTHCODE_SHIP,
                REDESTATUS = orderModel.REDESTATUS,
                REDESTATUS_SHIP = orderModel.REDESTATUS_SHIP,
                REDESTATUSDESC = orderModel.REDESTATUSDESC,
                REDESTATUSDESC_SHIP = orderModel.REDESTATUSDESC_SHIP,
                First6 = orderModel.First6,
                Last4 = orderModel.Last4,
                NomeCC = orderModel.NomeCC
            };

        }

        public List<OrderSummaryViewModel> MapOrderSummaryList(List<OrderSummaryModel> orderModelList)
        {
            List<OrderSummaryViewModel> list = orderModelList.ConvertAll(x => MapOrderSummary(x));
            return list;
        }

        public OrderViewModel MapOrderViewModel(OrderModel orderModel)
        {
            OrderViewModel viewModel = new()
            {
                OrderSummaryViewModel = MapOrderSummary(orderModel.OrderSummary),
                OrderItemViewModelList = MapOrderItemsViewModelList(orderModel.Items),
                CustomerAddressViewModel =
                _customerViewMapper.MapCustomerAddressViewModel(orderModel.CustomerModel.CustomerAddressModelList[0]),
                AirportViewModel = _airportViewMapper.MapAirportViewModel(orderModel.AirportModel),
                BuslogModel = orderModel.BuslogModel
            };

            return viewModel;
        }

        private static List<OrderItemViewModel> MapOrderItemsViewModelList(List<OrderItemModel> items)
        {
            List<OrderItemViewModel> modelList = items.ConvertAll(x => MapOrderItemViewModel(x));
            return modelList;
        }

        private static OrderItemViewModel MapOrderItemViewModel(OrderItemModel item)
        {
            OrderItemViewModel itemViewModel = new()
            {
                PKId = item.PKId,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                CreatioDate = item.CreatioDate,
                ErpId = item.ErpId,
                Estoque = item.estoque,
                MinParcJuros = item.MinParcJuros,
                ProductName = PageLabelHelper.FormatProductName(item.ProductName),
                ProductWeight = item.ProductWeight,
                SiteId = item.SiteId,
                SubTipoId = item.SubTipoId,
                UpdateDate = item.UpdateDate,
                Weight = item.Weight
            };
            return itemViewModel;
        }
    }
}
