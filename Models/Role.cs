using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace bds.Models
{
    [Table("Role")]
    public class Role
    {
        [Key]
        public int RoleID { get; set; }
        public string RoleName { get; set; } = string.Empty;

        //  Quan hệ
        public ICollection<User>? Users { get; set; }
    }
}
