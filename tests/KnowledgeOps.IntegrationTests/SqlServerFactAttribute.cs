namespace KnowledgeOps.IntegrationTests;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")))
        {
            Skip = "Set ConnectionStrings__DefaultConnection to run SQL Server persistence integration tests.";
        }
    }
}
