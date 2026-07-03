using CPElite.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CPElite.Tests.Integration.Support;

public sealed class ApiTestFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"CPEliteTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EaSync:Enabled"] = "false",
                ["DiscordBot:ApiKey"] = "test-discord-key"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<CPEliteDbContext>>();
            services.RemoveAll<DbContextOptions<CPEliteDbContext>>();
            services.AddDbContext<CPEliteDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }
}
