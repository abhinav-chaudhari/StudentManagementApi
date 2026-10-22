using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.DTOs;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/branches")]
    public class BranchController : ControllerBase
    {
        private readonly IBranchService branchService;
        public BranchController(IBranchService branchService)
        {
            this.branchService = branchService;
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BranchResponse>>> GetAll(CancellationToken cancellationToken = default)
        {
            var branches = await branchService.GetAllAsync(cancellationToken);
            return Ok(branches);
        }
        [HttpGet("{Id:int}")]
        public async Task<ActionResult<BranchResponse>> GetById(int Id, CancellationToken cancellationToken = default)
        {
            var branch = await branchService.GetByIdAsync(Id, cancellationToken);
            if (branch == null)
                return NotFound();
            return Ok(branch);
        }
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<BranchResponse>> Create([FromBody] CreateBranchRequest request, CancellationToken cancellationToken = default)
        {
            var outcome = await branchService.CreateAsync(request.Name, cancellationToken);
            if (outcome.result == BranchWriteResult.DuplicateName)
                return Conflict("A Branch with the same name is already present.");
            if (outcome.result != BranchWriteResult.Success || outcome.response == null)
                throw new InvalidOperationException("Invalid Branch creation result.");
            return CreatedAtAction(nameof(GetById), new {id=outcome.response.Id},outcome.response);
        }
        [Authorize]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> Delete(int Id, CancellationToken cancellationToken = default)
        {
            var outcome = await branchService.DeleteAsync(Id, cancellationToken);
            return outcome switch
            {
                BranchWriteResult.Success => NoContent(),
                BranchWriteResult.NotFound => NotFound(),
                BranchWriteResult.HasStudents => Conflict("Cannot delete this branch is already assigned to students."),
                _ => throw new InvalidOperationException("Invalid branch operation result.")
            };
        }
        [Authorize]
        [HttpPut("{id:int}")]
        public async Task<ActionResult> Update(int Id, [FromBody] UpdateBranchRequest request, CancellationToken cancellationToken = default)
        {
            var outcome = await branchService.UpdateAsync(Id, request.Name, cancellationToken);
            return outcome switch
            {
                BranchWriteResult.Success => NoContent(),
                BranchWriteResult.NotFound => NotFound(),
                BranchWriteResult.DuplicateName => Conflict("A branch with same name already exists."),
                _ => throw new InvalidOperationException("Invalid branch update result.")
            };
        }
    }
}
