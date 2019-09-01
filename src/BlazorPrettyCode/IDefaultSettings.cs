using System;

namespace BlazorPrettyCode
{
    public interface IDefaultSettings
    {
        bool AttemptToFixTabs { get; set; }
        string DefaultTheme { get; set; }
        bool IsCollapsed { get; set; }
        bool IsDevelopmentMode { get; set; }
        bool KeepOriginalLineNumbers { get; set; }
        bool ShowCollapse { get; set; }
        bool ShowException { get; set; }
        bool ShowLineNumbers { get; set; }
        IDisposable Subscribe(IObserver<IDefaultSettings> observer);
    }
}