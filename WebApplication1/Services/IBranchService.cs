using WebApplication1.DTOs;

namespace WebApplication1.Services
{
    public enum BranchWriteResult
    {
        Success, NotFound, DuplicateName, HasStudents
    }
    public record CreateBranchResult(BranchWriteResult result, BranchResponse? response);
    public interface IBranchService
    {
        Task<List<BranchResponse>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<BranchResponse?> GetByIdAsync(int Id, CancellationToken cancellationToken = default);
        Task<CreateBranchResult> CreateAsync(string name, CancellationToken cancellationToken = default);
        Task<BranchWriteResult> UpdateAsync(int Id, string name, CancellationToken cancellationToken = default);
        Task<BranchWriteResult> DeleteAsync(int Id, CancellationToken cancellationToken = default);
    }
}
