using System.ComponentModel.DataAnnotations;

namespace WebApplication1.DTOs
{
    public class CreateStudentRequest
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        [Required]
        [Range(0,100)]
        public int? Marks { get; set; }
        [Range(1,int.MaxValue)]
        public int? BranchId { get; set; }
    }
}
