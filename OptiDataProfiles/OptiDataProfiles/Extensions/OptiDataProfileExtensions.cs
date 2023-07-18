using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace OptiDataProfiles;

public static class OptiDataProfileExtensions
{
    public static IServiceCollection AddOptiDataProfile(this IServiceCollection services)
    {
        return AddOptiDataProfile(services, _ => { });
    }

    public static IServiceCollection AddOptiDataProfile(this IServiceCollection services, Action<OptiDataProfileOptions> setupAction)
    {
        services.AddOptions<OptiDataProfileOptions>()
            .Configure<IConfiguration>((options, configuration) =>
            {
                setupAction(options);
                configuration.GetSection("OptiDataProfile").Bind(options);
            });

        services.AddHttpClient<IOptiDataProfileService, OptiDataProfileService>(client =>
        {
            client.BaseAddress = new Uri("https://api.zaius.com/v3/profiles");
        });

        services.TryAddSingleton<IOptiDataProfileService, OptiDataProfileService>();

        return services;
    }
}