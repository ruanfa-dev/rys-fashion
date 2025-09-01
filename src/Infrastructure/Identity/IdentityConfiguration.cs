using Core.Identity;

using Infrastructure.Persistence.Contexts;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Identity;

public static class IdentityConfiguration
{
    /// <summary>
    /// Alternative configuration using IdentityCore with manually added services
    /// Use this if you specifically need IdentityCore instead of full Identity
    /// </summary>
    public static IServiceCollection AddShopIdentityCore(this IServiceCollection services)
    {
        services
            .AddIdentityCore<User>(options =>
            {
                // Password rules — balanced for e-shops
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;  // don't frustrate customers
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 1;

                // Lockout — stops brute-force
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // Users
                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters =
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

                // Sign-in flow
                options.SignIn.RequireConfirmedEmail = true;
                options.SignIn.RequireConfirmedPhoneNumber = false;
            })
            .AddRoles<Role>()                                   // role support for admin/customer split
            .AddEntityFrameworkStores<ApplicationDbContext>()   // EF Core persistence
            .AddDefaultTokenProviders()                         // for password reset, email confirm, etc.
            .AddSignInManager<SignInManager<User>>()            // *** This is the missing piece ***
            .AddRoleManager<RoleManager<Role>>()                // Role management
            .AddUserManager<UserManager<User>>();               // User management

        // Token lifespan tuning (affects email confirmation / reset tokens)
        services.Configure<DataProtectionTokenProviderOptions>(o =>
        {
            o.TokenLifespan = TimeSpan.FromHours(2); // common practice for shops
        });


        return services;
    }

    /// <summary>
    /// Recommended configuration using full Identity (simpler and more complete)
    /// </summary>
    public static IServiceCollection AddShopIdentity(this IServiceCollection services)
    {
        services
            .AddIdentity<User, Role>(options =>
            {
                // Password rules — balanced for e-shops
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 4;

                // Lockout — stops brute-force
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // Users
                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters =
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

                // Sign-in flow
                options.SignIn.RequireConfirmedEmail = true;
                options.SignIn.RequireConfirmedPhoneNumber = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()   // EF Core persistence
            .AddDefaultTokenProviders();                        // for password reset, email confirm, etc.

        // Token lifespan tuning (affects email confirmation / reset tokens)
        services.Configure<DataProtectionTokenProviderOptions>(o =>
        {
            o.TokenLifespan = TimeSpan.FromHours(2); // common practice for shops
        });

        return services;
    }
}