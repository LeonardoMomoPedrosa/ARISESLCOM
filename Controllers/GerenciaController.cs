using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ARISESLCOM.Data;
using ARISESLCOM.DTO.Api.Request;
using ARISESLCOM.Models;
using ARISESLCOM.Models.Domains.interfaces;
using ARISESLCOM.Models.Entities;
using ARISESLCOM.Models.Mappers;
using ARISESLCOM.Services.interfaces;
using Microsoft.AspNetCore.Identity;
using SLCOMLIB.Helpers;
using Microsoft.Extensions.Configuration;

namespace ARISESLCOM.Controllers
{
    public class GerenciaController(IDBContext dBContext,
                                    IRedisCacheService redis,
                                    IGerenciaDomainModel gerenciaDomainModel,
                                    IDestaqueDomainModel destaqueDomainModel,
                                    ISiteApiServices siteApiServices,
                                    IConfiguration configuration) : BasicController(redis)
    {
        private readonly IDBContext _dbContext = dBContext;
        private readonly IGerenciaDomainModel _gerenciaDomainModel = gerenciaDomainModel;
        private readonly IDestaqueDomainModel _destaqueDomainModel = destaqueDomainModel;
        private readonly ISiteApiServices _siteApiServices = siteApiServices;
        private readonly IConfiguration _configuration = configuration;
        private readonly DestaqueViewMapper _destaqueViewMapper = new();

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Check()
        {
            return View();
        }

        public IActionResult CheckResults(string stringg)
        {
            var passwordHasher = new PasswordHasher<object>();
            var hashedPassword = passwordHasher.HashPassword(null, stringg);
            ViewBag.stringg = hashedPassword;
            return View();
        }

        [Authorize]
        public async Task<IActionResult> ConfigGerais()
        {
            DestaqueViewModel? modalViewModel = null;
            _dbContext.GetSqlConnection().Open();
            try
            {
                _gerenciaDomainModel.SetContext(_dbContext);
                var bannerValue = await _gerenciaDomainModel.GetBannerPromoValueDBAsync();
                ViewBag.IsBlackFriday = bannerValue == "BF";

                // Buscar modal de entrada para preview
                _destaqueDomainModel.SetContext(_dbContext);
                var modalModel = await _destaqueDomainModel.GetModalEntradaAsync();
                
                if (modalModel != null)
                {
                    var imageBaseUrl = _configuration["ImagePreview:BaseUrl"];
                    var carouselImagePath = _configuration["ImagePreview:SiteCarouselImagePath"];
                    var fullImageBaseUrl = $"{imageBaseUrl?.TrimEnd('/')}/{carouselImagePath}";
                    modalViewModel = _destaqueViewMapper.MapDestaqueViewModel(modalModel, fullImageBaseUrl);
                }
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
            ViewBag.ModalViewModel = modalViewModel;
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBlackFriday([FromBody] bool isBlackFriday)
        {
            _dbContext.GetSqlConnection().Open();
            try
            {
                _gerenciaDomainModel.SetContext(_dbContext);
                var value = isBlackFriday ? "BF" : "P";
                await _gerenciaDomainModel.UpdateBannerPromoDBAsync(value);

                // Invalidar cache do site
                var cacheInfo = new List<CacheInvalidateRequest>
                {
                    new() { Region = SiteCacheKeyUtil.REGION_BANNERPROMOTION, Key = "1", CleanRegionInd = false }
                };

                await _siteApiServices.InvalidateAsync(cacheInfo);

                return Json(new { success = true, message = "Configura��o atualizada com sucesso!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Erro ao atualizar: {ex.Message}" });
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
        }

        [Authorize]
        public async Task<IActionResult> ModalEntrada()
        {
            DestaqueViewModel? viewModel = null;
            _dbContext.GetSqlConnection().Open();
            try
            {
                _destaqueDomainModel.SetContext(_dbContext);
                var model = await _destaqueDomainModel.GetModalEntradaAsync();
                
                if (model != null)
                {
                    var imageBaseUrl = _configuration["ImagePreview:BaseUrl"];
                    var carouselImagePath = _configuration["ImagePreview:SiteCarouselImagePath"];
                    var fullImageBaseUrl = $"{imageBaseUrl?.TrimEnd('/')}/{carouselImagePath}";
                    viewModel = _destaqueViewMapper.MapDestaqueViewModel(model, fullImageBaseUrl);
                }
            }
            catch (Exception)
            {
                return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
            }
            finally
            {
                await _dbContext.CloseAsync();
            }

            return View(viewModel);
        }

        [Authorize]
        [HttpPost]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> UploadModalEntradaImage(IFormFile file, string? link, int? id = null)
        {
            ActionResultModel resModel;
            bool imageOkInd = true;
            var fileFullPath = "";

            // Se n�o h� arquivo, atualiza apenas o link
            if (file == null || file.Length == 0)
            {
                _dbContext.GetSqlConnection().Open();
                try
                {
                    _destaqueDomainModel.SetContext(_dbContext);
                    
                    DestaqueModel? existingModel = null;
                    
                    // Se tem ID, busca pelo ID, sen�o busca o modal existente
                    if (id.HasValue)
                    {
                        existingModel = await _destaqueDomainModel.GetDestaqueAsync(id.Value);
                    }
                    else
                    {
                        existingModel = await _destaqueDomainModel.GetModalEntradaAsync();
                    }

                    if (existingModel == null)
                    {
                        return Json(new { success = false, message = "Modal n�o encontrado. � necess�rio enviar uma imagem para criar o modal." });
                    }

                    existingModel.Link = link;
                    resModel = await _destaqueDomainModel.UpdateDestaqueAsync(existingModel);

                    if (resModel.IsSuccess)
                    {
                        await InvalidateModalEntradaCacheAsync();
                    }
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
                finally
                {
                    await _dbContext.CloseAsync();
                }

                var modalModel = await _destaqueDomainModel.GetModalEntradaAsync();
                return Json(new
                {
                    success = resModel.IsSuccess,
                    message = resModel.IsSuccess ? "Link do modal atualizado com sucesso" : resModel.Message,
                    fileName = modalModel?.Arquivo ?? ""
                });
            }

            // Valida��o do arquivo
            string[] allowed = [".jpg", ".jpeg", ".png", ".webp"];
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowed.Contains(ext))
            {
                return Json(new { success = false, message = "Formato n�o suportado" });
            }

            _dbContext.GetSqlConnection().Open();
            try
            {
                _destaqueDomainModel.SetContext(_dbContext);

                // Upload image to remote service
                fileFullPath = await _siteApiServices.UploadDestaqueImageToSite(file);
                imageOkInd = !string.IsNullOrEmpty(fileFullPath);

                if (imageOkInd)
                {
                    // Verifica se j� existe modal de entrada
                    var existingModal = await _destaqueDomainModel.GetModalEntradaAsync();

                    if (existingModal != null)
                    {
                        // Update existing
                        existingModal.Arquivo = fileFullPath;
                        existingModal.Link = link;
                        resModel = await _destaqueDomainModel.UpdateDestaqueAsync(existingModal);
                    }
                    else
                    {
                        // Create new
                        var newModel = new DestaqueModel
                        {
                            Tipo = 200, // Modal Entrada
                            Arquivo = fileFullPath,
                            Link = link
                        };
                        resModel = await _destaqueDomainModel.CreateDestaqueAsync(newModel);
                    }

                    if (resModel.IsSuccess)
                    {
                        await InvalidateModalEntradaCacheAsync();
                    }
                }
                else
                {
                    resModel = new ActionResultModel(ActionResultModel.ERROR, "Erro ao enviar imagem");
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            finally
            {
                await _dbContext.CloseAsync();
            }

            bool isSuccess = imageOkInd && resModel.IsSuccess;
            var msg = isSuccess ? "Imagem do modal salva com sucesso" : resModel.Message;

            return Json(new
            {
                success = isSuccess,
                message = msg,
                fileName = fileFullPath
            });
        }

        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> DeleteModalEntrada()
        {
            ActionResultModel resModel;
            _dbContext.GetSqlConnection().Open();
            try
            {
                _destaqueDomainModel.SetContext(_dbContext);
                var modalModel = await _destaqueDomainModel.GetModalEntradaAsync();

                if (modalModel == null)
                {
                    return Json(new { success = false, message = "Modal n�o encontrado" });
                }

                resModel = await _destaqueDomainModel.DeleteDestaqueAsync(modalModel.PKId);

                if (resModel.IsSuccess)
                {
                    await InvalidateModalEntradaCacheAsync();
                }
            }
            catch (Exception ex)
            {
                resModel = new ActionResultModel(ActionResultModel.ERROR, ex.Message);
            }
            finally
            {
                await _dbContext.CloseAsync();
            }

            return Json(new { success = resModel.IsSuccess, message = resModel.Message });
        }

        private async Task InvalidateModalEntradaCacheAsync()
        {
            var cacheInfo = new List<CacheInvalidateRequest>
            {
                new() 
                { 
                    Region = "AliModal", 
                    Key = "", 
                    CleanRegionInd = true 
                }
            };
            await _siteApiServices.InvalidateAsync(cacheInfo);
        }
    }
}
