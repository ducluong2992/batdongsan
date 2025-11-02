using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace bds.Models
{
    public class Prefered
    {
        [Key]
        public int PreferedID { get; set; }

        public int UserID { get; set; }
        public int? PostID { get; set; }
        public int? ProjectID { get; set; }

        [ForeignKey("UserID")]
        public virtual User User { get; set; }

        [ForeignKey("PostID")]
        public virtual Post? Post { get; set; }

        [ForeignKey("ProjectID")]
        public virtual Project? Project { get; set; }
    }
}
