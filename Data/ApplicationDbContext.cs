using Microsoft.EntityFrameworkCore;
using bds.Models;
namespace bds.Data
{
    public class ApplicationDbContext:DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options):base (options) { }
        public DbSet<News> News { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Post> Post { get; set; }
        public DbSet<SearchHistory> SearchHistories { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<Province> Provinces { get; set; }
        public DbSet<CommuneWard> CommuneWards { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Statistic> Statistics { get; set; }
    }
}
