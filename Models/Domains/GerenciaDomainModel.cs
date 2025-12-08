using ARISESLCOM.Models.Domains.DB;
using ARISESLCOM.Models.Domains.interfaces;
using ARISESLCOM.Services.interfaces;

namespace ARISESLCOM.Models.Domains
{
    public class GerenciaDomainModel(IRedisCacheService redis) : GerenciaDB(redis), IGerenciaDomainModel
    {
    }
}
