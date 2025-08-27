namespace SharedKernel.Domain.Attributes;

public interface IAuditable
{
    DateTimeOffset CreatedAt { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }
    string? CreatedBy { get; set; }
    string? UpdatedBy { get; set; }

    void MarkAsCreated(string? userId = default);
    void MarkAsUpdated(string? userId = default);
}