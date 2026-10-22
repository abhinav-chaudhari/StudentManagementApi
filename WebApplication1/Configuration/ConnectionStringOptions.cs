namespace WebApplication1.Configuration
{
    public class ConnectionStringOptions
    {
        public const string SectionName = "ConnectionStrings";

        public string StudentDb { get; set; } = string.Empty;
    }
}
