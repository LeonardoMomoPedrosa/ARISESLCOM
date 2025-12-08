using Microsoft.CodeAnalysis;
using Microsoft.Data.SqlClient;
using ARISESLCOM.Helpers;
using ARISESLCOM.Models.Domains.DB;
using ARISESLCOM.Models.Domains.interfaces;
using ARISESLCOM.Models.Entities;
using ARISESLCOM.Services;
using ARISESLCOM.Services.interfaces;
using SLCOMLIB.Helpers;
using StackExchange.Redis;
using System.Linq;

namespace ARISESLCOM.Models.Domains
{
    public class OrderDomainModel(ICreditDomainModel creditDomainModel,
                                    IProductDomainModel productDomainModel,
                                    IShipmentDomainModel shipmentDomainModel,
                                    IRedisCacheService redis,
                                    ILionServices lion) : OrderDB(redis), IOrderDomainModel
    {
        private readonly ILionServices _lionService = lion;
        private readonly ICreditDomainModel _creditDomainModel = creditDomainModel;
        private readonly IProductDomainModel _productDomainModel = productDomainModel;
        private readonly IShipmentDomainModel _shipmentDomainModel = shipmentDomainModel;

        public async Task<List<OrderSummaryModel>> GetOrdersByStatusAsync(string status)
        {
            return await GetOrdersByStatusDBAsync(status);
        }

        public async Task<OrderModel> GetOrderAsync(int orderId)
        {
            return await GetOrderDBAsync(orderId);
        }

        public async Task<bool> OrderExistsAsync(int orderId)
        {
            return await OrderExistsDBAsync(orderId);
        }

        private async Task<List<OrderItemModel>> GetOrderItemsAsync(int orderId)
        {
            return await GetOrderItemsDBAsync(orderId);
        }


        public async Task<ActionResultModel> ChangeOrderStatusAsync(int orderId, string status, string origStatus)
        {
            ActionResultModel resultModel = new(ActionResultModel.SUCCESS, "");
            var orderModel = await GetOrderAsync(orderId);

            //Bloqueia REENVIO
            if (origStatus.Equals(LibConsts.ORDER_STATUS_ENVIADO))
            {
                return new ActionResultModel(ActionResultModel.WARNING,
                                                "Pedido j� enviado. N�o � poss�vel alterar o status.");
            }
            //Bloqueia Cancelamento de j� importada
            else if (status.Equals(LibConsts.ORDER_STATUS_CANC)
                        && orderModel.OrderSummary.LionOrderId > 0)
            {
                return new ActionResultModel(ActionResultModel.WARNING,
                                "Esta pedido foi importado para o sistema, n�o � poss�vel cancelar.");
            }
            //Bloqueia envio de pedido sem importar
            else if (status.Equals(LibConsts.ORDER_STATUS_ENVIADO)
            && orderModel.OrderSummary.LionOrderId <= 0)
            {
                return new ActionResultModel(ActionResultModel.WARNING,
                                "Esta pedido n�o foi importado para o sistema, n�o � poss�vel enviar.");
            }
            else if (origStatus.Equals(LibConsts.ORDER_STATUS_CANC))
            {
                return new ActionResultModel(ActionResultModel.WARNING,
                "Esta pedido j� foi cancelado. N�o � poss�vel alterar seu status.");
            }
            //Caso seja cancelamento, devolver creditos utilizados.
            else if (status.Equals(LibConsts.ORDER_STATUS_CANC))
            {
                var crd = await ReturnCreditAsync(orderId);
                if (crd == null)
                {
                    resultModel.SetSuccess();
                    resultModel.Message = $"Pedido cancelado.";
                }
                else if (crd.IsSuccess)
                {
                    resultModel.SetSuccess();
                    resultModel.Message = $"Um cr�dito foi devolvido ao cliente.";
                }
                else
                {
                    resultModel.SetError();
                    resultModel.Message = "Erro ao devolver cr�dito ao cliente";
                }
            }

            if (status.Equals(LibConsts.ORDER_STATUS_ENVIADO))
            {
                //Enviar pedido no Lion
                var sendResult = await _lionService.SendOrder(orderModel.OrderSummary.LionOrderId);

                if (sendResult.Contains("-OK"))
                {
                    //Envia trx email
                    var trxResult = await _lionService.CreateTrx(orderId, LionServices.TRX_EMAIL);

                    if (!trxResult.Contains("200"))
                    {
                        return new ActionResultModel(ActionResultModel.ERROR
                                                        , $"Erro ao enviar. Comunica��o com ERP falhou. {trxResult}");
                    }
                }
                else
                {
                    return new ActionResultModel(ActionResultModel.ERROR
                                                    , $"Erro ao enviar. Comunica��o com ERP falhou. {sendResult}");
                }
            }

            if (status.Equals(SLCOMLIB.Helpers.LibConsts.FRETE_CLIENTE_RETIRA))
            {
                //publica aviso
                var trxResult = await _lionService.CreateTrx(orderId, LionServices.TRX_CLIENTE_RETIRA);

                if (!trxResult.Contains("200"))
                {
                    return new ActionResultModel(ActionResultModel.ERROR
                                                    , $"Erro ao enviar. Comunica��o com ERP falhou. {trxResult}");
                }
            }

            if (status.Equals(LibConsts.ORDER_STATUS_N_AUTORIZ))
            {
                //publica aviso
                var trxResult = await _lionService.CreateTrx(orderId, LionServices.TRX_NAO_AUTORIZADO);

                if (!trxResult.Contains("200"))
                {
                    return new ActionResultModel(ActionResultModel.ERROR
                                                    , $"Erro ao enviar. Comunica��o com ERP falhou. {trxResult}");
                }
            }

            await ChangeOrderStatusDBAsync(orderId, status);
            return resultModel;
        }

        public async Task<ActionResultModel> ReturnCreditAsync(int orderId)
        {
            _creditDomainModel.SetContext(_dbContext);
            var creditModel = await _creditDomainModel.GetCreditByOrderAsync(orderId);

            ActionResultModel res = null;

            //Order credit is negative
            if (creditModel.Amount < 0)
            {
                res = await _creditDomainModel.DeleteCreditByOrderAsync(orderId);
            }

            return res;
        }

        public async Task<int> UpdateTrackingAsync(TrackingModel model)
        {
            return await UpdateTrackingDBAsync(model);
        }

        public async Task<List<TrackingModel>> GetTrackingHistoryAsync()
        {
            return await GetTrackingHistoryDBAsync();
        }

        public async Task<ActionResultModel> AddProduct(int orderId, int customerId, int productId, int qtd)
        {
            _productDomainModel.SetContext(_dbContext);

            ActionResultModel resultModel = new(ActionResultModel.SUCCESS, "");

            try
            {
                // Check if product already exists in order
                var existingItem = await GetOrderDetailByProductAsync(orderId, productId);
                
                if (existingItem.HasValue)
                {
                    // Product exists: increment quantity instead of creating duplicate
                    var newQuantity = existingItem.Value.Quantity + qtd;
                    await UpdateOrderDetailQuantityAsync(existingItem.Value.PKId, newQuantity);
                }
                else
                {
                    // Product doesn't exist: create new item
                    var productModel = await _productDomainModel.GetProductDBAsync(productId);
                    await InsertOrderDetailDBAsync(orderId, customerId, qtd, productModel);
                }

                // Check and manage special packaging for live products
                await CheckLivePackageAsync(orderId, customerId);

                await UpdateOrderShipment(orderId);

                var orderModel = await GetOrderAsync(orderId);
                var newParcVal = TermRecalc(orderModel);
                await UpdateOrderParcValDBAsync(orderId, newParcVal);

            }
            catch (Exception ex)
            {
                resultModel.SetError($"Erro ao adicionar produto - {ex.Message}");
            }

            return resultModel;
        }

        private static decimal TermRecalc(OrderModel orderModel)
        {
            var orderItems = orderModel.Items;
            var grossAmount = orderItems.Sum(s => s.UnitPrice * s.Quantity);
            var netAmount = grossAmount - (decimal)orderModel.OrderSummary.Credito;
            var finalAmount = (decimal)(100 - orderModel.OrderSummary.Desconto) * netAmount / 100;
            var amountToPay = orderModel.OrderSummary.Parc <= 3 ? finalAmount / orderModel.OrderSummary.Parc :
                                MathHelper.GetTermAmount(finalAmount, (short)orderModel.OrderSummary.Parc);

            // Aplicar valida��o de parcela m�nima apenas para compras parceladas (2 ou mais parcelas)
            if (orderModel.OrderSummary.Parc >= 2 && amountToPay < 50)
            {
                throw new Exception("A parcela m�nima n�o fica acima de R$ 50,00");
            }

            return amountToPay;
        }

        private async Task UpdateOrderShipment(int orderId)
        {
            _shipmentDomainModel.SetContext(_dbContext);

            var orderModel = await GetOrderAsync(orderId);

            if (orderModel.OrderSummary.Fretetp.Equals(SLCOMLIB.Helpers.LibConsts.FRETE_TRANSPORTADORA))
            {
                var ship = await _shipmentDomainModel.GetShipmentTranspAsync(orderId);
                await UpdateShipmentDBAsync(orderId, ship.Preco);
            }
            else if (orderModel.OrderSummary.Fretetp.Equals(SLCOMLIB.Helpers.LibConsts.FRETE_AEROPORTO))
            {
                var ship = await _shipmentDomainModel.GetShipmentAirportAsync(orderId);
                await UpdateShipmentDBAsync(orderId, ship.Preco);
            }
            else if (orderModel.OrderSummary.Fretetp.Equals(SLCOMLIB.Helpers.LibConsts.FRETE_PAC))
            {
                var ship = await _shipmentDomainModel.GetShipmentCorreiosAsync(orderId);
                await UpdateShipmentDBAsync(orderId, ship.Preco);
            }
            else if (orderModel.OrderSummary.Fretetp.Equals(SLCOMLIB.Helpers.LibConsts.FRETE_BUSLOG))
            {
                var ship = await _shipmentDomainModel.GetShipmentBuslogAsync(orderId);
                await UpdateShipmentDBAsync(orderId, ship.Preco);
            }
            else if (orderModel.OrderSummary.Fretetp.Equals(SLCOMLIB.Helpers.LibConsts.FRETE_CLIENTE_RETIRA))
            {
                await UpdateShipmentDBAsync(orderId, 0);
            }
        }

        public async Task<ActionResultModel> DeleteOrderDetailAsync(int orderId, int odpkid)
        {
            ActionResultModel resultModel = new(ActionResultModel.SUCCESS, "");

            try
            {
                // Get customer ID before deleting (needed for CheckLivePackage)
                var orderModel = await GetOrderAsync(orderId);
                var customerId = orderModel.OrderSummary.PKIdUsuario;

                await DeleteOrderDetailDBAsync(odpkid);

                // Check and manage special packaging for live products
                await CheckLivePackageAsync(orderId, customerId);

                await UpdateOrderShipment(orderId);

                orderModel = await GetOrderAsync(orderId);
                var newTermVal = TermRecalc(orderModel);
                await UpdateOrderParcValDBAsync(orderId, newTermVal);


            }
            catch (Exception ex)
            {
                resultModel.SetError($"Erro ao remover produto - {ex.Message}");
            }

            return resultModel;
        }

        private async Task CheckLivePackageAsync(int orderId, int customerId)
        {
            try
            {
                var orderItems = await GetOrderItemsAsync(orderId);

                // Check if order has live products
                bool hasLiveProducts = orderItems.Any(item => SLCOMLIB.Helpers.LibConsts.LIVE_PRODUCT_TYPES.Contains(item.SubTipoId));

                // Check if order has special packaging product (ID 1354)
                bool hasPackage = orderItems.Any(item => item.SiteId == SLCOMLIB.Helpers.LibConsts.SPECIAL_PACKAGING_PRODUCT_ID);

                // Scenario A: Has live products AND no package -> Add package
                if (hasLiveProducts && !hasPackage)
                {
                    var packageProduct = await _productDomainModel.GetProductDBAsync(SLCOMLIB.Helpers.LibConsts.SPECIAL_PACKAGING_PRODUCT_ID);
                    if (packageProduct != null)
                    {
                        await InsertOrderDetailDBAsync(orderId, customerId, 1, packageProduct);
                    }
                }
                // Scenario B: No live products AND has package -> Remove package
                else if (!hasLiveProducts && hasPackage)
                {
                    var packageItem = orderItems.FirstOrDefault(item => item.SiteId == SLCOMLIB.Helpers.LibConsts.SPECIAL_PACKAGING_PRODUCT_ID);
                    if (packageItem != null)
                    {
                        await DeleteOrderDetailDBAsync(packageItem.PKId);
                    }
                }
                // Scenarios C and D: No change needed
                // C: Has live products AND has package -> Keep as is
                // D: No live products AND no package -> Keep as is
            }
            catch (Exception ex)
            {
                // Log warning but don't break the operation
                // Package management is not critical - order remains functional
                // In production, you might want to log this to a logger
                System.Diagnostics.Debug.WriteLine($"Warning: CheckLivePackage failed - {ex.Message}");
            }
        }

        public async Task<List<OrderSummaryModel>> GetRejectedOrdersTodayAsync()
        {
            return await GetRejectedOrdersTodayDBAsync();
        }

        public async Task<List<OrderSummaryModel>> GetSentOrdersTodayAsync()
        {
            return await GetSentOrdersTodayDBAsync();
        }

        public async Task<List<OrderSummaryModel>> GetCancelledOrdersTodayAsync()
        {
            return await GetCancelledOrdersTodayDBAsync();
        }

        public async Task<List<OrderModel>> GetCreditCardOrdersForProcessingAsync()
		{
            return await GetCreditCardOrdersForProcessingDBAsync();
		}
    }
}
