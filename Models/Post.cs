using System.ComponentModel.DataAnnotations;
using static System.Net.Mime.MediaTypeNames;

namespace bds.Models
{
    public class Post
    {
        [Key]
        public int PostID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public double? Area { get; set; }
        public decimal? Price { get; set; }
        public int ClickCount { get; set; } = 0;
        public string? Status { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.Now;

        public int? UserID { get; set; }
        public int? CommuneID { get; set; }
        public int? CategoryID { get; set; }

        //  Quan hệ
        public User? User { get; set; }
        public CommuneWard? CommuneWard { get; set; }
        public Category? Category { get; set; }
        public ICollection<Image>? Images { get; set; }
    }
}
