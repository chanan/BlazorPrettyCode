using BlazorPrettyCode;
using BlazorStyled;
using BlazorTypography;
using Microsoft.AspNetCore.Components.Builder;
using Microsoft.Extensions.DependencyInjection;
using SamplePages;

namespace Sample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddBlazorPrettyCode();
            services.AddTypography();
        }

        public void Configure(IComponentsApplicationBuilder app)
        {
            app.AddComponent<App>("app");
            app.AddComponent<ClientSideStyled>("#styled");
        }
    }
}
