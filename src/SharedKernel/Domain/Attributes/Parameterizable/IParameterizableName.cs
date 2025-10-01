namespace SharedKernel.Domain.Attributes.Parameterizable;
/// <summary>
/// Port of Spree's ParameterizableName concern.
/// Entities implementing this interface expose the minimal surface required
/// for the shared service & query helpers to operate (name / presentation).
/// </summary>
public interface IParameterizableName
{
    string Name { get; set; }
    string Presentation { get; set; }
}
