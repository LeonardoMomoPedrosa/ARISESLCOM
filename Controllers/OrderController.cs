using Microsoft.AspNetCore.Mvc;
using ARISESLCOM.Models;
using ARISESLCOM.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using ARISESLCOM.Data;
using ARISESLCOM.Models.Domains.interfaces;
using ARISESLCOM.Models.Mappers.interfaces;
using ARISESLCOM.Models.Domains;
using StackExchange.Redis;
using ARISESLCOM.Services.interfaces;
using ARISESLCOM.Services;
using ARISESLCOM.Models.Domains.DB;
using ARISESLCOM.Models.Reports;
using ARISESLCOM.Helpers;
using System.Linq;
using ARISESLCOM.DTO;

namespace ARISESLCOM.Controllers
{

    [Authorize]
    public class OrderController(IOrderDomainModel orderDomainModel,
                                    ICustomerDomainModel customerDomainModel,
                                    IOrderViewMapper orderViewMapper,
                                    IFreteDomainModel freteDomainModel,
                                    IDBContext dBContext,
                                    IRedisCacheService redis,
                                    ICorreiosService correiosService,
                                    ILionServices lionServices,
                                    IDynamoDBService dynamoDBService,
                                    ILogger<OrderController> logger) : BasicController(redis)
    {
        private readonly IOrderDomainModel _orderDomainModel = orderDomainModel;
        private readonly ICustomerDomainModel _customerDomainModel = customerDomainModel;
        private readonly IFreteDomainModel _freteDomainModel = freteDomainModel;
        private readonly IOrderViewMapper _orderViewMapper = orderViewMapper;
        private readonly IDBContext _dbContext = dBContext;
        private readonly ICorreiosService _correiosService = correiosService;
        private readonly ILionServices _lionServices = lionServices;
        private readonly IDynamoDBService _dynamoDBService = dynamoDBService;
        private readonly ILogger<OrderController> _logger = logger;

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult ListarPrevia()
        {
            return View();
        }

        [HttpGet]
        [Route("/Order/Listar/{tp}")]
        public async Task<IActionResult> Listar(string tp)
        {
            List<OrderSummaryModel> model;
            _dbContext.GetSqlConnection().Open();
            try
            {
                _orderDomainModel.SetContext(_dbContext);
                
                // Casos especiais para pedidos de hoje
                if (tp == "rejected-today")
                {
                    model = await _orderDomainModel.GetRejectedOrdersTodayAsync();
                    ViewBag.PageTitle = "Pedidos Recusados Hoje";
                }
                else if (tp == "sent-today")
                {
                    model = await _orderDomainModel.GetSentOrdersTodayAsync();
                    ViewBag.PageTitle = "Pedidos Enviados Hoje";
                }
                else if (tp == "cancelled-today")
                {
                    model = await _orderDomainModel.GetCancelledOrdersTodayAsync();
                    ViewBag.PageTitle = "Pedidos Cancelados Hoje";
                }
                else
                {
                    model = await _orderDomainModel.GetOrdersByStatusAsync(tp);
                }
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
            return View(_orderViewMapper.MapOrderSummaryList(model));
        }


        [HttpGet]
        [Route("/Order/ShowOrder/{orderId}")]
        public async Task<IActionResult> ShowOrder(int orderId)
        {
            OrderModel orderModel;
            _dbContext.GetSqlConnection().Open();
            try
            {
                _orderDomainModel.SetContext(_dbContext);
                _customerDomainModel.SetContext(_dbContext);
                _freteDomainModel.SetContext(_dbContext);

                orderModel = await _orderDomainModel.GetOrderAsync(orderId);

                orderModel.CustomerModel = await _customerDomainModel.GetCustomerModelAsync(orderModel.OrderSummary.PKIdUsuario,
                                                                                            orderModel.OrderSummary.IdDados);
                orderModel = await _freteDomainModel.GetFreteInfo(orderModel);
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
            return View(_orderViewMapper.MapOrderViewModel(orderModel));
        }

        [HttpPost]
        public async Task<IActionResult> ChangeStatus(int orderId, string status, string origStatus)
        {
            _dbContext.GetSqlConnection().Open();

            try
            {
                _orderDomainModel.SetContext(_dbContext);
                ActionResultModel model = await _orderDomainModel.ChangeOrderStatusAsync(orderId, status, origStatus);

                //Warning/Err mostra na tela e bot�o volta pro pedido
                if (model.Status.Equals(ActionResultModel.WARNING)
                    || model.Status.Equals(ActionResultModel.ERROR))
                {
                    model.Controller = "Order";
                    model.Action = "ShowOrder";
                    model.ButtonType = ActionResultModel.BUTTON_TYPE_BACK;
                    model.Param = orderId + "";
                    return View("Message", model);
                }
                else if (model.Status.Equals(ActionResultModel.SUCCESS))
                {
                    //Se tiver mensagem
                    if (model.Message.Length > 10)
                    {
                        model.Controller = "Order";
                        model.Action = "ListarPrevia";
                        model.ButtonType = ActionResultModel.BUTTON_TYPE_CONTIUE;
                        model.Param = "";
                        return View("Message", model);
                    }
                }
            }
            finally
            {
                await _dbContext.CloseAsync();
            }

            return RedirectToAction("ListarPrevia");
        }

        [HttpGet]
        public IActionResult Search()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SearchResultsAsync(int orderId, string cusName)
        {
            List<OrderSummaryModel> model = [];
            _dbContext.GetSqlConnection().Open();
            try
            {
                _orderDomainModel.SetContext(_dbContext);
                if (cusName != null && cusName.Length > 0)
                {
                    model = await _orderDomainModel.SearchOrderByCustomerNameDBAsync(cusName);
                }
                else if (orderId > 0)
                {
                    var orderModel = await _orderDomainModel.GetOrderAsync(orderId);
                    if (orderModel != null && orderModel.Items.Count > 0)
                    {
                        model.Add(orderModel.OrderSummary);
                    }
                }
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
            return View(_orderViewMapper.MapOrderSummaryList(model));
        }

        [HttpGet]
        [Route("/Order/GetRastreamento/{codigoRastreamento}")]
        public async Task<IActionResult> GetRastreamento(string codigoRastreamento)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(codigoRastreamento))
                {
                    return BadRequest(new { error = "C�digo de rastreamento � obrigat�rio" });
                }

                var rastreamento = await _correiosService.GetRastreamentoAsync(codigoRastreamento);
                return Ok(rastreamento);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Erro ao buscar rastreamento: " + ex.Message });
            }
        }

        public async Task<IActionResult> Tracking()
        {
            List<TrackingModel> trackingHistory = [];
            _dbContext.GetSqlConnection().Open();
            try
            {
                _orderDomainModel.SetContext(_dbContext);
                trackingHistory = await _orderDomainModel.GetTrackingHistoryAsync();
            }
            finally
            {
                await _dbContext.CloseAsync();
            }

            ViewBag.TrackingHistory = trackingHistory;

            return View();
        }

        public async Task<IActionResult> SaveTracking(TrackingModel model)
        {
            _logger.LogInformation("SaveTracking chamado - OrderId: {OrderId}, TrackNo: '{TrackNo}', Via: '{Via}', Source: '{Source}'", 
                model?.OrderId, model?.TrackNo, model?.Via, model?.Source);
            
            List<TrackingModel> trackingHistory = [];
            if (ModelState.IsValid)
            {
                // Validar se o pedido existe antes de salvar
                var source = !string.IsNullOrWhiteSpace(model.Source) 
                    ? model.Source.Trim().ToUpperInvariant() 
                    : string.Empty;
                _logger.LogInformation("SaveTracking - Source recebido: '{Source}', Processado: '{ProcessedSource}'", model.Source, source);
                
                if (source == "E")
                {
                    _logger.LogInformation("Validando pedido no E-Commerce antes de salvar - OrderId: {OrderId}", model.OrderId);
                    try
                    {
                        _dbContext.GetSqlConnection().Open();
                        try
                        {
                            _orderDomainModel.SetContext(_dbContext);
                            var orderExists = await _orderDomainModel.OrderExistsAsync(model.OrderId);
                            
                            if (!orderExists)
                            {
                                _logger.LogWarning("Pedido {OrderId} n�o encontrado no E-Commerce", model.OrderId);
                                ViewBag.Success = false;
                                ViewBag.ShowOrderNotFoundModal = true;
                                ViewBag.OrderIdNotFound = model.OrderId;
                                ViewBag.SourceNotFound = "E";
                                
                                // Carregar hist�rico para exibir na view
                                trackingHistory = await _orderDomainModel.GetTrackingHistoryAsync();
                                ViewBag.TrackingHistory = trackingHistory;
                                return View("Tracking", model);
                            }
                            
                            _logger.LogInformation("Pedido {OrderId} validado no E-Commerce. Continuando com o fluxo.", model.OrderId);
                        }
                        finally
                        {
                            await _dbContext.CloseAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao validar pedido {OrderId} no E-Commerce", model.OrderId);
                        ViewBag.Success = false;
                        ViewBag.ShowOrderNotFoundModal = true;
                        ViewBag.OrderIdNotFound = model.OrderId;
                        ViewBag.SourceNotFound = "E";
                        
                        // Carregar hist�rico para exibir na view
                        if (!_dbContext.GetSqlConnection().State.HasFlag(System.Data.ConnectionState.Open))
                        {
                            _dbContext.GetSqlConnection().Open();
                        }
                        try
                        {
                            _orderDomainModel.SetContext(_dbContext);
                            trackingHistory = await _orderDomainModel.GetTrackingHistoryAsync();
                        }
                        finally
                        {
                            await _dbContext.CloseAsync();
                        }
                        ViewBag.TrackingHistory = trackingHistory;
                        return View("Tracking", model);
                    }
                }
                else if (source == "L")
                {
                    _logger.LogInformation("Validando pedido no ERP-Lion antes de salvar - OrderId: {OrderId}", model.OrderId);
                    try
                    {
                        var clientInfo = await _lionServices.GetClientNameByOrderId(model.OrderId);
                        _logger.LogInformation("GetClientNameByOrderId retornou - ClientName: '{ClientName}', Email: '{Email}'", 
                            clientInfo?.ClientName, clientInfo?.Email);
                        
                        // Se n�o retornar email ou email vazio, o pedido n�o existe no ERP-Lion
                        if (clientInfo == null || string.IsNullOrWhiteSpace(clientInfo.Email))
                        {
                            _logger.LogWarning("Pedido {OrderId} n�o encontrado no ERP-Lion (email vazio ou nulo)", model.OrderId);
                            ViewBag.Success = false;
                            ViewBag.ShowOrderNotFoundModal = true;
                            ViewBag.OrderIdNotFound = model.OrderId;
                            ViewBag.SourceNotFound = "L";
                            
                            // Carregar hist�rico para exibir na view
                            _dbContext.GetSqlConnection().Open();
                            try
                            {
                                _orderDomainModel.SetContext(_dbContext);
                                trackingHistory = await _orderDomainModel.GetTrackingHistoryAsync();
                            }
                            finally
                            {
                                await _dbContext.CloseAsync();
                            }
                            ViewBag.TrackingHistory = trackingHistory;
                            return View("Tracking", model);
                        }
                        
                        _logger.LogInformation("Pedido {OrderId} validado no ERP-Lion. Continuando com o fluxo.", model.OrderId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao validar pedido {OrderId} no ERP-Lion", model.OrderId);
                        ViewBag.Success = false;
                        ViewBag.ShowOrderNotFoundModal = true;
                        ViewBag.OrderIdNotFound = model.OrderId;
                        ViewBag.SourceNotFound = "L";
                        
                        // Carregar hist�rico para exibir na view
                        _dbContext.GetSqlConnection().Open();
                        try
                        {
                            _orderDomainModel.SetContext(_dbContext);
                            trackingHistory = await _orderDomainModel.GetTrackingHistoryAsync();
                        }
                        finally
                        {
                            await _dbContext.CloseAsync();
                        }
                        ViewBag.TrackingHistory = trackingHistory;
                        return View("Tracking", model);
                    }
                }

                _dbContext.GetSqlConnection().Open();
                try
                {
                    _orderDomainModel.SetContext(_dbContext);
                    int rows = await _orderDomainModel.UpdateTrackingAsync(model);

                    if (rows > 0)
                    {
                        // Se origem for ERP (Lion), chamar API do Lion para atualizar o rastreamento
                        if (source == "L")
                        {
                            _logger.LogInformation("Chamando UpdateOrderTrack - OrderId: {OrderId}, TrackNo: {TrackNo}, Via: {Via}", 
                                model.OrderId, model.TrackNo, model.Via);
                            try
                            {
                                var lionResult = await _lionServices.UpdateOrderTrack(model.OrderId, model.TrackNo, model.Via);
                                _logger.LogInformation("UpdateOrderTrack chamado com sucesso. Resultado (primeiros 200 chars): {Result}", 
                                    lionResult?.Substring(0, Math.Min(200, lionResult?.Length ?? 0)));
                            }
                            catch (Exception ex)
                            {
                                // Log do erro, mas n�o falha a opera��o
                                // O rastreamento j� foi salvo no banco
                                _logger.LogError(ex, "Erro ao chamar UpdateOrderTrack para OrderId: {OrderId}", model.OrderId);
                            }
                        }
                        else
                        {
                            _logger.LogInformation("Source n�o � 'L' (� '{Source}'), pulando chamada ao Lion", source);
                        }

                        ViewBag.Success = true;
                        ViewBag.Result = $"Rastreamento inserido com sucesso para pedido <b>#{model.OrderId}</b>";
                        trackingHistory = await _orderDomainModel.GetTrackingHistoryAsync();
                    }
                    else
                    {
                        ViewBag.Success = false;
                        ViewBag.Result = $"Problema ao inserir rastreamento para pedido <b>#{model.OrderId}</b>";
                    }
                }
                finally
                {
                    await _dbContext.CloseAsync();
                }
            }
            else
            {
                ViewBag.Success = false;
            }

            if (trackingHistory.Count == 0)
            {
                _dbContext.GetSqlConnection().Open();
                try
                {
                    _orderDomainModel.SetContext(_dbContext);
                    trackingHistory = await _orderDomainModel.GetTrackingHistoryAsync();
                }
                finally
                {
                    await _dbContext.CloseAsync();
                }
            }

            ViewBag.TrackingHistory = trackingHistory;

            return View("Tracking", model);
        }

        [HttpGet]
        [Route("/Tracking/Follow")]
        public async Task<IActionResult> Follow()
        {
            try
            {
                var trackingPedidos = await _dynamoDBService.GetAllTrackingPedidosAsync();
                return View(trackingPedidos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar rastreamentos do DynamoDB");
                ViewBag.Error = $"Erro ao carregar rastreamentos: {ex.Message}";
                return View(new List<TrackerPedidoViewModel>());
            }
        }

        public async Task<IActionResult> Aging()
        {
            List<OrderStatusReportModel> model = [];
            _dbContext.GetSqlConnection().Open();
            try
            {
                _orderDomainModel.SetContext(_dbContext);

                model = await _orderDomainModel.GetOrderStatusReportDBAsync();
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> SendList()
        {
            List<OrderSummaryModel> model;
            _dbContext.GetSqlConnection().Open();
            try
            {
                _orderDomainModel.SetContext(_dbContext);
                model = await _orderDomainModel.GetOrdersByStatusAsync(SLCOMLIB.Helpers.LibConsts.ORDER_STATUS_PAGTO_CONF_PREP);
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
            return View(_orderViewMapper.MapOrderSummaryList(model));
        }

        [HttpPost]
        public async Task<IActionResult> Send(List<OrderSummaryViewModel> model)
        {
            _dbContext.GetSqlConnection().Open();
            bool allPassed = true;
            ActionResultModel resultModel = new(ActionResultModel.SUCCESS, "");

            try
            {
                _orderDomainModel.SetContext(_dbContext);

                foreach (var modelItem in model)
                {
                    if (modelItem.SendAction)
                    {
                        var actionResult = await _orderDomainModel.ChangeOrderStatusAsync(modelItem.PKId,
                                                                    SLCOMLIB.Helpers.LibConsts.ORDER_STATUS_ENVIADO,
                                                                    modelItem.Status);
                        if (!actionResult.IsSuccess && allPassed)
                        {
                            allPassed = false;
                        }
                    }
                }

                //Warning/Err mostra na tela e bot�o volta pro pedido
                if (!allPassed)
                {
                    resultModel.SetWarning();
                    resultModel.Controller = "Order";
                    resultModel.Action = "SendList";
                    resultModel.ButtonType = ActionResultModel.BUTTON_TYPE_BACK;
                    resultModel.Message = "Alguns envios falharam, tente novamente.";
                    return View("Message", resultModel);
                }
                else
                {
                    resultModel.SetSuccess();
                    resultModel.Controller = "Order";
                    resultModel.Action = "SendList";
                    resultModel.ButtonType = ActionResultModel.BUTTON_TYPE_BACK;
                    resultModel.Message = "Pedidos Enviados com Sucesso";
                    return View("Message", resultModel);
                }
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct(int orderId, int customerId, string prodname, int qtd)
        {

            _dbContext.GetSqlConnection().Open();
            ActionResultModel model = new();
            try
            {
                 _dbContext.StartTrxAsync();
                _orderDomainModel.SetContext(_dbContext);

                var prodItems = prodname.Split('-');
                var productId = int.Parse(prodItems[0]);

                model = await _orderDomainModel.AddProduct(orderId, customerId, productId, qtd);
                await _dbContext.CheckTrxAsync(model);
            }
            catch (Exception)
            {
                await _dbContext.RollbackTrxAsync();
                return RedirectToAction("Message", model);

            }
            finally
            {
                await _dbContext.CloseAsync();
            }
            
            if (!model.IsSuccess)
            {
                model.Controller = "Order";
                model.Action = "ListarPrevia";
                model.ButtonType = ActionResultModel.BUTTON_TYPE_CONTIUE;
                return View("Message", model);
            }

            return RedirectToAction("ShowOrder", new { id = orderId });
        }

        [HttpPost]
        public async Task<IActionResult> DelProduct(int orderId, int odpkid)
        {

            _dbContext.GetSqlConnection().Open();
            ActionResultModel model = new();
            try
            {
                _dbContext.StartTrxAsync();
                _orderDomainModel.SetContext(_dbContext);
                
                model = await _orderDomainModel.DeleteOrderDetailAsync(orderId, odpkid);

                await _dbContext.CheckTrxAsync(model);
            }
            catch (Exception)
            {
                await _dbContext.RollbackTrxAsync();
                return RedirectToAction("/Message", model);

            }
            finally
            {
                await _dbContext.CloseAsync();
            }

            if (!model.IsSuccess)
            {
                model.Controller = "Order";
                model.Action = "ListarPrevia";
                model.ButtonType = ActionResultModel.BUTTON_TYPE_CONTIUE;
                return View("Message", model);
            }
            return RedirectToAction("ShowOrder", new { id = orderId });
        }
    }
}
