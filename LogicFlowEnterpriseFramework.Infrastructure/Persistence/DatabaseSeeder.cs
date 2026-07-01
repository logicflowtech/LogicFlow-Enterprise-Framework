using LogicFlowEnterpriseFramework.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LogicFlowEnterpriseFramework.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static readonly Guid DefaultTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static async Task SeedAsync(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IOptions<BootstrapAdminOptions> bootstrapAdminOptions)
    {
        await dbContext.Database.MigrateAsync();

        if (!await dbContext.Tenants.IgnoreQueryFilters().AnyAsync(x => x.Id == DefaultTenantId))
        {
            dbContext.Tenants.Add(new Tenant
            {
                Id = DefaultTenantId,
                Name = "Default Tenant",
                Identifier = "default",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "seed"
            });
            await dbContext.SaveChangesAsync();
        }

        await EnsureBootstrapAdminAsync(dbContext, userManager, roleManager, bootstrapAdminOptions.Value);
    }

    private static async Task EnsureBootstrapAdminAsync(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        BootstrapAdminOptions bootstrapAdminOptions)
    {
        if (string.IsNullOrWhiteSpace(bootstrapAdminOptions.Email) || string.IsNullOrWhiteSpace(bootstrapAdminOptions.InitialPassword))
        {
            return;
        }

        const string adminRoleName = "Admin";
        if (!await roleManager.RoleExistsAsync(adminRoleName))
        {
            var roleResult = await roleManager.CreateAsync(new ApplicationRole(adminRoleName));
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", roleResult.Errors.Select(error => error.Description)));
            }
        }

        var user = await userManager.FindByEmailAsync(bootstrapAdminOptions.Email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Email = bootstrapAdminOptions.Email,
                UserName = bootstrapAdminOptions.Email,
                FullName = string.IsNullOrWhiteSpace(bootstrapAdminOptions.FullName) ? "System Administrator" : bootstrapAdminOptions.FullName,
                TenantId = DefaultTenantId,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "bootstrap-seed"
            };

            var createResult = await userManager.CreateAsync(user, bootstrapAdminOptions.InitialPassword);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(error => error.Description)));
            }
        }
        else
        {
            var requiresUpdate = false;

            if (!user.IsActive)
            {
                user.IsActive = true;
                requiresUpdate = true;
            }

            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                requiresUpdate = true;
            }

            if (!string.Equals(user.UserName, bootstrapAdminOptions.Email, StringComparison.OrdinalIgnoreCase))
            {
                user.UserName = bootstrapAdminOptions.Email;
                requiresUpdate = true;
            }

            if (user.LockoutEnd.HasValue)
            {
                user.LockoutEnd = null;
                requiresUpdate = true;
            }

            if (requiresUpdate)
            {
                var updateResult = await userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    throw new InvalidOperationException(string.Join("; ", updateResult.Errors.Select(error => error.Description)));
                }
            }

            await userManager.ResetAccessFailedCountAsync(user);
        }

        if (!await userManager.IsInRoleAsync(user, adminRoleName))
        {
            var addRoleResult = await userManager.AddToRoleAsync(user, adminRoleName);
            if (!addRoleResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", addRoleResult.Errors.Select(error => error.Description)));
            }
        }
    }
}
