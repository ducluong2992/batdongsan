using System.ComponentModel.DataAnnotations;
using static System.Net.Mime.MediaTypeNames;

namespace bds.Models
{
    public class News
    {
        [Key]
        public int NewsID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Content { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.Now;
        public int Author { get; set; }

        //  Quan hệ
        public User? User { get; set; }
        public ICollection<Image>? Images { get; set; }
    }
}
