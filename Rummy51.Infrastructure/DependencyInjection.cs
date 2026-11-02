using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rummy51.Application.Authentication;
using Rummy51.Application.Lobby;
using Rummy51.Domain.Entities;
using Rummy51.Infrastructure.Identity;
using Rummy51.Infrastructure.Lobby;
using Rummy51.Infrastructure.Persistence;

namespace Rummy51.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddPlayerIdentity();

        services.Configure<AuthenticationSettings>(
            configuration.GetSection(AuthenticationSettings.SectionName));

        // Injected rather than read from DateTimeOffset.UtcNow, so a test can
        // pin the clock.
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<IPlayerAccountService, PlayerAccountService>();

        // Singleton: the roster is the shared state, not a per-request view of it.
        services.AddSingleton<IPlayerConnections, InMemoryPlayerConnections>();

        return services;
    }

    private static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Rummy51Db")
            ?? throw new InvalidOperationException("Connection string 'Rummy51Db' was not found.");

        services.AddDbContext<Rummy51DbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        // On disk these are lost whenever the host is replaced, which signs
        // every player out.
        services.AddDataProtection().PersistKeysToDbContext<Rummy51DbContext>();
    }

    private static void AddPlayerIdentity(this IServiceCollection services) =>
        services
            .AddIdentityCore<Player>(options =>
            {
                // The email doubles as the login identifier, so it has to be unique.
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<Rummy51DbContext>()
            .AddSignInManager();
}
