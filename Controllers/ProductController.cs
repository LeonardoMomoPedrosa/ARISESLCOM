using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ARISESLCOM.Data;
using ARISESLCOM.DTO.Api.Request;
using ARISESLCOM.Models;
using ARISESLCOM.Models.Domains;
using ARISESLCOM.Models.Domains.interfaces;
using ARISESLCOM.Models.Entities;
using ARISESLCOM.Models.Mappers;
using ARISESLCOM.Models.Mappers.interfaces;
using ARISESLCOM.Services.interfaces;
using SLCOMLIB.Helpers;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using ARISESLCOM.Services;
using ARISESLCOM.Helpers;

namespace ARISESLCOM.Controllers
{
    [Authorize]
    public class ProductController(IDBContext dBContext,
                                    IGroupDomainModel groupDomainModel,
                                    IProductDomainModel productDomainModel,
                                    IProductViewMapper productViewMapper,
                                    ISiteApiServices siteApi,
                                    IRedisCacheService redis,
                                    ILogger<ProductController> logger) : BasicController(redis)
    {
        private readonly IDBContext _dbContext = dBContext;
        private readonly IGroupDomainModel _groupDomainModel = groupDomainModel;
        private readonly IProductDomainModel _productDomainModel = productDomainModel;
        private readonly ISiteApiServices _siteApi = siteApi;
        private readonly IProductViewMapper _productViewMapper = productViewMapper;
        private readonly ILogger<ProductController> _logger = logger;

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> NewP(int id, int? sid = null)
        {
            NewProductViewModel model = new()
            {
                IdTipo = id,
                ProductModel = new ProductModel()
            };
            _dbContext.GetSqlConnection().Open();
            try
            {
                _groupDomainModel.SetContext(_dbContext);
                model.SubTypeList = await _groupDomainModel.GetProductSubTypeModelsAsync(id);
                if (sid.HasValue)
                {
                    model.ProductModel.SubTipo = sid.Value;
                }
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
            return View(model);
        }

        [HttpPost]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> UploadImage(int id, IFormFile file)
        {
            _logger.LogInformation("=== IN�CIO DO UPLOAD DE IMAGEM ===");
            _logger.LogInformation("Produto ID: {ProductId}", id);
            _logger.LogInformation("Arquivo recebido: {FileName}, Tamanho: {FileSize} bytes", 
                file?.FileName ?? "null", file?.Length ?? 0);
            _logger.LogInformation("Modo de comunica��o: IP privado (sem SSL)");
            _logger.LogWarning("ATEN��O: Upload ser� realizado via IP privado sem criptografia SSL");

            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("Upload falhou: Arquivo inv�lido ou vazio. Produto ID: {ProductId}", id);
                return Json(new { success = false, message = "Arquivo inv�lido" });
            }

            string[] allowed = [".jpg", ".jpeg", ".png", ".webp"];
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            _logger.LogInformation("Extens�o do arquivo: {FileExtension}", ext);

            if (!allowed.Contains(ext))
            {
                _logger.LogWarning("Upload falhou: Formato n�o suportado. Extens�o: {FileExtension}, Produto ID: {ProductId}", 
                    ext, id);
                return Json(new { success = false, message = "Formato n�o suportado" });
            }

            bool cacheSuccessInd = false;
            bool imageOkInd = false;
            var fileFullPath = "";
            _dbContext.GetSqlConnection().Open();
            try
            {
                _logger.LogInformation("Conectando ao banco de dados para buscar produto ID: {ProductId}", id);
                _productDomainModel.SetContext(_dbContext);

                var prodModel = await _productDomainModel.GetProductDBAsync(id);
                _logger.LogInformation("Produto encontrado: {ProductName}, SubTipo: {SubTipo}, SubSubTipo: {SubSubTipo}", 
                    prodModel?.Nome ?? "null", prodModel?.SubTipo ?? 0, prodModel?.SubSubTipo ?? 0);

                _logger.LogInformation("Iniciando upload da imagem para o site via IP privado (sem SSL). Produto ID: {ProductId}", id);
                fileFullPath = await _siteApi.UploadImageToSite(id, file);
                imageOkInd = !string.IsNullOrEmpty(fileFullPath);
                _logger.LogInformation("Upload da imagem via IP privado conclu�do. Sucesso: {Success}, Caminho: {FilePath}", 
                    imageOkInd, fileFullPath ?? "null");

                if (imageOkInd)
                {
                    _logger.LogInformation("Atualizando nome da imagem no banco de dados. Produto ID: {ProductId}, Nome da imagem: {ImageName}", 
                        id, fileFullPath);
                    var rm = await _productDomainModel.UpdateImageNameAsync(id, fileFullPath);
                    _logger.LogInformation("Atualiza��o no banco conclu�da. Sucesso: {Success}, Mensagem: {Message}", 
                        rm.IsSuccess, rm.Message);
                }

                var cacheType = prodModel.SubSubTipo > 0 ? prodModel.SubSubTipo : prodModel.SubTipo;
                _logger.LogInformation("Preparando invalida��o de cache. Cache Type: {CacheType}, Produto ID: {ProductId}", 
                    cacheType, id);
                
                var cacheInfo = new List<CacheInvalidateRequest>
                {
                    new() { Region = SiteCacheKeyUtil.REGION_CATALOGPRODUCTLIST, Key = $"{cacheType}pTrue", CleanRegionInd = false },
                    new() { Region = SiteCacheKeyUtil.REGION_CATALOGPRODUCTLIST, Key = $"{cacheType}pFalse", CleanRegionInd = false },
                    new() { Region = SiteCacheKeyUtil.REGION_PRODUCTDETAILS, Key = $"{id}", CleanRegionInd = false },
                    new() { Region = SiteCacheKeyUtil.REGION_RECOMENDATION, Key = $"{id}", CleanRegionInd = false }
                };
                
                _logger.LogInformation("Iniciando invalida��o de cache. Regi�es: {Regions}", 
                    string.Join(", ", cacheInfo.Select(c => $"{c.Region}:{c.Key}")));
                
                cacheSuccessInd = await _siteApi.InvalidateAsync(cacheInfo);
                _logger.LogInformation("Invalida��o de cache conclu�da. Sucesso: {Success}", cacheSuccessInd);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante o upload de imagem. Produto ID: {ProductId}, Arquivo: {FileName}", 
                    id, file?.FileName ?? "null");
                return BadRequest(ex.Message);
            }
            finally
            {
                _logger.LogInformation("Fechando conex�o com o banco de dados");
                await _dbContext.CloseAsync();
            }

            bool isSuccess = imageOkInd && cacheSuccessInd;
            var msg = "Imagem alterada com sucesso.";
            
            _logger.LogInformation("=== RESULTADO FINAL DO UPLOAD ===");
            _logger.LogInformation("Upload da imagem: {ImageSuccess}", imageOkInd);
            _logger.LogInformation("Invalida��o de cache: {CacheSuccess}", cacheSuccessInd);
            _logger.LogInformation("Sucesso geral: {OverallSuccess}", isSuccess);
            
            if (!isSuccess)
            {
                if (!imageOkInd)
                {
                    msg = "Erro ao enviar imagem";
                    _logger.LogWarning("Falha no upload da imagem. Produto ID: {ProductId}", id);
                }
                else
                {
                    msg = "Erro ao atualizar cache";
                    _logger.LogWarning("Falha na invalida��o de cache. Produto ID: {ProductId}", id);
                }
            }
            else
            {
                _logger.LogInformation("Upload de imagem conclu�do com sucesso. Produto ID: {ProductId}, Arquivo: {FileName}", 
                    id, fileFullPath);
            }
            
            return Json(new
            {
                success = isSuccess,
                message = msg,
                fileName = fileFullPath
            });
        }


