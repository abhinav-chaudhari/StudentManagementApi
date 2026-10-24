using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebApplication1.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Tests
{
    public class DatabaseMigrationTests
    {
        [Fact]
        public void Model_HasNoChangesMissingFromMigration()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite("DataSource=:memory:").Options;
            using var db = new AppDbContext(options);
            Assert.False(db.Database.HasPendingModelChanges(), "The model changed without a matching migration");
        }
        [Fact]
        public async Task Migrations_OnEmptyDatabase_CreateWorkingSchema()
        {
            await using var connection = new SqliteConnection("DataSource=:memory:");

            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

            int studentId;
            int branchId;
            Guid version;

            await using (var db = new AppDbContext(options))
            {
                Assert.NotEmpty(db.Database.GetMigrations());

                await db.Database.MigrateAsync();

                Assert.Empty(await db.Database.GetPendingMigrationsAsync());

                var branch = new BranchEntity { Name = "Computer Science" };

                var student = new StudentEntity { Name = "Claire", Marks = 95, Branch = branch };

                db.Students.Add(student);
                await db.SaveChangesAsync();

                studentId = student.Id;
                branchId = branch.Id;
                version = student.Version;

                Assert.Equal(0, await db.Set<IdentityUser>().CountAsync());
            }

            await using var verification = new AppDbContext(options);

            var saved = await verification.Students.AsNoTracking().Include(s => s.Branch).SingleAsync(s => s.Id == studentId);

            Assert.Equal("Claire", saved.Name);
            Assert.Equal(95, saved.Marks);
            Assert.Equal(branchId, saved.BranchId);
            Assert.NotNull(saved.Branch);
            Assert.Equal("Computer Science", saved.Branch.Name);
            Assert.NotEqual(Guid.Empty, saved.Version);
            Assert.Equal(version, saved.Version);
        }
    }
}
