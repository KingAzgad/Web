    namespace Web.Models
    {
        public class Order
        {
            public int Id { get; set; }
            public string UserId { get; set; }
            public ApplicationUser User { get; set; }
            public DateTime OrderDate { get; set; }
            public decimal TotalAmount { get; set; }
            public string Status { get; set; }
            public List<OrderItem> Items { get; set; } = new List<OrderItem>();
        }
    }