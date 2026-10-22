using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using WebApplication1.Configuration;
using WebApplication1.Data;
using WebApplication1.Security;
using WebApplication1.Services;
using WebApplication1.Setup;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using WebApplication1.Middlerware;
using WebApplication1.Errors;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        Description = "Paste the access token returned by login."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IBranchService, BranchService>();
//builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("StudentDb")));
builder.Services.AddDbContext<AppDbContext>((services, options) => { var connectionStrings = services.GetRequiredService<IOptions<ConnectionStringOptions>>().Value; options.UseSqlite(connectionStrings.StudentDb); });
builder.Services.AddIdentityApiEndpoints<IdentityUser>(options => options.User.RequireUniqueEmail = true).AddRoles<IdentityRole>().AddEntityFrameworkStores<AppDbContext>();
builder.Services.AddAuthorization(options => { options.AddPolicy(AuthorizationPolicies.ManageSchoolData, policy => { policy.RequireAuthenticatedUser(); policy.RequireRole("Admin"); }); });
builder.Services.AddOptions<ConnectionStringOptions>().Bind(builder.Configuration.GetSection(ConnectionStringOptions.SectionName)).Validate(options => !string.IsNullOrWhiteSpace(options.StudentDb), "ConnectionStrings:StudentDb is required.").ValidateOnStart();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>(name: "database", tags: new[] { "ready" });
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
var app = builder.Build();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseExceptionHandler();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if(app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("AdminBootstrap:Enabled"))
{
    await AdminBootstrapper.RunAsync(app.Services, app.Configuration);
}
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGroup("api/auth").MapIdentityApi<IdentityUser>();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") }).AllowAnonymous();
app.Run();
public partial class Program { }