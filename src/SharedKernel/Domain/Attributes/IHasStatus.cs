namespace SharedKernel.Domain.Attributes;
public interface IHasStatus<T> where T : Enum
{
    T Status { get; set; } 
}
