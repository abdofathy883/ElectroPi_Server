using ElectroPi.Domain.Entities;
using ElectroPi.Domain.Enums;
using ElectroPi.Infrastructure.Persistance;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Identity
{
    public static class AuthSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var context = serviceProvider.GetRequiredService<AppDbContext>();

            // Seed Roles
            var roles = new[] {
                UserRole.Admin.ToString(),
                UserRole.Agent.ToString(),
                UserRole.Customer.ToString()
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Seed SuperAdmin
            string adminPhone = "01028128912";

            var existingUser = await userManager.FindByNameAsync(adminPhone);
            if (existingUser == null)
            {
                var admin = new AppUser
                {
                    FullName = "Abdo Fathy",
                    UserName = adminPhone,
                    Email = "abdofathy883@gmail.com",
                    EmailConfirmed = true,
                    PhoneNumber = "01028128912",
                    PhoneNumberConfirmed = true
                };

                var result = await userManager.CreateAsync(admin, "Aa123#");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
                else
                {
                    throw new Exception($"Failed to create Admin: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
