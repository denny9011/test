namespace project.Models.Request
{
    public class PaymentResponseDto
    {
        public ResponseInfoDto Info { get; set; }
        public string ReturnCode { get; set; }
        public string ReturnMessage { get; set; }
    }

    public class ResponseInfoDto
    {
        public string PaymentAccessToken { get; set; }
        public ResponsePaymentUrlDto PaymentUrl { get; set; }
        public string TransactionId { get; set; }
    }

    public class ResponsePaymentUrlDto
    {
        public string App { get; set; }
        public string Web { get; set; }
    }
}