using AssetHierarchyWebAPI.Models;
using Microsoft.AspNetCore.Identity;

namespace AssetHierarchyWebAPI.Data
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(RoleManager<IdentityRole> roleManager, UserManager<AppUser> userManager)
        {
            // Ensure roles
            foreach (var role in new[] { "Admin", "User" })
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Create Admin
            var adminUserName = "admin";
            var adminEmail = "admin123@gmail.com";
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
        }
    }
}
