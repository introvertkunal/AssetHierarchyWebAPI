using AssetHierarchyWebAPI.Domain.Entities;
using AssetHierarchyWebAPI.Domain.Entities.Auth;
using AssetHierarchyWebAPI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace AssetHierarchyWebAPI.Infrastructure.Data
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(RoleManager<IdentityRole> roleManager, UserManager<AppUser> userManager, AssetContext dbContext)
        {
            // Ensure roles
            foreach (var role in new[] { "Admin", "User" })
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Create Admin
            var adminUserName = "administration";
            var adminEmail = "administration123@gmail.com";
            var admin = await userManager.FindByNameAsync(adminUserName);

            if (admin == null)
            {
                admin = new AppUser
                {
                    UserName = adminUserName,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(admin, "Kun@l989");
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
                else
                {
                    var errors = string.Join("; ", createResult.Errors.Select(e => $"{e.Code}:{e.Description}"));
                    throw new Exception($"Failed to create admin user: {errors}");
                }
            }

            dbContext.SignalValues.RemoveRange(dbContext.SignalValues);
            await dbContext.SaveChangesAsync();

            var random = new Random();
            var signalValues = new List<SignalValue>();

            for (int i = 0; i < 2000000; i++)
            {
                signalValues.Add(new SignalValue
                {
                    SignalValueData = random.NextDouble() * 1000, 
                    SignalId = random.Next(1, 21),               
                    RecordedAt = DateTime.UtcNow
                });
            }

            await dbContext.SignalValues.AddRangeAsync(signalValues);
            await dbContext.SaveChangesAsync();
        }
    }
}