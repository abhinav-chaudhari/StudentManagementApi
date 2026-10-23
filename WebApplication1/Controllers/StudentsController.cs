using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebApplication1.DTOs;
using WebApplication1.Models;
using WebApplication1.Security;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/students")]
    public class StudentsController : ControllerBase
    {
        private readonly IStudentService studentService;

        public StudentsController(IStudentService studentService) 
        {
            this.studentService = studentService;
        }
        [HttpGet]
        [EnableRateLimiting(RateLimitPolicies.StudentRead)]
        public async Task<ActionResult<PagedResult<Student>>> GetAllAsync([FromQuery] StudentQueryRequest request,CancellationToken cancellationToken = default)
        {
            var student = await studentService.GetAllAsync(request);
            return Ok(student);
        }
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Student>> GetById(int id,CancellationToken cancellationToken = default)
        {
            var student = await studentService.GetByIdAsync(id,cancellationToken);
            if (student == null)
                return NotFound();
            else
                return Ok(student);
        }
        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.ManageSchoolData)]
        public async Task<ActionResult<Student>> Create([FromBody]CreateStudentRequest request, CancellationToken cancellationToken = default)
        {
            try {
                var student = await studentService.CreateAsync(request.Name.Trim(), request.Marks.Value, request.BranchId,cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = student.Id }, student);
            } catch (ArgumentException ex)
            {
                return BadRequest("Selected branch does not exist.");
            }
        }
        [HttpPut("{Id:int}")]
        [Authorize(Policy = AuthorizationPolicies.ManageSchoolData)]
        public async Task<IActionResult> Update(int Id, [FromBody] UpdateStudentRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await studentService.UpdateAsync(Id, request.Name.Trim(), request.Marks!.Value, request.Branchid, request.Version!.Value,cancellationToken);
                return result switch
                {
                    StudentUpdateResult.Success => NoContent(),
                    StudentUpdateResult.NotFound => NotFound(),
                    StudentUpdateResult.Conflict => Conflict("Student has changed Reload it before updating."),
                    _=> throw new InvalidOperationException("Unexpected update result.")
                };
            }catch(ArgumentException ex) when (ex.ParamName == "branchId")
            {
                return BadRequest("selected branch does not exist.");
            }
        }
        [HttpDelete("{Id:int}")]
        [Authorize(Policy = AuthorizationPolicies.ManageSchoolData)]
        public async Task<IActionResult> Delete(int Id, CancellationToken cancellationToken = default)
        {
            bool Deleted = await studentService.DeleteAsync(Id,cancellationToken);
            if (!Deleted)
                return NotFound();
            return NoContent();
        }
    }
}
