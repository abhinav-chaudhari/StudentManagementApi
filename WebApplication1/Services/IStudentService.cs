using WebApplication1.Models;
using WebApplication1.DTOs;

namespace WebApplication1.Services
{
    public interface IStudentService
    {
        Task<PagedResult<Student>> GetAllAsync(StudentQueryRequest request, CancellationToken cancellationToken = default);
        Task<Student?> GetByIdAsync(int Id, CancellationToken cancellationToken=default);
        Task<Student> CreateAsync(string name, int mark, int? branchId,CancellationToken cancellationToken = default);
        Task<StudentUpdateResult> UpdateAsync(int Id, string Name, int Marks, int? branchId,Guid Version,CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int Id, CancellationToken cancellationToken = default);
    }
}
