using System.ComponentModel.DataAnnotations;

namespace bds.Models
{
    public class Role
    {
        [Key]
        public int RoleID { get; set; }
        public string RoleName { get; set; } = string.Empty;

        //  Quan hệ
        public ICollection<User>? Users { get; set; }
    }
}
