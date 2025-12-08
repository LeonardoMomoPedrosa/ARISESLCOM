using Microsoft.AspNetCore.Mvc;
using ARISESLCOM.Data;
using ARISESLCOM.Models;
using ARISESLCOM.Models.Domains.interfaces;
using ARISESLCOM.Models.Entities;
using ARISESLCOM.Models.Mappers.interfaces;
using ARISESLCOM.Services.interfaces;

namespace ARISESLCOM.Controllers
{
    public class AcrController(IAcrDomainModel acrDomainModel, 
    IRedeService redeService, IDBContext dbContext, IOrderDomainModel orderDomainModel, IOrderViewMapper orderViewMapper, ILogger<AcrController> logger) : Controller
    {
        private readonly IAcrDomainModel _acrDomainModel = acrDomainModel;
        private readonly IRedeService _redeService = redeService;
        private readonly IDBContext _dbContext = dbContext;
        private readonly IOrderDomainModel _orderDomainModel = orderDomainModel;
        private readonly IOrderViewMapper _orderViewMapper = orderViewMapper;
        private readonly ILogger<AcrController> _logger = logger;

        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("ACR Index - Iniciando carregamento de pedidos para processamento");
            
            try
            {
                await _dbContext.OpenAsync();
                _logger.LogInformation("ACR Index - Conexão com banco de dados aberta");
                
                _acrDomainModel.SetContext(_dbContext);
                _orderDomainModel.SetContext(_dbContext);
                _logger.LogInformation("ACR Index - Contextos configurados");

                var orders = await _orderDomainModel.GetCreditCardOrdersForProcessingAsync();
                _logger.LogInformation("ACR Index - {OrderCount} pedidos encontrados para processamento", orders.Count);

                // Map to a minimal OrderViewModel list (summary + items)
                var viewModelList = new List<OrderViewModel>();
                foreach (var order in orders)
                {
                    _logger.LogDebug("ACR Index - Processando pedido {OrderId} com status {Status}", 
                        order.OrderSummary.PKId, order.OrderSummary.REDESTATUS);
                    
                    var summaryVm = _orderViewMapper.MapOrderSummary(order.OrderSummary);

                    var itemsVm = order.Items.ConvertAll(item => new OrderItemViewModel
                    {
                        PKId = item.PKId,
                        UnitPrice = item.UnitPrice,
                        Quantity = item.Quantity,
                        CreatioDate = item.CreatioDate,
                        ErpId = item.ErpId,
                        Estoque = item.estoque,
                        MinParcJuros = item.MinParcJuros,
                        ProductName = item.ProductName,
                        ProductWeight = item.ProductWeight,
                        SiteId = item.SiteId,
                        SubTipoId = item.SubTipoId,
                        UpdateDate = item.UpdateDate,
                        Weight = item.Weight
                    });

                    viewModelList.Add(new OrderViewModel
                    {
                        OrderSummaryViewModel = summaryVm,
                        OrderItemViewModelList = itemsVm,
                        CustomerAddressViewModel = new CustomerAddressViewModel(),
                        AirportViewModel = new AirportViewModel(),
                        BuslogModel = new BuslogModel()
                    });
                }

                _logger.LogInformation("ACR Index - {ViewModelCount} ViewModels criados com sucesso", viewModelList.Count);
                return View(viewModelList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ACR Index - Erro ao carregar pedidos: {ErrorMessage}", ex.Message);
                TempData["ErrorMessage"] = $"Erro ao carregar pedidos: {ex.Message}";
                return View(new List<OrderViewModel>());
            }
            finally
            {
                await _dbContext.CloseAsync();
                _logger.LogInformation("ACR Index - Conexão com banco de dados fechada");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ProcessPayment(int orderId, string chargeType)
        {
            _logger.LogInformation("ACR ProcessPayment - Iniciando processamento de pagamento para pedido {OrderId} com tipo {ChargeType}", 
                orderId, chargeType);
            
            try
            {
                await _dbContext.OpenAsync();
                _logger.LogInformation("ACR ProcessPayment - Conexão com banco de dados aberta para pedido {OrderId}", orderId);
                
                _acrDomainModel.SetContext(_dbContext);
                _logger.LogInformation("ACR ProcessPayment - Contexto ACR configurado para pedido {OrderId}", orderId);

                _logger.LogInformation("ACR ProcessPayment - Chamando ProcessPaymentAsync para pedido {OrderId} com tipo {ChargeType}", 
                    orderId, chargeType);
                
                var result = await _acrDomainModel.ProcessPaymentAsync(orderId, chargeType, _redeService);

                if (result.Success)
                {
                    _logger.LogInformation("ACR ProcessPayment - Pagamento processado com sucesso para pedido {OrderId}. Mensagem: {Message}", 
                        orderId, result.Message);
                    TempData["SuccessMessage"] = result.Message;
                }
                else
                {
                    _logger.LogWarning("ACR ProcessPayment - Falha no processamento do pagamento para pedido {OrderId}. Erro: {ErrorMessage}", 
                        orderId, result.ErrorMessage);
                    TempData["ErrorMessage"] = result.ErrorMessage;
                }

                _logger.LogInformation("ACR ProcessPayment - Redirecionando para Index após processamento do pedido {OrderId}", orderId);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ACR ProcessPayment - Erro ao processar pagamento para pedido {OrderId}: {ErrorMessage}", 
                    orderId, ex.Message);
                TempData["ErrorMessage"] = $"Erro ao processar pagamento: {ex.Message}";
                return RedirectToAction("Index");
            }
            finally
            {
                await _dbContext.CloseAsync();
                _logger.LogInformation("ACR ProcessPayment - Conexão com banco de dados fechada para pedido {OrderId}", orderId);
            }
        }

        [HttpPost]
        public async Task<IActionResult> ProcessShipping(int orderId)
        {
            _logger.LogInformation("ACR ProcessShipping - Processando frete para pedido {OrderId}", orderId);
            return await ProcessPayment(orderId, "FRETE");
        }

        [HttpPost]
        public async Task<IActionResult> ProcessFull(int orderId)
        {
            _logger.LogInformation("ACR ProcessFull - Processando produto + frete para pedido {OrderId}", orderId);
            return await ProcessPayment(orderId, "PRODUTO+FRETE");
        }
    }
}
