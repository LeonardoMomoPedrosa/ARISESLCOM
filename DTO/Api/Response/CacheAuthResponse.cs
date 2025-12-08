namespace ARISESLCOM.DTO.Api.Response
{
    public class CacheAuthResponse
    {
        public string Token { get; set; }
        public string TokenType { get; set; }
        public int ExpiresIn { get; set; }
    }
}


