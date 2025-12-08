namespace ARISESLCOM.DTO.Api.Request
{
    public class ChangeOrderRequest
    {
        public int OrderId { get; set; }
        public string NewStatus { get; set; }
    }
}
