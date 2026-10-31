using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rummy51.Application.Authentication;
using Rummy51.Domain.Entities;
using Rummy51.Infrastructure.Identity;
using Rummy51.Infrastructure.Persistence;

namespace Rummy51.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Rummy51Db")
            ?? throw new InvalidOperationException(
                "Connection string 'Rummy51Db' was not found. In development set it with " +
                "'dotnet user-secrets set \"ConnectionStrings:Rummy51Db\" \"...\"'.");

        services.AddDbContext<Rummy51DbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        services.AddIdentityCore<Player>(options =>
            {
                // The email doubles as the login identifier, so it has to be unique.
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<Rummy51DbContext>();

        // Injected rather than read from DateTimeOffset.UtcNow, so that a test
        // can pin the clock when it checks CreatedAt.
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<IPlayerAccountService, PlayerAccountService>();

        return services;
    }
}
