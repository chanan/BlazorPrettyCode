using BlazorStyled;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BlazorPrettyCode
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBlazorPrettyCode(this IServiceCollection serviceCollection, Action<DefaultSettings> defaultSettings)
        {
            DefaultSettings defaultSettingsObj = new DefaultSettings();
            defaultSettings(defaultSettingsObj);
            serviceCollection.AddSingleton(defaultSettingsObj);
            return serviceCollection.AddBlazorStyled(defaultSettingsObj.IsDevelopmentMode);
        }

        public static IServiceCollection AddBlazorPrettyCode(this IServiceCollection serviceCollection)
        {
            return serviceCollection.AddBlazorPrettyCode(defaultSettings =>
            {
                defaultSettings.IsDevelopmentMode = false;
                defaultSettings.DefaultTheme = "PrettyCodeDefault";
            });
        }
    }
}
