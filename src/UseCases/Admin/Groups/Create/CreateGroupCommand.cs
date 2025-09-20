using ErrorOr;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Admin.Groups.Create;

public static partial class CreateGroupCommand
{
    public const string Name = nameof(CreateGroupCommand);
    public const string Summary = "Create new group in Keycloak";
    public const string Description = "Creates a new group in Keycloak realm for organizing users";

    public record Command(CreateGroupParam Param) : ICommand<CreateGroupResult>;

    public record CreateGroupParam
    {
        public string Name { get; init; } = string.Empty;
        public string? Path { get; init; }
        public Dictionary<string, List<string>>? Attributes { get; init; }
        public List<string>? RealmRoles { get; init; }
    }

    public record CreateGroupResult
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Path { get; init; } = string.Empty;
    }
}