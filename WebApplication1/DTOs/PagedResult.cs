namespace WebApplication1.DTOs
{
    public record PagedResult<T>(List<T> items,int TotalCount, int Page, int pageSize)
    {
        public int TotalPage => (int)Math.Ceiling((double)TotalCount / pageSize);
    }
}
