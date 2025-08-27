namespace SharedKernel.Domain.Attributes;
public interface ISoftDelete
{
    /// <summary>
    /// 
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Marks the entity as deleted.
    /// </summary>
    void MarkAsDeleted();
}