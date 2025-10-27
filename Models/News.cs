using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace bds.Models
{
    public class News
    {
        [Key]
        public int NewsID { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Content { get; set; }

        public DateTime CreateAt { get; set; } = DateTime.Now;

        [ForeignKey("User")]
        public int UserID { get; set; }

        // Quan hệ
        public User? User { get; set; }

        public ICollection<Image>? Images { get; set; }
        public int ViewCount { get; set; } = 0;
    }
}
