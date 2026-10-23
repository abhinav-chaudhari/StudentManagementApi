using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NuGet.Frameworks;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using WebApplication1.Controllers;
using WebApplication1.DTOs;
using WebApplication1.Models;
using static WebApplication1.Tests.StudentsControllerTests;

namespace WebApplication1.Tests
{
    public class StudentsIntegrationTests
    {
        [Fact]
        public async Task Create_WithoutMarks_ReturnsValidationError()
        {
            using var factory = new StudentApiFactory();
            using var client = factory.CreateStudentClient();
            await TestAuthentication.RegisterAndLoginAsync(client, factory, true);
            using var response = await client.PostAsJsonAsync("/api/students", new { name = "Claiare" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

            Assert.NotNull(problem);
            Assert.Contains("Marks", problem.Errors.Keys);
        }
        [Fact]
        public async Task Create_WithoutValidInput_ReturnWorkingLocation()
        {
            using var factory = new StudentApiFactory();
            using var client = factory.CreateStudentClient();
            await TestAuthentication.RegisterAndLoginAsync(client, factory, true);
            using var response = await client.PostAsJsonAsync("/api/students", new { name = "Claire", marks = 85 });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var created = await response.Content.ReadFromJsonAsync<Student>();
            Assert.NotNull(created);
            Assert.True(created.Id > 0);
            Assert.Equal("Claire", created.Name);
            Assert.Equal(85, created.Marks);

            var location = response.Headers.Location;
            Assert.NotNull(location);

            using var getResponse = await client.GetAsync(location);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var fetched = await getResponse.Content.ReadFromJsonAsync<Student>();
            Assert.Equal(created, fetched);
        }
        [Fact]
        public async Task Update_WithoutValidInput_ReturnWorkingLocation()
        {
            using var factory = new StudentApiFactory();
            using var client = factory.CreateStudentClient();
            await TestAuthentication.RegisterAndLoginAsync(client, factory, true);
            using var response = await client.PostAsJsonAsync("/api/students", new { name = "Claire", marks = 85 });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var created = await response.Content.ReadFromJsonAsync<Student>();
            Assert.NotNull(created);

            using var updateResponse = await client.PutAsJsonAsync($"api/students/{created.Id}", new { name = "Claire", marks = 98, branchId = created.branchId ,  version=created.Version});
            Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

            using var getResponse = await client.GetAsync($"/api/students/{created.Id}");

            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var updated = await getResponse.Content.ReadFromJsonAsync<Student>();
            Assert.NotNull(updated);
            Assert.True(updated.Id > 0);
            Assert.Equal("Claire", updated.Name);
            Assert.Equal(98, updated.Marks);
        }
        [Fact]
        public async Task GetById_WhenStudentExists_ReturnsOkWithStudent()
        {
            var controller = new StudentsController(new FakeStudentService());

            var response = await controller.GetById(101);

            var okResult = Assert.IsType<OkObjectResult>(response.Result);
            var student = Assert.IsType<Student>(okResult.Value);

            Assert.Equal(new Student(101, "Ab", 95, student.branchId, student.branchName, student.Version), student);
        }
        [Fact]
        public async Task Create_WithMissingBranch_ReturnsBadRequest()
        {
            using var factory = new StudentApiFactory();
            using var client = factory.CreateStudentClient();
            await TestAuthentication.RegisterAndLoginAsync(client, factory, true);
            using var response = await client.PostAsJsonAsync("/api/students", new { name = "Claire", marks = 85, branchId = 999 });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        [Fact]
        public async Task Create_WithExistingBranch_PersistsBranchDetails()
        {
            using var factory = new StudentApiFactory();
            using var client = factory.CreateStudentClient();
            await TestAuthentication.RegisterAndLoginAsync(client, factory, true);
            using var response = await client.PostAsJsonAsync("api/branches", new { name = "Computer Science" });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var created = await response.Content.ReadFromJsonAsync<BranchResponse>();
            Assert.NotNull(created);

            using var studentResponse = await client.PostAsJsonAsync("/api/students", new { name = "Claire", marks = 95, branchId = created.Id });

            Assert.Equal(HttpStatusCode.Created, studentResponse.StatusCode);
            var student = await studentResponse.Content.ReadFromJsonAsync<Student>();

            Assert.NotNull(student);
            Assert.Equal("Claire", student.Name);
            Assert.Equal(95, student.Marks);
            Assert.Equal(created.Id, student.branchId);
            Assert.Equal("Computer Science", student.branchName);

            using var studentResponse1 = await client.GetAsync($"/api/students/{student.Id}");
            Assert.Equal(HttpStatusCode.OK, studentResponse1.StatusCode);

            var fetched = await studentResponse1.Content.ReadFromJsonAsync<Student>();
            Assert.NotNull(fetched);
            Assert.Equal(created.Id, fetched.branchId);
            Assert.Equal("Computer Science", fetched.branchName);
        }
        [Fact]
        public async Task Update_WithDifferentBranch_PersistsNewBranch()
        {
            using var factory = new StudentApiFactory();
            using var client = factory.CreateStudentClient();
            await TestAuthentication.RegisterAndLoginAsync(client, factory, true);
            using var firstResponse = await client.PostAsJsonAsync("/api/branches", new { name = "Computer Science" });
            Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

            var first = firstResponse.Content.ReadFromJsonAsync<BranchResponse>();
            Assert.NotNull(first);

            using var secondResponse = await client.PostAsJsonAsync("/api/branches", new { name = "Mechanical Engineering" });
            Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);

            var second = secondResponse.Content.ReadFromJsonAsync<BranchResponse>();
            Assert.NotNull(second);

            using var createResponse = await client.PostAsJsonAsync("/api/students", new { name = "Claire", marks = 95, branchId = first.Id });
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

            var student = await createResponse.Content.ReadFromJsonAsync<Student>();
            Assert.NotNull(student);
            Assert.Equal(first.Id, student.branchId);

            using var updateResponse = await client.PutAsJsonAsync($"/api/students/{student.Id}", new { name = "Claire", marks = 95, branchId = second.Id,version=student.Version });
            Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

            using var getResponse = await client.GetAsync($"/api/students/{student.Id}");
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var updated = await getResponse.Content.ReadFromJsonAsync<Student>();
            Assert.NotNull(updated);
            Assert.Equal(student.Id, updated.Id);
            Assert.Equal(second.Id, updated.branchId);
            Assert.Equal("Mechanical Engineering", updated.branchName);
        }
        [Fact]
        public async Task GetAll_WithTwoBranchesAndStudentInEach_ReturnsStudents()
        {
            var service = new FakeStudentService();
            var controller = new StudentsController(service);

            var request = new StudentQueryRequest { MinMarks = 40, page = 1, pageSize = 5, Branchid = 1 };

            var response = await controller.GetAllAsync(request);

            var okResult = Assert.IsType<OkObjectResult>(response.Result);
            var result = Assert.IsType<PagedResult<Student>>(okResult.Value);

            Assert.Equal(40, service.ReceivedMinMarks);
            Assert.Equal(1, service.ReceivedBranchId);
            Assert.Equal(1, result.TotalCount);

            var student = Assert.Single(result.items);
            Assert.Equal(new Student(101, "Ab", 98, 1, "Computer Science", student.Version), student);
        }
        [Fact]
        public async Task GetAll_WithBranchAndMinimumMarks_ReturnsOnlyMatchingStudents()
        {
            using var factory = new StudentApiFactory();
            using var client = factory.CreateStudentClient();
            await TestAuthentication.RegisterAndLoginAsync(client, factory, true);
            using var firstBranch = await client.PostAsJsonAsync("/api/branches", new { name = "Computer Science" });
            Assert.Equal(HttpStatusCode.Created, firstBranch.StatusCode);

            var firstBranchCreated = await firstBranch.Content.ReadFromJsonAsync<BranchResponse>();
            Assert.NotNull(firstBranchCreated);

            using var secondBranch = await client.PostAsJsonAsync("/api/branches", new { name = "Mechanical Engineering" });
            Assert.Equal(HttpStatusCode.Created, secondBranch.StatusCode);

            var secondBranchCreated = await secondBranch.Content.ReadFromJsonAsync<BranchResponse>();
            Assert.NotNull(secondBranchCreated);

            using var createStudent1 = await client.PostAsJsonAsync("/api/students", new { name = "Ab", marks = 98, branchId = firstBranchCreated.Id });
            Assert.Equal(HttpStatusCode.Created, createStudent1.StatusCode);

            var student1 = await createStudent1.Content.ReadFromJsonAsync<Student>();
            Assert.NotNull(student1);

            using var createStudent2 = await client.PostAsJsonAsync("/api/students", new { name = "Fadi", marks = 32, branchId = firstBranchCreated.Id });
            Assert.Equal(HttpStatusCode.Created, createStudent2.StatusCode);

            var student2 = await createStudent2.Content.ReadFromJsonAsync<Student>();
            Assert.NotNull(student2);

            using var createStudent3 = await client.PostAsJsonAsync("/api/students", new { name = "Claire", marks = 95, branchid = secondBranchCreated.Id });
            Assert.Equal(HttpStatusCode.Created, createStudent3.StatusCode);

            var student3 = await createStudent3.Content.ReadFromJsonAsync<Student>();
            Assert.NotNull(student3);
            using var response = await client.GetAsync($"/api/students?MinMarks=40&page=1&pageSize=5&Branchid={firstBranchCreated.Id}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // for verifying issue 
            //var body = await response.Content.ReadAsStringAsync();
            //Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected 200, received {(int)response.StatusCode}. Body: {body}");


            var result = await response.Content.ReadFromJsonAsync<PagedResult<Student>>();
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);

            var student = Assert.Single(result.items);
            Assert.Equal("Ab", student.Name);
            Assert.Equal(98, student.Marks);
            Assert.Equal(firstBranchCreated.Id, student.branchId);
            Assert.Equal("Computer Science", student.branchName);
        }
        [Fact]
        public async Task GetAll_WithSortOrder_ReturnsStudentsInDescendingOrder()
        {
            using var factory = new StudentApiFactory();
            using var client = factory.CreateStudentClient();
            await TestAuthentication.RegisterAndLoginAsync(client, factory, true);
            var createFirstStudent = await client.PostAsJsonAsync("/api/students", new { name = "Ab", marks = 95 });
            Assert.Equal(HttpStatusCode.Created, createFirstStudent.StatusCode);
            var firstStudent = await createFirstStudent.Content.ReadFromJsonAsync<Student>();
            Assert.NotNull(firstStudent);

            var createSecondStudent = await client.PostAsJsonAsync("/api/students", new { name = "Fadi", marks = 32 });
            Assert.Equal(HttpStatusCode.Created, createSecondStudent.StatusCode);
            var secondStudent = await createSecondStudent.Content.ReadFromJsonAsync<Student>();
            Assert.NotNull(secondStudent);

            var createThirdStudent = await client.PostAsJsonAsync("/api/students", new { name = "Claire", marks = 95 });
            Assert.Equal(HttpStatusCode.Created, createThirdStudent.StatusCode);
            var thirdStudent = await createThirdStudent.Content.ReadFromJsonAsync<Student>();
            Assert.NotNull(thirdStudent);

            using var response = await client.GetAsync($"/api/students?sortBy=marks&ascending=false&page=1&pageSize=2");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<PagedResult<Student>>();
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalCount);

            Assert.Equal(2, result.items.Count);
            Assert.Equal(new[] { firstStudent.Id, thirdStudent.Id }, result.items.Select(s => s.Id));

            Assert.All(result.items, s => Assert.Equal(95, s.Marks));

            using var pageTwoResponse = await client.GetAsync($"/api/students?sortBy=marks&ascending=false&page=2&pageSize=2");
            Assert.Equal(HttpStatusCode.OK, pageTwoResponse.StatusCode);

            var pageTwo = await pageTwoResponse.Content.ReadFromJsonAsync<PagedResult<Student>>();
            Assert.NotNull(pageTwo);

            Assert.Equal(3, pageTwo.TotalCount);

            var remainingStudent = Assert.Single(pageTwo.items);
            Assert.Equal(secondStudent.Id, remainingStudent.Id);
            Assert.Equal("Fadi", remainingStudent.Name);
            Assert.Equal(32, remainingStudent.Marks);
        }
        [Fact]
        public async Task GetAll_WithInvalidSortOrder()
        {
            using var factory = new StudentApiFactory();
            using var client = factory.CreateStudentClient();
            await TestAuthentication.RegisterAndLoginAsync(client, factory, true);
            using var response = await client.GetAsync("/api/students?sortBy=unknown");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        [Theory]
        [InlineData("page=0", "page")]
        [InlineData("pageSize=101", "pageSize")]
        [InlineData("minMarks=105", "MinMarks")]
        [InlineData("branchId=0", "Branchid")]
        [InlineData("sortBy=unknown", "sortBy")]
        public async Task GetAll_WithInvalidQuery_ReturnsValidationError(string query, string expectedField)
        {
            using var factory = new StudentApiFactory();
            using var client = factory.CreateStudentClient();
            await TestAuthentication.RegisterAndLoginAsync(client, factory, true);
            using var response = await client.GetAsync($"/api/students?{query}");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
            Assert.NotNull(problem);
            Assert.Contains(expectedField, problem.Errors.Keys);
        }
        [Fact]
        public async Task GetAll_WithSearch_ReturnsStudentsAccordingToSearch()
        {
            using var factory = new StudentApiFactory();
            using var client = factory.CreateStudentClient();
            await TestAuthentication.RegisterAndLoginAsync(client, factory, true);
            using var createFirstStudent = await client.PostAsJsonAsync("/api/students", new { name = "Ab", marks = 98 });
            Assert.Equal(HttpStatusCode.Created, createFirstStudent.StatusCode);
            var firstStudent = await createFirstStudent.Content.ReadFromJsonAsync<Student>();
            Assert.NotNull(firstStudent);

            using var createSecondStudent = await client.PostAsJsonAsync("/api/students", new { name = "Abigail", marks = 75 });
            Assert.Equal(HttpStatusCode.Created, createSecondStudent.StatusCode);
            var secondStudent = await createSecondStudent.Content.ReadFromJsonAsync<Student>();
            Assert.NotNull(secondStudent);

            using var createThirdStudent = await client.PostAsJsonAsync("/api/students", new { name = "Abhishek", marks = 32 });
            Assert.Equal(HttpStatusCode.Created, createThirdStudent.StatusCode);
            var thirdStudent = await createThirdStudent.Content.ReadFromJsonAsync<Student>();
            Assert.NotNull(thirdStudent);

            using var createForthStudent = await client.PostAsJsonAsync("/api/students", new { name = "Claire", marks = 95 });
            Assert.Equal(HttpStatusCode.Created, createForthStudent.StatusCode);
            var forthStudent = await createForthStudent.Content.ReadFromJsonAsync<Student>();
            Assert.NotNull(forthStudent);

            using var response = await client.GetAsync("/api/students?search=AB&minMarks=40&page=1&pageSize=1");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<PagedResult<Student>>();
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);

            var student = Assert.Single(result.items);
            Assert.Equal("Ab", student.Name);

            var responsePageTwo = await client.GetAsync("/api/students?search=AB&minMarks=40&page=2&pageSize=1");
            Assert.Equal(HttpStatusCode.OK, responsePageTwo.StatusCode);

            var resultPageTwo = await responsePageTwo.Content.ReadFromJsonAsync<PagedResult<Student>>();
            Assert.NotNull(resultPageTwo);

            Assert.Equal(2, resultPageTwo.TotalCount);
            var studentPageTwo = Assert.Single(resultPageTwo.items);
            Assert.Equal("Abigail", studentPageTwo.Name);
        }
        [Theory]
        [InlineData("Computer Science")]
        [InlineData("computer science")]
        [InlineData(" Computer Science ")]
        public async Task Create_WithDuplicateBranchName_ReturnsConflict(string duplicateName)
        {
            using var factory = new StudentApiFactory();
            using var client = factory.CreateStudentClient();
            await TestAuthentication.RegisterAndLoginAsync(client, factory, true);
            using var firstResponse = await client.PostAsJsonAsync("/api/branches", new { name = "Computer Science" });
            Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

            using var duplicateResponse = await client.PostAsJsonAsync("/api/branches", new { name = duplicateName });
            Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
            using var listResponses = await client.GetAsync("/api/branches");
            var branches = await listResponses.Content.ReadFromJsonAsync<List<BranchResponse>>();

            Assert.NotNull(branches);
            Assert.Single(branches);
        }
        [Fact]
        public async Task Delete_UnusedBranch_RemoveBranch()
        {
            using var factory = new StudentApiFactory();
            using var client = factory.CreateStudentClient();
            await TestAuthentication.RegisterAndLoginAsync(client, factory, true);
            using var response = await client.PostAsJsonAsync("/api/branches", new { name = "Computer Science" });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var branch = await response.Content.ReadFromJsonAsync<BranchResponse>();
            Assert.NotNull(branch);

            using var deleteResponse = await client.DeleteAsync($"/api/branches/{branch.Id}");
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            using var getResponse = await client.GetAsync($"/api/branches/{branch.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

            using var repeatResponse = await client.DeleteAsync($"/api/branches/{branch.Id}");
            Assert.Equal(HttpStatusCode.NotFound, repeatResponse.StatusCode);
        }
        [Fact]
        public async Task Delete_UsedBranch_ReturnsConflict()
        {
            using var factory = new StudentApiFactory();
            using var client = factory.CreateStudentClient();
            await TestAuthentication.RegisterAndLoginAsync(client, factory, true);
            using var createBranch = await client.PostAsJsonAsync("/api/branches", new { name = "Computer Science" });
            Assert.Equal(HttpStatusCode.Created, createBranch.StatusCode);

            var branch = await createBranch.Content.ReadFromJsonAsync<BranchResponse>();
            Assert.NotNull(branch);

            using var createStudent = await client.PostAsJsonAsync("/api/students", new { name = "Ab", marks = 98, branchId = branch.Id });
            Assert.Equal(HttpStatusCode.Created, createStudent.StatusCode);

            var student = await createStudent.Content.ReadFromJsonAsync<Student>();
            Assert.NotNull(student);

            using var deleteBranch = await client.DeleteAsync($"/api/branches/{branch.Id}");
            Assert.Equal(HttpStatusCode.Conflict, deleteBranch.StatusCode);

            var getResponse = await client.GetAsync($"/api/branches/{branch.Id}");
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var getStudent = await client.GetAsync($"/api/students/{student.Id}");
            Assert.Equal(HttpStatusCode.OK, getStudent.StatusCode);

            var fetch = await getStudent.Content.ReadFromJsonAsync<Student>();
            Assert.NotNull(fetch);
            Assert.Equal(student.Id, fetch.Id);
            Assert.Equal(branch.Id, fetch.branchId);
            Assert.Equal("Computer Science", fetch.branchName);
        }
        [Fact]
        public async Task Rename_Branch_UpdatesBranchName()
        {
            using var factory = new StudentApiFactory();
            using var client = factory.CreateStudentClient();
            await TestAuthentication.RegisterAndLoginAsync(client, factory, true);
            using var createBranch = await client.PostAsJsonAsync("/api/branches", new { name = "Computer Science" });
            Assert.Equal(HttpStatusCode.Created, createBranch.StatusCode);
            var branch = await createBranch.Content.ReadFromJsonAsync<BranchResponse>();
            Assert.NotNull(branch);

            using var createStudent = await client.PostAsJsonAsync("/api/students", new { name = "Ab", marks = 98, branchId = branch.Id });
            Assert.Equal(HttpStatusCode.Created, createStudent.StatusCode);
            var student = await createStudent.Content.ReadFromJsonAsync<Student>();
            Assert.NotNull(student);

            using var updateBranch = await client.PutAsJsonAsync($"/api/branches/{branch.Id}", new { name = "Computer Science and Engineering" });
            Assert.Equal(HttpStatusCode.NoContent, updateBranch.StatusCode);

            using var getBranch = await client.GetAsync($"/api/branches/{branch.Id}");
            Assert.Equal(HttpStatusCode.OK, getBranch.StatusCode);

            using var getStudent = await client.GetAsync($"/api/students/{student.Id}");
            Assert.Equal(HttpStatusCode.OK, getStudent.StatusCode);
            var fetchedStudent = await getStudent.Content.ReadFromJsonAsync<Student>();
            Assert.NotNull(fetchedStudent);

            Assert.Equal(student.Id, fetchedStudent.Id);
            Assert.Equal(branch.Id, fetchedStudent.branchId);
            Assert.Equal("Computer Science and Engineering", fetchedStudent.branchName);
        }
        [Fact]
        public async Task Update_BranchWithSameNameAsAnother_ReturnConflict()
        {
            using var factory = new StudentApiFactory();
            using var client = factory.CreateStudentClient();
            await TestAuthentication.RegisterAndLoginAsync(client, factory, true);
            using var createBranch = await client.PostAsJsonAsync("/api/branches", new { name = "Computer Science" });
            Assert.Equal(HttpStatusCode.Created, createBranch.StatusCode);
            var branch = await createBranch.Content.ReadFromJsonAsync<BranchResponse>();
            Assert.NotNull(branch);

            using var anotherBranch = await client.PostAsJsonAsync("/api/branches", new { name = "Information Technology" });
            Assert.Equal(HttpStatusCode.Created, anotherBranch.StatusCode);
            var otherBranch = await anotherBranch.Content.ReadFromJsonAsync<BranchResponse>();
            Assert.NotNull(otherBranch);

            using var createStudent = await client.PostAsJsonAsync("/api/students", new { name = "Ab", marks = 98, branchId = branch.Id });
            Assert.Equal(HttpStatusCode.Created, createStudent.StatusCode);
            var student = await createStudent.Content.ReadFromJsonAsync<Student>();
            Assert.NotNull(student);

            using var updateBranch = await client.PutAsJsonAsync($"/api/branches/{branch.Id}", new { name = otherBranch.Name });
            Assert.Equal(HttpStatusCode.Conflict, updateBranch.StatusCode);
            Assert.Equal("Information Technology", otherBranch.Name);

            using var getResponse = await client.GetAsync($"/api/branches/{branch.Id}");

            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var unchanged = await getResponse.Content.ReadFromJsonAsync<BranchResponse>();

            Assert.NotNull(unchanged);
            Assert.Equal("Computer Science", unchanged.Name);
        }
        [Fact]
        public async Task Update_BranchNameSameAsBefore_ReturnsNoContent()
        {
            using var factory = new StudentApiFactory();
            using var client = factory.CreateStudentClient();
            await TestAuthentication.RegisterAndLoginAsync(client, factory, true);
            using var createBranch = await client.PostAsJsonAsync("/api/branches", new { name = "Computer Science" });
            Assert.Equal(HttpStatusCode.Created, createBranch.StatusCode);
            var branch = await createBranch.Content.ReadFromJsonAsync<BranchResponse>();
            Assert.NotNull(branch);

            using var createStudent = await client.PostAsJsonAsync("/api/students", new { name = "Ab", marks = 98, branchId = branch.Id });
            Assert.Equal(HttpStatusCode.Created, createStudent.StatusCode);

            using var updateBranch = await client.PutAsJsonAsync($"/api/branches/{branch.Id}", new { name = "Computer Science" });
            Assert.Equal(HttpStatusCode.NoContent, updateBranch.StatusCode);
            using var getUpdatedBranch = await client.GetAsync($"/api/branches/{branch.Id}");
            Assert.Equal(HttpStatusCode.OK, getUpdatedBranch.StatusCode);
            var updatedBranch = await getUpdatedBranch.Content.ReadFromJsonAsync<BranchResponse>();
            Assert.NotNull(updatedBranch);

            Assert.Equal("Computer Science", updatedBranch.Name);
        }
        [Fact]
        public async Task Update_MissingBranch_ReturnsNotFound()
        {
            using var factory = new StudentApiFactory();
            using var client = factory.CreateStudentClient();
            await TestAuthentication.RegisterAndLoginAsync(client, factory, true);
            var branchThatDoesNotExist = 101;
            using var createBranch = await client.PostAsJsonAsync("/api/branches", new { name = "Computer Science" });
            Assert.Equal(HttpStatusCode.Created, createBranch.StatusCode);

            using var updateBranch = await client.PutAsJsonAsync($"/api/branches/{branchThatDoesNotExist}", new { name = "Information Technology" });
            Assert.Equal(HttpStatusCode.NotFound, updateBranch.StatusCode);
        }
        [Fact]
        public async Task Update_BranchWithEmptyBranchName_ReturnsBadRequest()
        {
            using var factory = new StudentApiFactory();
            using var client = factory.CreateStudentClient();
            await TestAuthentication.RegisterAndLoginAsync(client, factory, true);
            using var createBranch = await client.PostAsJsonAsync("/api/branches", new { name = "Computer Science" });
            Assert.Equal(HttpStatusCode.Created, createBranch.StatusCode);
            var branch = await createBranch.Content.ReadFromJsonAsync<BranchResponse>();
            Assert.NotNull(branch);

            using var updateBranch = await client.PutAsJsonAsync($"/api/branches/{branch.Id}", new { name = "" });
            Assert.Equal(HttpStatusCode.BadRequest, updateBranch.StatusCode);
        }
        [Fact]
        public async Task Create_WithoutAuthentication_ReturnsUnauthorized()
        {
            using var factory = new StudentApiFactory();
            using var client = factory.CreateStudentClient();

            using var response = await client.PostAsJsonAsync("/api/students", new { name = "Ab", marks = 98 });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        [Fact]
        public async Task GetBranches_WithoutAuthentication_ReturnsOk()
        {
            using var factory = new StudentApiFactory();
            using var client = factory.CreateStudentClient();

            using var response = await client.GetAsync("/api/branches");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        [Fact]
        public async Task Create_AsRegularUser_ReturnsForbidden()
        {
            using var factory = new StudentApiFactory();
            using var client = factory.CreateStudentClient();

            await TestAuthentication.RegisterAndLoginAsync(client, factory, asAdmin: false);
            using var response = await client.PostAsJsonAsync("/api/students", new { name = "Ab", marks = 98 });
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        [Fact]
        public async Task Create_AsAdmin_ReturnsCreated()
        {
            using var factory = new StudentApiFactory();
            using var client = factory.CreateStudentClient();
            await TestAuthentication.RegisterAndLoginAsync(client, factory, asAdmin: true);
            using var response = await client.PostAsJsonAsync("/api/students", new { name = "Ab", marks = 98 });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
        [Theory]
        [InlineData("/health/live")]
        [InlineData("/health/ready")]
        public async Task Health_WhenApplicationHealthy_ReturnsHealthy(string path)
        {
            using var factory = new StudentApiFactory();
            using var client = factory.CreateStudentClient();

            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
        }
        [Fact]
        public async Task Health_WhenDatabaseCheckFails_ReadyIsUnavailableButLiveIsHealthy()
        {
            using var factory = new StudentApiFactory();
            using var failingFactory = factory.WithWebHostBuilder(builder => { builder.ConfigureServices(services => { services.Configure<HealthCheckServiceOptions>(options => { var databaseChecks = options.Registrations.Where(check => check.Name == "database").ToList(); foreach (var check in databaseChecks) { options.Registrations.Remove(check); } }); services.AddHealthChecks().AddCheck("database", () => HealthCheckResult.Unhealthy("Simulated database failure."), tags: new[] { "ready" }); }); });

            using var client = failingFactory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

            using var readyResponse = await client.GetAsync("/health/ready");
            Assert.Equal(HttpStatusCode.ServiceUnavailable, readyResponse.StatusCode);  

            var readyBody = await readyResponse.Content.ReadAsStringAsync();
            Assert.Equal("Unhealthy", readyBody);
            Assert.DoesNotContain("Simulated database failure.", readyBody);

            using var liveResponse = await client.GetAsync("/health/live");

            Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);
            Assert.Equal("Healthy", await liveResponse.Content.ReadAsStringAsync());
        }
        [Fact]
        public async Task GetAll_WhenRateLimitExceeded_ReturnsTooManyRequests()
        {
            using var factory = new StudentApiFactory();
            using var client = factory.CreateStudentClient();

            for (int i=0; i<10; i++)
            {
                using var response = await client.GetAsync("/api/students");
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
            using var rejected = await client.GetAsync("/api/students");

            Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);

            using var health = await client.GetAsync("/health/ready");
            Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        }
        [Fact]
        public async Task Update_WithStaleVersion_ReturnsConflictAndPreservesFirstUpdate()
        {
            using var factory = new StudentApiFactory();
            using var client = factory.CreateStudentClient();

            await TestAuthentication.RegisterAndLoginAsync(client, factory, asAdmin: true);

            using var createResponse = await client.PostAsJsonAsync("/api/students", new { name = "Claire", marks = 75 });
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

            var original = await createResponse.Content.ReadFromJsonAsync<Student>();
            Assert.NotNull(original);

            using var firstUpdate = await client.PutAsJsonAsync($"/api/students/{original.Id}", new { name = "Claire", marks = 90, version = original.Version });
            Assert.Equal(HttpStatusCode.NoContent, firstUpdate.StatusCode);

            using var staleUpdate = await client.PutAsJsonAsync($"/api/students/{original.Id}", new { name = "Claire", marks = 80, version = original.Version });
            Assert.Equal(HttpStatusCode.Conflict, staleUpdate.StatusCode);

            using var response = await client.GetAsync($"/api/students/{original.Id}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var fetched = await response.Content.ReadFromJsonAsync<Student>();
            Assert.NotNull(fetched);
            Assert.Equal(90, fetched.Marks);
            Assert.NotEqual(original.Version,fetched.Version);
        }
    }
}

