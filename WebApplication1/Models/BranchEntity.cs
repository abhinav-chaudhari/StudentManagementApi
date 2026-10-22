namespace WebApplication1.Models
{
    public class BranchEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ICollection<StudentEntity> Students { get; set; } = new List<StudentEntity>();
    }
}
