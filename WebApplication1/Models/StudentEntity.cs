namespace WebApplication1.Models
{
    public class StudentEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Marks { get; set; }
        public int? BranchId { get; set; }
        public BranchEntity? Branch { get; set; }
        public Guid Version { get; set; } = Guid.NewGuid();
    }
}
