using BlazorPrettyCode;
using BlazorTypography;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SamplePages;
using System.Threading.Tasks;
using System.Net.Http;
using System;

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

            builder.Services.AddSingleton(new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            builder.RootComponents.Add<App>("app");

            await builder.Build().RunAsync();
        }
    }
}
