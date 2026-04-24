namespace QLS.Backend.DTOs.Machine
{
    public class ConfirmPaymentRequestDto
    {
        /// <summary>
        /// Mã giao dịch từ payment gateway (VNPay, Momo, v.v.).
        /// Nullable vì một số phương thức thanh toán (tiền mặt/xu) không có transaction ID.
        /// </summary>
        public string? TransactionId { get; set; }
    }
}
