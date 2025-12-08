namespace ARISESLCOM.DTO.Api.Response
{
    public class SiteImageUploadResult
    {
        public bool Success { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public long FileSize { get; set; }
    }
}
