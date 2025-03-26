namespace Web.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string UserId { get; set; } // Liên kết với ApplicationUser
        public ApplicationUser User { get; set; }
        public DateTime OrderDate { get; set; } // Ngày đặt hàng
        public decimal TotalAmount { get; set; } // Tổng tiền
        public string Status { get; set; } // Trạng thái đơn hàng (ví dụ: "Pending", "Completed", "Cancelled")
        public List<OrderItem> Items { get; set; } = new List<OrderItem>(); // Danh sách sản phẩm trong đơn hàng
    }
}