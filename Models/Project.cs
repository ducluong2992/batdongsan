using System.ComponentModel.DataAnnotations;
using static System.Net.Mime.MediaTypeNames;

namespace bds.Models
{
    public class Project
    {
        [Key]
        public int ProjectID { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public double? Area { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Status { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.Now;

        public int? UserID { get; set; }
        public int? CommuneID { get; set; }

        //  Quan hệ
        public User? User { get; set; }
        public CommuneWard? CommuneWard { get; set; }
        public ICollection<Image>? Images { get; set; }
    }
}
