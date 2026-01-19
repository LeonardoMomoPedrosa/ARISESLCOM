using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
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
using System.Drawing;

namespace ARISESLCOM.Controllers
{
    [Authorize]
    public class GroupController(IDBContext dBContext,
                                   IGroupDomainModel groupDomainModel,
                                   IGroupViewMapper groupViewMapper,
                                   IRedisCacheService redis,
                                    ISiteApiServices siteCache) : BasicController(redis)
    {
        private readonly IDBContext _dbContext = dBContext;
        private readonly IGroupDomainModel _groupDomainModel = groupDomainModel;
        private readonly IGroupViewMapper _groupViewMapper = groupViewMapper;
        private readonly ISiteApiServices _siteCache = siteCache;

        [HttpPatch]
        public async Task<IActionResult> PatchGroup([FromBody] ProductTypeModel model)
        {
            _dbContext.GetSqlConnection().Open();
            ActionResultModel resModel;
            try
            {
                _groupDomainModel.SetContext(_dbContext);

                resModel = await _groupDomainModel.UpdateGroupDBAsync(model);

                var cacheInfo = new List<CacheInvalidateRequest>
                {
                    new() { Region = SiteCacheKeyUtil.REGION_DRYCATEGORYMODELS, Key = "", CleanRegionInd = true },
                    new() { Region = SiteCacheKeyUtil.REGION_DRYCATALOGDESC, Key = $"{model.PKId}", CleanRegionInd = false },
                    new() { Region = SiteCacheKeyUtil.REGION_MENU, Key = "", CleanRegionInd = true }
                };

                cacheSuccessInd = await _siteCache.InvalidateAsync(cacheInfo);
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

        [HttpPatch]
        public async Task<IActionResult> PatchSubGroup([FromBody] ProductSubTypeModel model)
        {
            _dbContext.GetSqlConnection().Open();
            ActionResultModel resModel;

            try
            {
                _groupDomainModel.SetContext(_dbContext);

                resModel = await _groupDomainModel.UpdateSubGroupDBAsync(model);

                var cacheInfo = new List<CacheInvalidateRequest>
                {
                    new() { Region = SiteCacheKeyUtil.REGION_DRYCATEGORYMODELS, Key = "", CleanRegionInd = true },
                    new() { Region = SiteCacheKeyUtil.REGION_FISHCATALOGDESC, Key = $"{model.PKId}", CleanRegionInd = false },
                    new() { Region = SiteCacheKeyUtil.REGION_MENU, Key = "", CleanRegionInd = true }
                };

                cacheSuccessInd = await _siteCache.InvalidateAsync(cacheInfo);
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
        public async Task<IActionResult> DeleteGroup(int id)
        {
            _dbContext.GetSqlConnection().Open();
            ActionResultModel resModel;
            try
            {
                _groupDomainModel.SetContext(_dbContext);

                resModel = await _groupDomainModel.DeleteGroupAsync(id);

                var cacheInfo = new CacheInvalidateRequest() { Region = SiteCacheKeyUtil.REGION_MENU, Key = "", CleanRegionInd = true };
                cacheSuccessInd = await _siteCache.InvalidateAsync(cacheInfo);
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

        [HttpDelete]
        public async Task<IActionResult> DeleteSubGroup(int typeId, int subTypeId)
        {
            _dbContext.GetSqlConnection().Open();
            ActionResultModel resModel;
            try
            {
                _groupDomainModel.SetContext(_dbContext);

                resModel = await _groupDomainModel.DeleteSubGroupAsync(typeId, subTypeId);

                var cacheInfo = new CacheInvalidateRequest() { Region = SiteCacheKeyUtil.REGION_MENU, Key = "", CleanRegionInd = true };
                cacheSuccessInd = await _siteCache.InvalidateAsync(cacheInfo);
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

        [HttpPost]
        public async Task<IActionResult> New(string tipo, string descricao)
        {
            _dbContext.GetSqlConnection().Open();
            ActionResultModel resModel;
            try
            {
                _groupDomainModel.SetContext(_dbContext);

                resModel = await _groupDomainModel.NewGroupDBAsync(tipo, descricao);
                resModel.ButtonType = ActionResultModel.BUTTON_TYPE_CONTIUE;
                resModel.Controller = "/Product";
                resModel.Action = "EditGroup";
            }
            catch (Exception ex)
            {
                resModel = new(ActionResultModel.ERROR, ex.Message);
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
            return View("Message", resModel);
        }

        [HttpPost]
        public async Task<IActionResult> NewSub(SubGroupViewModel model)
        {
            _dbContext.GetSqlConnection().Open();
            ActionResultModel resModel;
            try
            {
                _groupDomainModel.SetContext(_dbContext);
                model.NewModel.TypeId = model.IdTipo;
                resModel = await _groupDomainModel.NewSubGroupDBAsync(model.NewModel);
                resModel.ButtonType = ActionResultModel.BUTTON_TYPE_CONTIUE;
                resModel.Controller = "/Group";
                resModel.Action = "EditSubGroup";
                resModel.ParamName = "id";
                resModel.Param = model.IdTipo + "";
            }
            catch (Exception ex)
            {
                resModel = new(ActionResultModel.ERROR, ex.Message);
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
            return View("Message", resModel);
        }

        [HttpGet]
        public async Task<IActionResult> EditSubGroup(int id)
        {
            _dbContext.GetSqlConnection().Open();
            _groupDomainModel.SetContext(_dbContext);
            List<ProductSubTypeModel> resModel;
            try
            {
                resModel = await _groupDomainModel.GetProductSubTypeModelsAsync(id);
            }
            finally
            {
                await _dbContext.CloseAsync();
            }
            return View(_groupViewMapper.MapSubGroupViewModel(id, resModel));
        }

    }
}
