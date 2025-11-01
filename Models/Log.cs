using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace bds.Models
{
    [Table("tblLog")]
    public class Log
    {
        public int LogID { get; set; }

        public int? UserID { get; set; }
        public string ActionType { get; set; } = null!;
        public string? ActionDescription { get; set; }
        public string? TableName { get; set; }
        public int? RecordID { get; set; }
        public string? IPAddress { get; set; }
        public string? BrowserInfo { get; set; }
        public bool IsSuccess { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual User? User { get; set; } 
    }
}
