using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace WebApplication1.Tests
{
    public static class TestAuthentication
    {
        public static async Task RegisterAndLoginAsync(HttpClient client, StudentApiFactory factory, bool asAdmin = false)
        {
            var credentials = new
            {
                email = $"{Guid.NewGuid():N}@example.com",
                password = "Pass@1234"
            };

            using var registration = await client.PostAsJsonAsync("/api/auth/register", credentials);
            Assert.Equal(HttpStatusCode.OK, registration.StatusCode);

            if (asAdmin)
            {
                using var scope = factory.Services.CreateScope();
                var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

                if (!await roles.RoleExistsAsync("Admin"))
                {
                    var roleResult = await roles.CreateAsync(new IdentityRole("Admin"));
                    Assert.True(roleResult.Succeeded, string.Join("; ", roleResult.Errors.Select(e => e.Description)));
                }

                var user = await users.FindByEmailAsync(credentials.email);
                Assert.NotNull(user);

                var assignment = await users.AddToRoleAsync(user, "Admin");
                Assert.True(assignment.Succeeded, string.Join("; ", assignment.Errors.Select(e => e.Description)));
            }

            using var login = await client.PostAsJsonAsync("/api/auth/login?useCookies=false", credentials);
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);

            var tokens = await login.Content.ReadFromJsonAsync<LoginTokenResponse>();

            Assert.NotNull(tokens);
            Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        }

        private sealed record LoginTokenResponse(string AccessToken);
    }
}
