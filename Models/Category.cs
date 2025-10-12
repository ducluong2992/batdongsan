using System.ComponentModel.DataAnnotations;

namespace bds.Models
{
    public class Category
    {
        [Key]
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = string.Empty;

        //  Quan hệ
        public ICollection<Post>? Posts { get; set; }
    }
}
