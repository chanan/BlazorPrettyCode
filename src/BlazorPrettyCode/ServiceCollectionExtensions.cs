using BlazorPrettyCode.Internal;
using BlazorPrettyCode.Themes;
using BlazorStyled;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BlazorPrettyCode
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBlazorPrettyCode(this IServiceCollection serviceCollection, Action<IDefaultSettings> defaultSettings)
        {
            IDefaultSettings defaultSettingsObj = new DefaultSettings();
            defaultSettings(defaultSettingsObj);
            serviceCollection.AddSingleton<IDefaultSettings>(defaultSettingsObj);
            serviceCollection.AddScoped<ThemeCache>();
            serviceCollection.AddBlazorStyled(isDevelopment: defaultSettingsObj.IsDevelopmentMode, isDebug: defaultSettingsObj.IsDevelopmentMode);
            return serviceCollection;
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
