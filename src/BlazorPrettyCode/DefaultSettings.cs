namespace BlazorPrettyCode
{
    public class DefaultSettings
    {
        public bool IsDevelopmentMode { get; set; }
        public string DefaultTheme { get; set; } = "PrettyCodeDefault";
        public bool ShowLineNumbers { get; set; } = true;
        public bool ShowException { get; set; } = true;
        public bool ShowCollapse { get; set; } = false;
        public bool IsCollapsed { get; set; } = false;
        public bool AttemptToFixTabs { get; set; } = true;
        public bool KeepOriginalLineNumbers { get; set; }
    }
}
