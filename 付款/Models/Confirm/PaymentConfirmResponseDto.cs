namespace project.Models.Confirm
{
    public class ConfirmResponseInfoDto
    {
        public string OrderId { get; set; }
        public ConfirmResponsePayInfoDto[] PayInfo { get; set; }
        public long TransactionId { get; set; }
    }

    public class ConfirmResponsePackageDto
    {
        public int Amount { get; set; }
        public string Id { get; set; }
        public int UserFeeAmount { get; set; }
    }

    public class ConfirmResponsePayInfoDto
    {
        public int Amount { get; set; }
        public string CreditCardBrand { get; set; }
        public string CreditCardNickname { get; set; }
        public string MaskedCreditCardNumber { get; set; }
        public string Method { get; set; }
        public ConfirmResponsePackageDto[] Packages { get; set; }
        public ConfirmResponseShippingOptionsDto Shipping { get; set; }
    }

    public class ConfirmResponseShippingOptionsDto
    {
        public int FeeAmount { get; set; }
        public string MethodId { get; set; }
    }

    public class PaymentConfirmResponseDto
    {
        public ConfirmResponseInfoDto Info { get; set; }
        public string ReturnCode { get; set; }
        public string ReturnMessage { get; set; }
    }
}