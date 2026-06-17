using DTOs;
using Models;

namespace DTOs
{
    public class RegisterOrderRequest
    {
        public required Client Client { get; set; }
        public DateTimeOffset ReceivedDate { get; set; } = DateTimeOffset.UtcNow;
        public required Order Order { get; set; }
        public required List<OrderDetail> OrderDetails { get; set; }
        public required PaymentDetailsDTO PaymentDetails { get; set; }


    }
}