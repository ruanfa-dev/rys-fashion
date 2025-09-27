using SharedKernel.Messaging;

namespace Core.Identity.Users;
public partial class User
{
    public record Events
    {
        public const string UserCreated = "User.Created";
        public const string UserUpdated = "User.Updated";
        public const string UserDeleted = "User.Deleted";
        public const string UserPasswordChanged = "User.PasswordChanged";
        public const string UserEmailConfirmed = "User.EmailConfirmed";
        public const string UserPhoneNumberConfirmed = "User.PhoneNumberConfirmed";
        public const string UserLockoutEnabled = "User.LockoutEnabled";
        public const string UserLockoutDisabled = "User.LockoutDisabled";
        public const string UserTwoFactorEnabled = "User.TwoFactorEnabled";
        public const string UserTwoFactorDisabled = "User.TwoFactorDisabled";
        public const string UserRoleAdded = "User.RoleAdded";
        public const string UserRoleRemoved = "User.RoleRemoved";
        public const string UserClaimAdded = "User.ClaimAdded";
        public const string UserClaimRemoved = "User.ClaimRemoved";

        public sealed record UserRegistered(Guid UserId) : DomainEvent;
    }
}
