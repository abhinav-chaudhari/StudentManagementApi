using Microsoft.AspNetCore.Identity;

namespace WebApplication1.Setup
{
    public static class AdminBootstrapper
    {
        public static async Task RunAsync(IServiceProvider services, IConfiguration configuration)
        {
            string email = configuration["AdminBootstrap:Email"]?.Trim() ?? throw new InvalidOperationException("Admin bootstrap email is missing.");

            if (string.IsNullOrEmpty(email))
                throw new InvalidOperationException("Admin bootstrap email is empty");

            using var scope = services.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var existing = await users.FindByEmailAsync(email);

            if (existing != null) 
            {
                if (await users.IsInRoleAsync(existing, "Admin"))
                    return;

                throw new InvalidOperationException("This email already belongs to a non-admin account." +" Use new dedicated bootstrap email");
            }
            string password = configuration["AdminBootstrap:Password"]?.Trim() ?? throw new InvalidOperationException("Admin bootstrap password is missing.");
            if(!await roles.RoleExistsAsync("Admin"))
            {
                EnsureSuccess(await roles.CreateAsync(new IdentityRole("Admin")), "Create Admin role.");
            }
            var user = new IdentityUser
            {
                UserName = email,
                Email = email
            };
            EnsureSuccess(await users.CreateAsync(user, password), "Create Administrator.");

            EnsureSuccess(await users.AddToRoleAsync(user, "Admin"), "Assign Admin role.");
        }
        private static void EnsureSuccess(IdentityResult result, string operation)
        {
            if (!result.Succeeded)
            {
                string errors = string.Join("; ", result.Errors.Select(e => e.Description));

                throw new InvalidOperationException($"{operation} falied: {errors}");
            }
        }
    }
}
