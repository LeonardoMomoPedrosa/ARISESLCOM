using Azure;
using ARISESLCOM.Models.Domains.DB;
using ARISESLCOM.Models.Domains.interfaces;
using ARISESLCOM.Models.Entities;
using ARISESLCOM.Services.interfaces;

namespace ARISESLCOM.Models.Domains
{
    public class GroupDomainModel(IRedisCacheService redis,
                                    IProductDomainModel productDomainModel) : GroupDB(redis), IGroupDomainModel
    {
        private readonly IProductDomainModel _productDomainModel = productDomainModel;

        public async Task<ActionResultModel> DeleteGroupAsync(int pkid)
        {
            _productDomainModel.SetContext(_dbContext);

            ActionResultModel resultModel = new(ActionResultModel.SUCCESS, "");

            var productList = await _productDomainModel.GetProductListByTypeDBAsync(pkid);

            if (pkid == 8 || pkid == 19 || pkid == 20 || pkid == 21 || pkid == 54)
            {
                resultModel.SetError("Esse grupo n�o pode ser removido");
            }
            else if (productList != null && productList.Count > 0)
            {
                resultModel.SetError("Este grupo cont�m produtos.");
            } else
            {
                resultModel = await DeleteGroupDBAsync(pkid);
            }

            return resultModel;
        }

        public async Task<ActionResultModel> DeleteSubGroupAsync(int typeId, int subTypeId)
        {
            _productDomainModel.SetContext(_dbContext);

            ActionResultModel resultModel = new(ActionResultModel.SUCCESS, "");

            var productList = await _productDomainModel.GetProductListBySubTypeAsync(typeId, subTypeId);

            if (productList != null && productList.Count > 0)
            {
                resultModel.SetError("Este sub grupo cont�m produtos.");
            }
            else
            {
                resultModel = await DeleteSubGroupDBAsync(typeId, subTypeId);
            }

            return resultModel;
        }
    }
}
