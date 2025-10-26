using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace bds.Models
{
    public class CommuneWard
    {
        [Key]
        public int CommuneID { get; set; }

        [Required, StringLength(255)]
        public string CommuneName { get; set; } = string.Empty;

        // Khóa ngoại
        public int DistrictID { get; set; }

        [ForeignKey("DistrictID")]
        public District District { get; set; }

        // Quan hệ ngược
        public ICollection<Project>? Projects { get; set; }
        public ICollection<Post>? Posts { get; set; }
    }
}
