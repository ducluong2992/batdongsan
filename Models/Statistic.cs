using System.ComponentModel.DataAnnotations;

namespace bds.Models
{
    public class Statistic
    {
        [Key]
        public int StatisticID { get; set; }
        public int ViewCount { get; set; } = 0;
        public int PostCount { get; set; } = 0;
        public DateTime CreateAt { get; set; } = DateTime.Now;
        public int? ProvinceID { get; set; }

        //  Quan hệ
        public Province? Province { get; set; }
    }
}
