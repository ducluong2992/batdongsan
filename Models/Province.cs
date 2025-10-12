using System.ComponentModel.DataAnnotations;

namespace bds.Models
{
    public class Province
    {
        [Key]
        public int ProvinceID { get; set; }
        public string ProvinceName { get; set; } = string.Empty;

        //  Quan hệ
        public ICollection<CommuneWard>? CommuneWards { get; set; }
        public ICollection<Statistic>? Statistics { get; set; }
    }
}
