namespace Order.Domain.Enums
{
    public enum OrderStatus
    {
        Pending = 1,        // Chờ xác nhận
        Confirmed = 2,      // Đã xác nhận
        Processing = 3,     // Đang xử lý
        Shipped = 4,        // Đã gửi hàng
        Delivered = 5,      // Đã giao hàng
        Completed = 6,      // Hoàn thành
        Canceled = 7,       // Đã hủy
        Returned = 8,       // Đã trả hàng
        PaymentPending = 9, // Chờ thanh toán
        PaymentFailed = 10  // Thanh toán thất bại
    }
}
