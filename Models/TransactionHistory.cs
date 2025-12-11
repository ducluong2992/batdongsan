namespace bds.Models
{
    public class TransactionHistory
    {
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Coins { get; set; }  // +10, -10 hoặc 0
        public int? PostID { get; set; }
        public int? ProjectID { get; set; }
    }
}
