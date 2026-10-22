using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.DTOs;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class BranchService : IBranchService
    {
        private readonly AppDbContext db;
        public BranchService(AppDbContext db)
        {
            this.db = db;
        }
        public async Task<List<BranchResponse>> GetAllAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await db.Branches.AsNoTracking().OrderBy(b => b.Id).Select(b => new BranchResponse(b.Id, b.Name)).ToListAsync(cancellationToken);
        }
        public async Task<BranchResponse?> GetByIdAsync(int branchId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var branch = await db.Branches.AsNoTracking().OrderBy(b => b.Name).ThenBy(b => b.Id).Where(b => b.Id == branchId).Select(b => new BranchResponse(b.Id, b.Name)).FirstOrDefaultAsync(cancellationToken);
            return branch == null ? null : new BranchResponse(branch.Id, branch.Name);
        }
        public async Task<CreateBranchResult> CreateAsync(string name, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            bool exists = await db.Branches.AnyAsync(b => b.Name == name.Trim());
            if (exists)
                return new CreateBranchResult(BranchWriteResult.DuplicateName, null);
            var branch = new BranchEntity { Name = name };
            db.Branches.Add(branch);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 })
            {
                db.Entry(branch).State = EntityState.Detached;
                return new CreateBranchResult(BranchWriteResult.DuplicateName, null);
            }
            return new CreateBranchResult(BranchWriteResult.Success, new BranchResponse(branch.Id, branch.Name));
        }
        public async Task<BranchWriteResult> UpdateAsync(int Id, string name, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var branch = await db.Branches.FirstOrDefaultAsync(b => b.Id == Id, cancellationToken);
            var duplicateExists = await db.Branches.AnyAsync(b => b.Id != Id && b.Name == name, cancellationToken);
            if (branch == null)
                return BranchWriteResult.NotFound;
            if (duplicateExists)
                return BranchWriteResult.DuplicateName;
            branch.Name = name;
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 })
            {
                db.Entry(branch).State = EntityState.Detached;
                return BranchWriteResult.DuplicateName;
            }
            return BranchWriteResult.Success;
        }
        public async Task<BranchWriteResult> DeleteAsync(int Id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var branch = await db.Branches.FirstOrDefaultAsync(b => b.Id == Id);
            bool hasStudents = await db.Students.AnyAsync(s => s.BranchId == Id, cancellationToken);
            if (branch == null)
                return BranchWriteResult.NotFound;
            if (hasStudents)
                return BranchWriteResult.HasStudents;
            db.Branches.Remove(branch);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqliteException sqlite && (sqlite.SqliteExtendedErrorCode == 787 || (sqlite.SqliteExtendedErrorCode == 1811 && sqlite.Message.Contains("FOREIGN KEY constraint failed"))))
            {
                db.Entry(branch).State = EntityState.Unchanged;
                return BranchWriteResult.HasStudents;
            }
            return BranchWriteResult.Success;
        }
    }
}
