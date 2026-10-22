using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.DTOs;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class StudentService : IStudentService
    {
        private readonly AppDbContext db;
        public StudentService(AppDbContext db)
        {
            this.db = db;
        }
        public async Task<PagedResult<Student>> GetAllAsync(StudentQueryRequest request, CancellationToken cancellationToken = default)
        {
            int? minMarks = request.MinMarks;
            int page = request.page;
            int pageSize = request.pageSize;
            string? sortBy = request.sortBy;
            bool ascending = request.Ascending;
            int? branchId = request.Branchid;
            string? search = request.search?.Trim().ToLowerInvariant();
            cancellationToken.ThrowIfCancellationRequested();
            var query = db.Students.AsNoTracking();
            if (minMarks.HasValue)
                query = query.Where(s => s.Marks >= minMarks.Value);
            if (branchId.HasValue)
                query = query.Where(s => s.BranchId == branchId.Value);
            if (!string.IsNullOrEmpty(search))
                query = query.Where(s => s.Name.ToLower().Contains(search));
            int skip = checked((page - 1) * pageSize);
            int totalCount = await query.CountAsync(cancellationToken);
            IOrderedQueryable<StudentEntity> orderedQuery = (sortBy, ascending) switch
            {
                ("name", true) => query.OrderBy(s => s.Name).ThenBy(s => s.Id),
                ("name", false) => query.OrderByDescending(s => s.Name).ThenBy(s => s.Id),
                ("marks", true) => query.OrderBy(s => s.Marks).ThenBy(s => s.Id),
                ("marks", false) => query.OrderByDescending(s => s.Marks).ThenBy(s => s.Id),
                ("id", true) => query.OrderBy(s => s.Id),
                ("id", false) => query.OrderByDescending(s => s.Id),
                _ => throw new ArgumentException("Unsupported sort field.", nameof(sortBy))
            };
            var items = await orderedQuery.Skip(skip).Take(pageSize).Select(s => new Student(s.Id, s.Name, s.Marks,s.BranchId,s.Branch==null?null:s.Branch.Name)).ToListAsync(cancellationToken);
            return new PagedResult<Student>(items, totalCount, page, pageSize);
        }
        public async Task<Student?> GetByIdAsync(int Id,CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entity = await db.Students.AsNoTracking().Where(s=> s.Id==Id).Select(s=>new Student(s.Id,s.Name,s.Marks,s.BranchId,s.Branch==null?null:s.Branch.Name)).FirstOrDefaultAsync(cancellationToken);
            return entity == null ? null : new Student(entity.Id,entity.Name,entity.Marks,entity.branchId,entity.branchName);
        }
        public async Task<Student> CreateAsync(string Name, int Marks, int? branchId,CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (branchId.HasValue)
            {
                bool exists = await db.Branches.AnyAsync(b=> b.Id == branchId.Value, cancellationToken);
                if(!exists)
                    throw new ArgumentException($"Branch does not exist.", nameof(branchId));
            }
            var entity = new StudentEntity { Name=Name,Marks=Marks,BranchId=branchId };
            db.Students.Add(entity);
            await db.SaveChangesAsync(cancellationToken);
            return  (await GetByIdAsync(entity.Id, cancellationToken))!;
        }
        public async Task<bool> UpdateAsync(int id, string name, int marks, int? branchId,CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entity = await db.Students.FirstOrDefaultAsync(s => s.Id == id,cancellationToken);
            if (entity == null)
                return false;
            if (branchId.HasValue)
            {
                bool exists = await db.Branches.AnyAsync(b => b.Id == branchId, cancellationToken);
                if (!exists) throw new ArgumentException($"Branch does not exist.", nameof(branchId));
            }
            entity.Name = name;
            entity.Marks = marks;
            entity.BranchId = branchId;
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entity = await db.Students.FirstOrDefaultAsync(s => s.Id == id,cancellationToken);
            if (entity == null)
                return false;
            db.Students.Remove(entity);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
