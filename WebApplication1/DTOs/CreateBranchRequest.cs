using System.ComponentModel.DataAnnotations;

namespace WebApplication1.DTOs
{
    public class CreateBranchRequest
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
    }
}
