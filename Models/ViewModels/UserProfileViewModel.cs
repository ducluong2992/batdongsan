namespace bds.Models.ViewModels
{
    public class UserProfileViewModel
    {
        public User User { get; set; }
        public List<Post> Posts { get; set; }
        public List<Project> Projects { get; set; }
    }
}
