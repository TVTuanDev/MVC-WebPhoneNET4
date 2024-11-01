namespace WebPhone.Models
{
    public class StatusResult
    {
        public bool Succeeded { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
        public dynamic Data { get; set; }
    }
}