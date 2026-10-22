using Microsoft.AspNetCore.Mvc;
using WebApplication1.Controllers;
using WebApplication1.DTOs;
using WebApplication1.Services;

namespace WebApplication1.Tests
{
    public class BranchControllerTests
    {
        [Fact]
        public async Task Delete_WhenBranchHasStudents_ReturnsConflict()
        {
            var service = new FakeBranchService();
            var controller = new BranchController(service);
            var response = await controller.Delete(1);
            Assert.IsType<ConflictObjectResult>(response);
        }
        [Fact]
        public async Task Delete_WhenSuccessful_ReturnsNoContent()
        {
            var service = new FakeBranchService { DeleteResult = BranchWriteResult.Success };
            var controller = new BranchController(service);
            var response = await controller.Delete(1);
            Assert.IsType<NoContentResult>(response);
        }
        [Fact]
        public async Task Update_WithValidDTO_ReturnsNotFoundResult()
        {
            var service = new FakeBranchService
            {
                UpdateResult = BranchWriteResult.NotFound
            };
            var controller = new BranchController(service);
            var response = await controller.Update(999, new UpdateBranchRequest { Name="Computer Science"});
            Assert.IsType<NotFoundResult>(response);
        }
        [Fact]
        public async Task Update_WithDuplicateName_ReturnsConflict()
        {
            var service = new FakeBranchService
            {
                UpdateResult = BranchWriteResult.DuplicateName
            };
            var controller = new BranchController(service);
            var response = await controller.Update(999, new UpdateBranchRequest { Name = "Computer Science" });
            Assert.IsType<ConflictObjectResult>(response);
        }
        [Fact]
        public async Task Create_WithValidDTO_ReturnsConflict()
        {
            var service = new FakeBranchService();
            var controller = new BranchController(service);
            var response = await controller.Create(new CreateBranchRequest { Name = "Computer Science" });
            Assert.IsType<ConflictObjectResult>(response.Result);
        }
        public class FakeBranchService : IBranchService
        {
            public BranchWriteResult DeleteResult { get; set; } = BranchWriteResult.HasStudents;
            public BranchWriteResult UpdateResult { get; set; } = BranchWriteResult.DuplicateName;
            public CreateBranchResult CreateResult { get; set; } = new(BranchWriteResult.DuplicateName, null);
            public Task<List<BranchResponse>> GetAllAsync(CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }
            public Task<BranchResponse?> GetByIdAsync(int Id, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }
            public Task<CreateBranchResult> CreateAsync(string name, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(CreateResult);
            }
            public Task<BranchWriteResult> UpdateAsync(int Id, string name, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(UpdateResult);
            }
            public Task<BranchWriteResult> DeleteAsync(int Id, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(DeleteResult);
            }
        }
    }
}
