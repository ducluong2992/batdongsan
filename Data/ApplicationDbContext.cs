using Microsoft.EntityFrameworkCore;
using bds.Models;
namespace bds.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        public DbSet<News> News { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Post> Post { get; set; }
        public DbSet<SearchHistory> SearchHistories { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<Province> Provinces { get; set; }
        public DbSet<District> Districts { get; set; }

        public DbSet<CommuneWard> CommuneWards { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Statistic> Statistics { get; set; }
        public DbSet<Log> Logs { get; set; }



        // --- THÊM PHƯƠNG THỨC NÀY ---
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Chỉ định rõ ràng tên bảng cho từng model
            modelBuilder.Entity<CommuneWard>().ToTable("CommuneWard");
            modelBuilder.Entity<District>().ToTable("District");
            modelBuilder.Entity<Role>().ToTable("Role");
            modelBuilder.Entity<User>().ToTable("User");
            modelBuilder.Entity<Province>().ToTable("Province");
            modelBuilder.Entity<Category>().ToTable("Category");
            modelBuilder.Entity<News>().ToTable("News");
            modelBuilder.Entity<Project>().ToTable("Projects");
            modelBuilder.Entity<Post>().ToTable("Post");
            modelBuilder.Entity<SearchHistory>().ToTable("SearchHistory");
            modelBuilder.Entity<Image>().ToTable("Images");
            modelBuilder.Entity<Statistic>().ToTable("Statistic");
            modelBuilder.Entity<Log>().ToTable("tblLog");
        }
    
    }
}
