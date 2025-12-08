using ARISESLCOM.Models.Entities;

namespace ARISESLCOM.Models.Mappers.interfaces
{
    public interface IProductViewMapper
    {
        public ProductViewModel MapProductViewModel(ProductModel productModel);
        public ProductModel MapProductModel(ProductViewModel productViewModel);
        List<ProductViewModel> MapProductViewModelList(List<ProductModel> productModelList);
    }
}