namespace Core.Identity;
public partial class UserRole
{
    public static class Constraints
    {
        // Roles: Minimum 1, Maximum 10 roles in the system
        public const int MinRolePerUser = 1;
        public const int MaxRolePerUser = 10;

        // Users: Minimum 1, Maximum 100 assignable role per user, Maximum 10000 users per role
        public const int MinUsersPerRole = 0;
        public const int MaxUsersPerRole = 1000;
    }
}