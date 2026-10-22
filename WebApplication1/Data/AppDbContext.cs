using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace WebApplication1.Data
{
    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        { }
        public DbSet<StudentEntity> Students => Set<StudentEntity>();
        public DbSet<BranchEntity> Branches => Set<BranchEntity>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<BranchEntity>().Property(b => b.Name).HasMaxLength(100).IsRequired();
            modelBuilder.Entity<StudentEntity>().HasOne(s => s.Branch).WithMany(b => b.Students).HasForeignKey(s => s.BranchId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BranchEntity>().Property(b => b.Name).HasMaxLength(100).IsRequired().UseCollation("NOCASE");
            modelBuilder.Entity<BranchEntity>().HasIndex(b => b.Name).IsUnique();
        }
    }
}
