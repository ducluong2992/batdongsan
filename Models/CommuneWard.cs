using System.ComponentModel.DataAnnotations;

namespace bds.Models
{
    public class CommuneWard
    {
        [Key]
        public int CommuneID { get; set; }
        public string CommuneName { get; set; } = string.Empty;
        public int? ProvinceID { get; set; }

        //  Quan hệ
        public Province? Province { get; set; }
        public ICollection<Project>? Projects { get; set; }
        public ICollection<Post>? Posts { get; set; }
    }
}
