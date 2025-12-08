namespace ARISESLCOM.Models
{
    public class ActionResultModel(string status, string message)
    {
        public ActionResultModel() : this(SUCCESS, "")
        {
        }
        public static readonly string SUCCESS = "SUCCESS";
        public static readonly string WARNING = "WARNING";
        public static readonly string ERROR = "ERROR";

        public static readonly string BUTTON_TYPE_CONTIUE = "CONT";
        public static readonly string BUTTON_TYPE_BACK = "BACK";

        public string Status { get; set; } = status;
        public string Message { get; set; } = message;
        public string ButtonType { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public string Param { get; set; }
        public string ParamName { get; set; }

        public bool IsSuccess { get { return Status.Equals(SUCCESS); } }
        public void SetSuccess() { Status = SUCCESS; }
        public void SetWarning() { Status = WARNING; }
        public void SetError() { Status = ERROR; }

        public void SetError(string message)
        {
            Status = ERROR;
            Message = message;
        }

        public void SetSuccess(string message)
        {
            Status = SUCCESS;
            Message = message;
        }
    }
}