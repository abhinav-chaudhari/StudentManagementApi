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
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

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
        [Fact]
        public async Task RepairVersionMigration_PreservesExistingStudent()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");

            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

            await using (var db = new AppDbContext(options))
            {
                var migrations = db.Database.GetMigrations().ToList();

                int repairIndex = migrations.FindIndex(name => name.EndsWith("_AddStudentVersion"));

                Assert.True(repairIndex > 0, "Expected the repair migration and a preceding migration.");

                string previousMigration = migrations[repairIndex - 1];

                var migrator = db.GetService<IMigrator>();
                await migrator.MigrateAsync(previousMigration);

                await db.Database.ExecuteSqlRawAsync("""insert into "Students" ("Id","Name","Marks","BranchId") values(101,'Claire',75,null);""");

                await db.Database.MigrateAsync();
                Assert.Empty(await db.Database.GetPendingMigrationsAsync());
            }

            await using (var verification = new AppDbContext(options))
            {
                var student = await verification.Students.SingleAsync(s => s.Id == 101);

                Assert.Equal("Claire", student.Name);
                Assert.Equal(75, student.Marks);
                Assert.Null(student.BranchId);

                Assert.Equal(Guid.Empty, student.Version);

                student.Marks = 90;
                student.Version = Guid.NewGuid();

                await verification.SaveChangesAsync();
            }
            await using var finalContext = new AppDbContext(options);

            var saved = await finalContext.Students.AsNoTracking().SingleAsync(s => s.Id == 101);

            Assert.Equal("Claire", saved.Name);
            Assert.Equal(90, saved.Marks);
            Assert.NotEqual(Guid.Empty, saved.Version);
            Assert.Equal(1, await finalContext.Students.CountAsync());
        }
    }
}
