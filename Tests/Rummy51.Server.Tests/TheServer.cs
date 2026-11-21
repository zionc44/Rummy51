using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

    public Logged Logged { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Not Development, where the framework puts its own exception page in
        // front of everything - and what is handed to a client instead of an
        // exception is one of the three things this suite is here for.
        builder.UseEnvironment("Testing");

        // Read while the host is being built and thrown over when missing,
        // which is earlier than anything a test could add afterwards. Nothing
        // reaches it, which is only true because of the key ring below.
        builder.UseSetting("ConnectionStrings:Rummy51Db", "Host=nowhere");

        builder.ConfigureTestServices(services =>
        {
            services.AddScoped<IPlayerAccountService>(_ => Accounts);

            // The one thing in the pipeline that goes to the database by
            // itself: the cookie wants a protector, and the keys are kept in
            // a table. Without this every run spends seconds failing to
            // resolve a host that is not there, and says so at length.
            services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());

            // Nothing reaches the console. An error written here is one a
            // test can be held to, which is the only way to tell the one
            // thrown on purpose from one nobody asked for.
            services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddProvider(Logged);
            });
        });
    }

    /// <summary>Following one would hide the redirect this project replaced.</summary>
    public HttpClient Caller() =>
        CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
}
