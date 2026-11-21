using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Rummy51.Server.Tests;

/// <summary>
/// The real pipeline, booted. Nothing in it is replaced - not the cookie,
/// not the handler, not the model binding - because what crosses the wire
/// is the whole of what this suite asks about. Only the account service is
/// a double, being the one thing behind the wire that wants a database.
/// </summary>
public sealed class TheServer : WebApplicationFactory<Program>
{
    public Accounts Accounts { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Not Development, where the framework puts its own exception page in
        // front of everything - and what is handed to a client instead of an
        // exception is one of the three things this suite is here for.
        builder.UseEnvironment("Testing");

        // Read while the host is being built and thrown over when missing,
        // which is earlier than anything a test could add afterwards. Nothing
        // here reaches a database, so what it says does not matter.
        builder.UseSetting("ConnectionStrings:Rummy51Db", "Host=nowhere");

        builder.ConfigureTestServices(services =>
            services.AddScoped<IPlayerAccountService>(_ => Accounts));
    }

    /// <summary>Following one would hide the redirect this project replaced.</summary>
    public HttpClient Caller() =>
        CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
}
