namespace SharedKernel.Domain.Attributes;
public interface IAssignable
{
    public DateTimeOffset? AssignedAt { get; set; }
    public string? AssignedBy { get; set; }

    void MarkAsAssigned(string? userId = default);
}
