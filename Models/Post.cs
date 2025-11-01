using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static System.Net.Mime.MediaTypeNames;

namespace bds.Models
{
    [Table("Post")]
    public class Post
    {
        [Key]
        public int PostID { get; set; }


        [Required(ErrorMessage = "Vui lòng nhập tiêu đề bài viết")]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [StringLength(255)]
        public string? Location { get; set; }

        public double? Area { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Price { get; set; }

        public int? ClickCount { get; set; } = 0;

        [StringLength(50)]
        public string? Status { get; set; }

        public DateTime CreateAt { get; set; } = DateTime.Now;
        [StringLength(255)]
        public string? RejectReason { get; set; }

        // --- KHÓA NGOẠI ---
        [ForeignKey(nameof(User))]


        public int? UserID { get; set; }

        [ForeignKey(nameof(CommuneWard))]
        public int? CommuneID { get; set; }

        [ForeignKey(nameof(Category))]
        public int? CategoryID { get; set; }

//<<<<<<< HEAD
        // --- QUAN HỆ ---
        public virtual User? User { get; set; }
        public virtual CommuneWard? CommuneWard { get; set; }
        public virtual Category? Category { get; set; }

        // Một bài đăng có thể có nhiều ảnh
        public virtual ICollection<Image>? Images { get; set; }
//=======
//        //  Quan hệ
//        public User? User { get; set; }

//        [ForeignKey("CommuneID")]
//        public CommuneWard? CommuneWard { get; set; }
//        public Category? Category { get; set; }
//        public ICollection<Image>? Images { get; set; }
//>>>>>>> origin/feature/admin
    }
}
