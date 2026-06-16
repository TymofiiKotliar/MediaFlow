namespace MediaFlow.Infrastructure.Persistence.Documents;

internal sealed class NamingTokenDocument
{
    public string Type { get; set; } = "";
    public string? Text { get; set; }
}
