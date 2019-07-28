using BlazorPrettyCode;
using Microsoft.AspNetCore.Components.Builder;
using Microsoft.Extensions.DependencyInjection;
using Polished;
using SamplePages;

namespace Sample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddBlazorPrettyCode();
            services.AddSingleton<IMixins, Mixins>();
            services.AddSingleton<IShorthand, Shorthand>();
        }

        public void Configure(IComponentsApplicationBuilder app)
        {
            app.AddComponent<App>("app");
            app.AddClientSidePrettyCode();
        }
    }
}
