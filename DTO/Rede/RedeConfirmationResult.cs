namespace ARISESLCOM.DTO.Rede
{
    public class RedeConfirmationResult
    {
        public bool Success { get; set; }
        public DateTime RequestDateTime { get; set; }
        public RedeAuthorization Authorization { get; set; } = new RedeAuthorization();
        public string ErrorCode { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
