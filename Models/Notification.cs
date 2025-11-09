using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace bds.Models
{
    [Table("Notification")]
    public class Notification
    {
        [Key]
        public int NotificationID { get; set; }

        public int UserID { get; set; } // Người nhận thông báo

        [StringLength(255)]
        public string? Title { get; set; }

        public string? Message { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Liên kết đến bài đăng nếu có
        public int? PostID { get; set; }
        [ForeignKey(nameof(PostID))]
        public virtual Post? Post { get; set; }

        public int? ProjectID { get; set; }

        [ForeignKey(nameof(ProjectID))]
        public virtual Project? Project { get; set; }


        [ForeignKey(nameof(UserID))]
        public virtual User? User { get; set; }
    }
}

