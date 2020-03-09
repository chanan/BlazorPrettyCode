using BlazorPrettyCode;
using BlazorTypography;
using Microsoft.AspNetCore.Blazor.Hosting;
using SamplePages;
using System.Threading.Tasks;

namespace Sample
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            //Configure Services
            builder.Services.AddBlazorPrettyCode();
            builder.Services.AddTypography();
            //End Configure Services

            builder.RootComponents.Add<App>("app");

            await builder.Build().RunAsync();
        }
    }
}
