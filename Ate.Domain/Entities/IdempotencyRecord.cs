namespace Ate.Domain.Entities;

public class IdempotencyRecord
{
    public string Id { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public bool Completed { get; set; }
    public int? StatusCode { get; set; }
    public string? Body { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
