using ARISESLCOM.Services.interfaces;

namespace ARISESLCOM.Models.Domains.DB
{
    public class FreteDB (IRedisCacheService redis) : DBDomain (redis)
    {
    }
}
