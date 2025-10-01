namespace SharedKernel.Domain.Attributes.Assignable;
public interface IAssignable
{
    public DateTimeOffset? AssignedAt { get; set; }
    public string? AssignedBy { get; set; }

}
