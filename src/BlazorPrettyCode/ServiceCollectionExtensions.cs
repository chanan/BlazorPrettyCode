using BlazorStyled;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorPrettyCode
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBlazorPrettyCode(this IServiceCollection serviceCollection, bool isDevelopment)
        {
            return serviceCollection.AddBlazorStyled(isDevelopment);
        }

        public static IServiceCollection AddBlazorPrettyCode(this IServiceCollection serviceCollection)
        {
            return serviceCollection.AddBlazorPrettyCode(false);
        }
    }
}
