using Microsoft.Extensions.DependencyInjection;
using Cis.Application.Common.Interfaces;
using Cis.Application.Identity;

namespace Cis.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<ISegregationOfDutiesService, SegregationOfDutiesService>();
        return services;
    }
}
