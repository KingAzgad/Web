namespace Web.Models
{
    public class UserViewModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Fullname { get; set; }
        public string? Address { get; set; }
        public string? Age { get; set; }
        public string Role { get; set; }
        public Cart Cart { get; set; }
        public List<Order> Orders { get; set; }
    }
}