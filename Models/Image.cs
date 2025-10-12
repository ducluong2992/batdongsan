using System.ComponentModel.DataAnnotations;

namespace bds.Models
{
    public class Image
    {
        [Key]
        public int ImageID { get; set; }
        public string ImageUrl { get; set; } = string.Empty;

        public int? PostID { get; set; }
        public int? ProjectID { get; set; }
        public int? NewsID { get; set; }

        //  Quan hệ
        public Post? Post { get; set; }
        public Project? Project { get; set; }
        public News? News { get; set; }
    }
}
