using System.Collections.Generic;

namespace project.Models.Request
{
    public class PaymentProduct
    {
        public string ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class PaymentRequestParameter
    {
        public string OrderId { get; set; }

        public List<PaymentProduct> Products { get; set; }
    }

    public class RedirectUrlsDto
    {
        public string CancelUrl { get; set; }
        public string ConfirmUrl { get; set; }
    }
}