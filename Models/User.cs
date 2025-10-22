
using Microsoft.Extensions.Hosting;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

namespace bds.Models
{
    [Table("User")]
    public class User
    {
        [Key]
        public int UserID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Preferences { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.Now;
        public int? RoleID { get; set; }
        public bool IsSuperAdmin { get; set; } = false;      //IsSuperAdmin = 1 : chủ trang web

        //QUan he
        public Role? Role { get; set; }
        public ICollection<Post>? Posts { get; set; }
        public ICollection<Project>? Projects { get; set; }
        public ICollection<SearchHistory>? SearchHistories { get; set; }
        public ICollection<News>? NewsList { get; set; }
    }
}