        public async Task<IActionResult> NewSelect()
        {
            List<ProductTypeModel> model;
            _dbContext.GetSqlConnection().Open();
            try
            {
                _groupDomainModel.SetContext(_dbContext);
                model = await _groupDomainModel.GetProductTypeModelsAsync();
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
            return View(model);
        }

        public async Task<IActionResult> SaveProductAsync(NewProductViewModel model)
        {
            _dbContext.GetSqlConnection().Open();
            ActionResultModel outModel;
            try
            {
                _productDomainModel.SetContext(_dbContext);
                outModel = await _productDomainModel.CreateProductAsync(model.ProductModel);
            }
            finally
            {
                await _dbContext.CloseAsync();
            }

            outModel.Controller = "/Product";
            outModel.Action = "NewSelect";
            outModel.ButtonType = ActionResultModel.BUTTON_TYPE_BACK;
            outModel.Param = "";
            return View("Message", outModel);
        }

        public async Task<IActionResult> EditSelect()
        {
            List<ProductTypeModel> model;
            _dbContext.GetSqlConnection().Open();
            try
            {
                _groupDomainModel.SetContext(_dbContext);
                model = await _groupDomainModel.GetProductTypeModelsAsync();
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
            return View(model);
        }

        public async Task<IActionResult> EditSelectSub(int id)
        {
            List<ProductSubTypeModel> model;
            _dbContext.GetSqlConnection().Open();
            try
            {
                _groupDomainModel.SetContext(_dbContext);
                model = await _groupDomainModel.GetProductSubTypeModelsAsync(id);
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
            return View(model);
        }

        public async Task<IActionResult> NewSelectSub(int id)
        {
            List<ProductSubTypeModel> model;
            _dbContext.GetSqlConnection().Open();
            try
            {
                _groupDomainModel.SetContext(_dbContext);
                model = await _groupDomainModel.GetProductSubTypeModelsAsync(id);
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, int sid)
        {
            List<ProductModel> model;
            _dbContext.GetSqlConnection().Open();
            try
            {
                _productDomainModel.SetContext(_dbContext);
                model = await _productDomainModel.GetProductListBySubTypeAsync(id, sid);
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
            return View(_productViewMapper.MapProductViewModelList(model));
        }

        [HttpPost]
        public async Task<IActionResult> EditSave([FromBody] ProductViewModel viewModel)
        {
            ActionResultModel resModel;
            _dbContext.GetSqlConnection().Open();
            try
            {
                _productDomainModel.SetContext(_dbContext);

                var prodModel = await _productDomainModel.GetProductDBAsync(viewModel.PKId);
                var model = _productViewMapper.MapProductModel(viewModel);
                resModel = await _productDomainModel.UpdateProductAsync(model);

                var cacheType = prodModel.SubSubTipo > 0 ? prodModel.SubSubTipo : prodModel.SubTipo;
                var isDry = prodModel.SubSubTipo <= 0;

                var cacheInfo = new List<CacheInvalidateRequest>
                {
                    new() { Region = SiteCacheKeyUtil.REGION_CATALOGPRODUCTLIST, Key = $"{cacheType}pTrue", CleanRegionInd = false },
                    new() { Region = SiteCacheKeyUtil.REGION_CATALOGPRODUCTLIST, Key = $"{cacheType}pFalse", CleanRegionInd = false },
                    new() { Region = SiteCacheKeyUtil.REGION_PRODUCTDETAILS, Key = $"{viewModel.PKId}", CleanRegionInd = false },
                    new() { Region = SiteCacheKeyUtil.REGION_RECOMENDATION, Key = $"{viewModel.PKId}", CleanRegionInd = false }
                };

                if (isDry)
                {
                    cacheInfo.Add(new CacheInvalidateRequest
                    {
                        Region = SiteCacheKeyUtil.REGION_DRYCATEGORYMODELS,
                        Key = $"",
                        CleanRegionInd = true
                    });
                }

                cacheSuccessInd = await _siteApi.InvalidateAsync(cacheInfo);
            }
            catch (Exception ex)
            {
                resModel = new(ActionResultModel.ERROR, ex.Message);
            }
            finally
            {
                await _dbContext.CloseAsync();
            }

            bool isSuccess = resModel.IsSuccess && cacheSuccessInd;
            var msg = cacheSuccessInd ? resModel.Message : "Erro ao atualizar cache";

            return Json(new { success = isSuccess, message = msg });
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            ActionResultModel resModel;
            _dbContext.GetSqlConnection().Open();
            try
            {
                _productDomainModel.SetContext(_dbContext);
                var prodModel = await _productDomainModel.GetProductDBAsync(id);
                resModel = await _productDomainModel.DeleteProductAsync(id);

                var cacheType = prodModel.SubSubTipo > 0 ? prodModel.SubSubTipo : prodModel.SubTipo;

                var cacheInfo = new List<CacheInvalidateRequest>
                {
                    new() { Region = SiteCacheKeyUtil.REGION_CATALOGPRODUCTLIST, Key = $"{cacheType}pTrue", CleanRegionInd = false },
                    new() { Region = SiteCacheKeyUtil.REGION_CATALOGPRODUCTLIST, Key = $"{cacheType}pFalse", CleanRegionInd = false },
                    new() { Region = SiteCacheKeyUtil.REGION_PRODUCTDETAILS, Key = $"{id}", CleanRegionInd = false },
                    new() { Region = SiteCacheKeyUtil.REGION_RECOMENDATION, Key = $"{id}", CleanRegionInd = false }
                };
                cacheSuccessInd = await _siteApi.InvalidateAsync(cacheInfo);
            }
            catch (Exception ex)
            {
                resModel = new(ActionResultModel.ERROR, ex.Message);
            }
            finally
            {
                await _dbContext.CloseAsync();
            }

            bool isSuccess = resModel.IsSuccess && cacheSuccessInd;
            var msg = cacheSuccessInd ? resModel.Message : "Erro ao atualizar cache";

            return Json(new { success = isSuccess, message = msg });
        }

        public IActionResult EditNameSelect()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> EditName(string pname)
        {
            List<ProductModel> model;
            _dbContext.GetSqlConnection().Open();
            try
            {
                _productDomainModel.SetContext(_dbContext);
                model = await _productDomainModel.GetProductListByNameDBAsync(pname);
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
            return View(_productViewMapper.MapProductViewModelList(model));
        }

        [HttpGet]
        public async Task<IActionResult> ProductNoStock()
        {
            List<ProductModel> model;
            _dbContext.GetSqlConnection().Open();
            try
            {
                _productDomainModel.SetContext(_dbContext);
                model = await _productDomainModel.GetProductListByStockDBAsync("P", false);
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
            return View(_productViewMapper.MapProductViewModelList(model));
        }

        [HttpGet]
        public async Task<IActionResult> ProductStock()
        {
            List<ProductModel> model;
            _dbContext.GetSqlConnection().Open();
            try
            {
                _productDomainModel.SetContext(_dbContext);
                model = await _productDomainModel.GetProductListByStockDBAsync("P", true);
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
            return View(_productViewMapper.MapProductViewModelList(model));
        }

        [HttpPost]
        public async Task<IActionResult> ChangeStock([FromBody] ProductViewModel viewModel)
        {
            ActionResultModel resModel = new(ActionResultModel.SUCCESS, "SUCCESS");
            var estoque = viewModel.Estoque != null && viewModel.Estoque.Equals("on");
            _dbContext.GetSqlConnection().Open();
            try
            {
                _productDomainModel.SetContext(_dbContext);

                var model = _productViewMapper.MapProductModel(viewModel);
                resModel = await _productDomainModel.UpdateProductStockDBAsync(viewModel.PKId, estoque);
            }
            catch (Exception ex)
            {
                resModel = new(ActionResultModel.ERROR, ex.Message);
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
            return Json(new { success = resModel.IsSuccess, message = resModel.Message });
        }

        [HttpGet]
        public async Task<IActionResult> EditGroup()
        {
            List<ProductTypeModel> model;
            _dbContext.GetSqlConnection().Open();
            try
            {
                _groupDomainModel.SetContext(_dbContext);
                model = await _groupDomainModel.GetProductTypeModelsAsync();
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditSubGroupSelect()
        {
            List<ProductTypeModel> model;
            _dbContext.GetSqlConnection().Open();
            try
            {
                _groupDomainModel.SetContext(_dbContext);
                model = await _groupDomainModel.GetProductTypeModelsAsync();
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
            return View(model);
        }

        public async Task<IActionResult> IntegrationSelect()
        {
            List<ProductTypeModel> model;
            _dbContext.GetSqlConnection().Open();
            try
            {
                _groupDomainModel.SetContext(_dbContext);
                model = await _groupDomainModel.GetProductTypeModelsAsync();
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
            return View(model);
        }

        public async Task<IActionResult> IntegrationSelectSub(int id)
        {
            List<ProductSubTypeModel> model;
            _dbContext.GetSqlConnection().Open();
            try
            {
                _groupDomainModel.SetContext(_dbContext);
                model = await _groupDomainModel.GetProductSubTypeModelsAsync(id);
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Integration(int id, int sid)
        {
            List<ProductModel> model;
            _dbContext.GetSqlConnection().Open();
            try
            {
                _productDomainModel.SetContext(_dbContext);
                model = await _productDomainModel.GetProductListBySubTypeAsync(id, sid);
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
            return View(_productViewMapper.MapProductViewModelList(model));
        }

        [HttpPatch]
        public async Task<IActionResult> PatchERPId(int id, int erpId, int erpStockMin)
        {
            ActionResultModel resModel;
            _dbContext.GetSqlConnection().Open();
            try
            {
                _productDomainModel.SetContext(_dbContext);
                resModel = await _productDomainModel.PatchERPIdAsync(id, erpId, erpStockMin);
            }
            catch (Exception ex)
            {
                resModel = new(ActionResultModel.ERROR, ex.Message);
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
            return Json(new { success = resModel.IsSuccess, message = resModel.Message });
        }

        [HttpGet]
        public async Task<JsonResult> GetSuggestions(string term)
        {
            List<string> result = [];
            _dbContext.GetSqlConnection().Open();
            try
            {
                _productDomainModel.SetContext(_dbContext);
                var resModel = await _productDomainModel.FullTextSearchAsync(term);
                result = resModel.Select(s => s.Result).ToList();
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
            return Json(result);
        }
    }
}



