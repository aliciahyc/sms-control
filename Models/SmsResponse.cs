namespace SmsControl.Models
{
    public class SmsResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public double? Count { get; set; }
    }
}