using ARISESLCOM.Models.Entities;

namespace ARISESLCOM.Models
{
    public class NewProductViewModel
    {
        public int IdTipo { get; set; }
        public ProductModel ProductModel { get; set; }
        public List<ProductSubTypeModel> SubTypeList { get; set; }
    }
}
