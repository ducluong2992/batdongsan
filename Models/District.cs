using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace bds.Models
{
    public class District
    {
        [Key]
        public int DistrictID { get; set; }

        [Required, StringLength(255)]
        public string DistrictName { get; set; }

        // Khóa ngoại liên kết với Province
        public int ProvinceID { get; set; }

        [ForeignKey("ProvinceID")]
        public Province Province { get; set; }

        // Danh sách xã/phường thuộc quận này
        public ICollection<CommuneWard> CommuneWards { get; set; }
    }
}
