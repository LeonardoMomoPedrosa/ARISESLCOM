using ARISESLCOM.Data;

namespace ARISESLCOM.Models.Domains.interfaces
{
    public interface IDomainModel
    {
        public void SetContext(IDBContext dBContext);
    }
}