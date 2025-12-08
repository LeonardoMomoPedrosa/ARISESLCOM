namespace ARISESLCOM.DTO.Api.Request
{
    public class CacheInvalidateRequest
    {
        public string Region { get; set; }
        public string Key { get; set; }
        public bool CleanRegionInd { get; set; }
    }
}


