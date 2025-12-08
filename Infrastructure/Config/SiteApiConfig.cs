namespace ARISESLCOM.Infrastructure.Config
{
    public class SiteApiConfig
    {
        public List<SiteApiServer> Servers { get; set; } = new();
        public string AuthPath { get; set; }
        public string InvalidateApi { get; set; }
        public string ImageUploadApi { get; set; }
        public string DestaqueImageUploadApi { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int TokenCacheMinutes { get; set; }
        
    }
}
