using System.ComponentModel.DataAnnotations;

namespace WebApplication1.DTOs
{
    public class StudentQueryRequest
    {
        [Range(0,100)]
        public int? MinMarks { get; set; }
        [Range(1,int.MaxValue)]
        public int? Branchid { get; set; }
        [Range(1, int.MaxValue)]
        public int page { get; set; } = 1;
        [Range(1, 100)]
        public int pageSize { get; set; } = 5;
        [Required]
        [RegularExpression(@"(?i)^\s*(id|name|marks)\s*$",ErrorMessage ="Sort By should be id, name or marks")]
        public string sortBy { get; set; } = "id";
        public bool Ascending { get; set; } = true;
        [StringLength(100)]
        public string? search { get; set; }
    }
}
