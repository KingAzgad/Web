using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
namespace Web.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Fullname { get; set; }
        public string? Address { get; set; }
        public string? Age { get; set; }
    }
}
