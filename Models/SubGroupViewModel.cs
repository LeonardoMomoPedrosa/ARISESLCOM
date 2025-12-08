using ARISESLCOM.Models.Entities;

namespace ARISESLCOM.Models
{
    public class SubGroupViewModel
    {
        public ProductSubTypeModel NewModel { get; set; }
        public int IdTipo { get; set; }
        public List<ProductSubTypeModel> SubGroupList { get; set; }
    }
}
