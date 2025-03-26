namespace Web.Models
{
    public class Cart
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public List<CartItem> Items { get; set; } = new List<CartItem>();

        public decimal GetTotal()
        {
            return Items.Sum(i => i.ProductPrice * i.Quantity);
        }
    }
}