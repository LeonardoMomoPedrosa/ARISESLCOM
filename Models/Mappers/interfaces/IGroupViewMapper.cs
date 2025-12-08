using ARISESLCOM.Models.Entities;

namespace ARISESLCOM.Models.Mappers.interfaces
{
    public interface IGroupViewMapper
    {
        public SubGroupViewModel MapSubGroupViewModel(int idTipo, List<ProductSubTypeModel> inModelList)
        {
            return new()
            {
                SubGroupList = inModelList,
                IdTipo = idTipo
            };
        }
    }
}
