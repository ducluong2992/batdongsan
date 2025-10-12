using System.ComponentModel.DataAnnotations;

namespace bds.Models
{
    public class SearchHistory
    {
        [Key]
        public int SearchID { get; set; }
        public string? KeyWord { get; set; }
        public int? Province { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public double? MinArea { get; set; }
        public double? MaxArea { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.Now;

        public int? UserID { get; set; }

        //  Quan hệ
        public User? User { get; set; }
        public Province? ProvinceObj { get; set; }
    }
}
