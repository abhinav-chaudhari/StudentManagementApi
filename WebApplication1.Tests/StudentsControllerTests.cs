using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;
using WebApplication1.Controllers;
using WebApplication1.DTOs;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Tests
{
    public class StudentsControllerTests
    {
        [Fact]
        public async Task GetById_WhenStudentDoesNotExist_ReturnsNotFound()
        {
            var service = new FakeStudentService();
            var controller = new StudentsController(service);

            var response = await controller.GetById(999);

            Assert.IsType<NotFoundResult>(response.Result);
        }
        [Fact]
        public async Task Delete_WhenStudentDoesNotExist_ReturnsNotFound()
        {
            var service = new FakeStudentService();
            var controller = new StudentsController(service);

            var response = await controller.Delete(999);

            Assert.IsType<NotFoundResult>(response);

        }
        [Fact]
        public async Task Update_WhenStudentDoesNotExist_ReturnsNotFound()
        {
            var service = new FakeStudentService();
            var controller = new StudentsController(service);

            var response = await controller.Update(999, new UpdateStudentRequest { Name = "Ab", Marks = 99 });
            Assert.IsType<NotFoundResult>(response);
        }
        [Fact]
        public async Task Create_WithValidRequest_ReturnsCreatedStudent()
        {
            var controller = new StudentsController(new FakeStudentService());
            var request = new CreateStudentRequest
            {
                Name = " Claire ",
                Marks = 85
            };

            var response = await controller.Create(request);

            var created = Assert.IsType<CreatedAtActionResult>(response.Result);
            var student = Assert.IsType<Student>(created.Value);

            Assert.Equal(201, created.StatusCode);
            Assert.Equal(nameof(StudentsController.GetById), created.ActionName);
            Assert.Equal(104, Assert.IsType<int>(created.RouteValues!["id"]));
            Assert.Equal(new Student(104, "Claire", 85, 1, "Computer Science"), student);
        }
        [Fact]
        public async Task GetAllAsync_WithInvalidPage_ReturnsBadRequest()
        {
            using var factory = new StudentApiFactory();
            using var client = factory.CreateStudentClient();

            using var response = await client.GetAsync("/api/students?page=0");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

            Assert.NotNull(problem);
            Assert.Contains("page", problem.Errors.Keys);
        }
        [Fact]
        public async Task GetById_ForwardsCancellationTokenToService()
        {
            using var source = new CancellationTokenSource();
            var service = new FakeStudentService();
            var controller = new StudentsController(service);
            await controller.GetById(101, source.Token);
            Assert.Equal(source.Token, service.ReceivedToken);
        }
        public class FakeStudentService : IStudentService
        {
            public CancellationToken ReceivedToken { get; set; }
            public int? ReceivedMinMarks { get; private set; }
            public int? ReceivedBranchId { get; private set; }
            public Task<Student?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            {
                ReceivedToken = cancellationToken;
                Student? student = id == 101 ? new Student(101, "Ab", 95, 1, "Computer Science") : null;
                return Task.FromResult<Student?>(student);
            }
            public Task<PagedResult<Student>> GetAllAsync(StudentQueryRequest request, CancellationToken cancellationToken = default)
            {
                ReceivedToken = cancellationToken;
                ReceivedMinMarks = request.MinMarks;
                ReceivedBranchId = request.Branchid;
                List<Student> items = [new(101, "Ab", 98, 1, "Computer Science")];
                return Task.FromResult(new PagedResult<Student>(items, 1, request.page, request.pageSize));
            }
            public Task<Student> CreateAsync(string name, int marks, int? branchId, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new Student(104, name, marks, 1, "Computer Science"));
            }
            public Task<bool> UpdateAsync(int id, string name, int marks, int? branchId, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(false);
            }
            public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(false);
            }
        }
    }
}
