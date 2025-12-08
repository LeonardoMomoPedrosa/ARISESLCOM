using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ARISESLCOM.Data;
using ARISESLCOM.DTO.Api.Request;
using ARISESLCOM.Infrastructure.Config;
using ARISESLCOM.Models;
using ARISESLCOM.Models.Domains.interfaces;
using ARISESLCOM.Models.Entities;
using ARISESLCOM.Models.Mappers;
using ARISESLCOM.Services.interfaces;
using SLCOMLIB.Helpers;

namespace ARISESLCOM.Controllers
{
    [Authorize]
    public class ContentController(IDBContext dBContext,
                                   IDestaqueDomainModel destaqueDomainModel,
                                   ISiteApiServices siteApi,
                                   IRedisCacheService redis,
                                   IOptions<SiteApiConfig> siteApiConfig,
                                   IConfiguration configuration) : BasicController(redis)
    {
        private readonly IDBContext _dbContext = dBContext;
        private readonly IDestaqueDomainModel _destaqueDomainModel = destaqueDomainModel;
        private readonly ISiteApiServices _siteApi = siteApi;
        private readonly DestaqueViewMapper _destaqueViewMapper = new();
        private readonly SiteApiConfig _siteApiConfig = siteApiConfig.Value;
        private readonly IConfiguration _configuration = configuration;

        private async Task InvalidateDestaqueCacheAsync()
        {
            var cacheInfo = new List<CacheInvalidateRequest>
            {
                new() 
                { 
                    Region = SiteCacheKeyUtil.REGION_DESTAQUE, 
                    Key = "", 
                    CleanRegionInd = true 
                }
            };
            await _siteApi.InvalidateAsync(cacheInfo);
        }

        public async Task<IActionResult> Carousel()
        {
            List<DestaqueModel> model;
            _dbContext.GetSqlConnection().Open();
            try
            {
                _destaqueDomainModel.SetContext(_dbContext);
                model = await _destaqueDomainModel.GetDestaqueListByTipoAsync(1); // Carousel tipo = 1
            }
            finally
            {
                await _dbContext.CloseAsync();
            }

            var imageBaseUrl = configuration["ImagePreview:BaseUrl"];
            var carouselImagePath = configuration["ImagePreview:SiteCarouselImagePath"];
            var fullImageBaseUrl = $"{imageBaseUrl?.TrimEnd('/')}/{carouselImagePath}";

            return View(_destaqueViewMapper.MapDestaqueViewModelList(model, fullImageBaseUrl));
        }

        [HttpPost]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> UploadCarouselImage(int? id, IFormFile file, string? link)
        {
            ActionResultModel resModel;
            bool imageOkInd = true; // Assume success if no file is provided
            var fileFullPath = "";

            // Se n�o h� arquivo mas h� ID, � uma atualiza��o apenas do link
            if ((file == null || file.Length == 0) && id.HasValue)
            {
                _dbContext.GetSqlConnection().Open();
                try
                {
                    _destaqueDomainModel.SetContext(_dbContext);
                    
                    // Update existing item with new link only
                    var existingModel = await _destaqueDomainModel.GetDestaqueAsync(id.Value);
                    existingModel.Link = link;
                    resModel = await _destaqueDomainModel.UpdateDestaqueAsync(existingModel);

                    if (resModel.IsSuccess)
                    {
                        await InvalidateDestaqueCacheAsync();
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

                var carouselModel = await _destaqueDomainModel.GetDestaqueAsync(id.Value);
                return Json(new
                {
                    success = resModel.IsSuccess,
                    message = resModel.IsSuccess ? "Link do carrossel atualizado com sucesso" : resModel.Message,
                    fileName = carouselModel.Arquivo // Keep existing file
                });
            }

            // Se n�o h� arquivo e n�o h� ID, � um erro
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "Arquivo inv�lido" });
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

                // Upload image to remote service using UploadDestaqueImage API
                fileFullPath = await _siteApi.UploadDestaqueImageToSite(file);
                imageOkInd = !string.IsNullOrEmpty(fileFullPath);

                if (imageOkInd)
                {
                    if (id.HasValue)
                    {
                        // Update existing
                        var existingModel = await _destaqueDomainModel.GetDestaqueAsync(id.Value);
                        existingModel.Arquivo = fileFullPath;
                        existingModel.Link = link;
                        resModel = await _destaqueDomainModel.UpdateDestaqueAsync(existingModel);
                    }
                    else
                    {
                        // Create new
                        var newModel = new DestaqueModel
                        {
                            Tipo = 1, // Carousel
                            Arquivo = fileFullPath,
                            Link = link
                        };
                        resModel = await _destaqueDomainModel.CreateDestaqueAsync(newModel);
                    }

                    if (resModel.IsSuccess)
                    {
                        await InvalidateDestaqueCacheAsync();
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
            var msg = isSuccess ? "Imagem do carrossel salva com sucesso" : resModel.Message;

            return Json(new
            {
                success = isSuccess,
                message = msg,
                fileName = fileFullPath
            });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCarouselItem([FromBody] DestaqueViewModel viewModel)
        {
            ActionResultModel resModel;
            _dbContext.GetSqlConnection().Open();
            try
            {
                _destaqueDomainModel.SetContext(_dbContext);
                var model = _destaqueViewMapper.MapDestaqueModel(viewModel);
                resModel = await _destaqueDomainModel.UpdateDestaqueAsync(model);

                // Invalidate cache after successful update
                if (resModel.IsSuccess)
                {
                    await InvalidateDestaqueCacheAsync();
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

        [HttpDelete]
        public async Task<IActionResult> DeleteCarouselItem(int id)
        {
            ActionResultModel resModel;
            _dbContext.GetSqlConnection().Open();
            try
            {
                _destaqueDomainModel.SetContext(_dbContext);
                resModel = await _destaqueDomainModel.DeleteDestaqueAsync(id);

                // Invalidate cache after successful deletion
                if (resModel.IsSuccess)
                {
                    await InvalidateDestaqueCacheAsync();
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

        public async Task<IActionResult> Mosaic()
        {
            List<DestaqueModel> model;
            _dbContext.GetSqlConnection().Open();
            try
            {
                _destaqueDomainModel.SetContext(_dbContext);
                model = await _destaqueDomainModel.GetMosaicItemsAsync();
            }
            catch (Exception)
            {
                return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
            }
            finally
            {
                await _dbContext.CloseAsync();
            }

            var imageBaseUrl = _configuration["ImagePreview:BaseUrl"];
            var carouselImagePath = _configuration["ImagePreview:SiteCarouselImagePath"];
            var fullImageBaseUrl = $"{imageBaseUrl?.TrimEnd('/')}/{carouselImagePath}";
            var viewModelList = _destaqueViewMapper.MapDestaqueViewModelList(model, fullImageBaseUrl);

            // Create a 3x8 grid structure
            var mosaicGrid = new List<List<DestaqueViewModel>>();
            for (int row = 0; row < 8; row++)
            {
                var rowItems = new List<DestaqueViewModel>();
                for (int col = 0; col < 3; col++)
                {
                    var frequency = GetFrequencyForPosition(row, col);
                    var item = viewModelList.FirstOrDefault(x => x.Frequencia3 == frequency);
                    rowItems.Add(item ?? new DestaqueViewModel { Frequencia3 = frequency });
                }
                mosaicGrid.Add(rowItems);
            }

            return View(mosaicGrid);
        }

        [HttpPost]
        public async Task<IActionResult> UploadMosaicImage(int row, int col, IFormFile file, string link, int? id = null)
        {
            ActionResultModel resModel;
            string fileFullPath = "";
            bool imageOkInd = true; // Assume success if no file is provided

            _dbContext.GetSqlConnection().Open();
            try
            {
                _destaqueDomainModel.SetContext(_dbContext);
                var frequency = GetFrequencyForPosition(row, col);

                // Se n�o h� arquivo mas h� ID, � uma atualiza��o apenas do link
                if ((file == null || file.Length == 0) && id.HasValue)
                {
                    var existingItem = await _destaqueDomainModel.GetDestaqueAsync(id.Value);
                    existingItem.Link = link;
                    resModel = await _destaqueDomainModel.UpdateDestaqueAsync(existingItem);

                    if (resModel.IsSuccess)
                    {
                        await InvalidateDestaqueCacheAsync();
                    }

                    return Json(new
                    {
                        success = resModel.IsSuccess,
                        message = resModel.IsSuccess ? "Link do mosaico atualizado com sucesso" : resModel.Message,
                        fileName = existingItem.Arquivo // Keep existing file
                    });
                }

                // Se n�o h� arquivo e n�o h� ID, � um erro
                if (file == null || file.Length == 0)
                {
                    return Json(new { success = false, message = "Arquivo inv�lido" });
                }

                // Upload image to remote service using UploadDestaqueImage API
                fileFullPath = await _siteApi.UploadDestaqueImageToSite(file);
                imageOkInd = !string.IsNullOrEmpty(fileFullPath);

                if (imageOkInd)
                {
                    // Check if item already exists for this position
                    var existingItems = await _destaqueDomainModel.GetMosaicItemsAsync();
                    var existingItem = existingItems.FirstOrDefault(x => x.Frequencia3 == frequency);

                    if (existingItem != null)
                    {
                        // Update existing item
                        existingItem.Arquivo = fileFullPath;
                        existingItem.Link = link;
                        resModel = await _destaqueDomainModel.UpdateDestaqueAsync(existingItem);
                    }
                    else
                    {
                        // Create new item
                        var newItem = new DestaqueModel
                        {
                            Tipo = 100, // Mosaic type
                            Arquivo = fileFullPath,
                            Link = link,
                            Frequencia3 = frequency
                        };
                        resModel = await _destaqueDomainModel.CreateDestaqueAsync(newItem);
                    }

                    if (resModel.IsSuccess)
                    {
                        await InvalidateDestaqueCacheAsync();
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
            var msg = isSuccess ? "Imagem do mosaico salva com sucesso" : resModel.Message;

            return Json(new
            {
                success = isSuccess,
                message = msg,
                fileName = fileFullPath
            });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateMosaicItem([FromBody] DestaqueViewModel viewModel)
        {
            ActionResultModel resModel;
            _dbContext.GetSqlConnection().Open();
            try
            {
                _destaqueDomainModel.SetContext(_dbContext);
                var model = _destaqueViewMapper.MapDestaqueModel(viewModel);
                resModel = await _destaqueDomainModel.UpdateDestaqueAsync(model);

                if (resModel.IsSuccess)
                {
                    await InvalidateDestaqueCacheAsync();
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

        private static decimal GetFrequencyForPosition(int row, int col)
        {
            // Convert 0-based row/col to 1-based
            int actualRow = row + 1;
            int actualCol = col + 1;

            return actualRow switch
            {
                1 => actualCol, // 1, 2, 3
                2 => actualCol + 3, // 4, 5, 6
                3 => actualCol + 6, // 7, 8, 9
                4 => actualCol == 1 ? 9.1m : actualCol == 2 ? 9.2m : 9.3m, // 9.1, 9.2, 9.3
                5 => actualCol + 9, // 10, 11, 12
                6 => actualCol + 12, // 13, 14, 15
                7 => actualCol + 15, // 16, 17, 18
                8 => actualCol == 1 ? 18.1m : actualCol == 2 ? 18.2m : 18.3m, // 18.1, 18.2, 18.3
                _ => 0
            };
        }
    }
}