using BlazorPrettyCode.Themes;

namespace BlazorPrettyCode
{
    public class DefaultSettings
    {
        public bool IsDevelopmentMode { get; set; }
        public string DefaultTheme { get; set; } = "PrettyCodeDefault";
        public bool ShowLineNumbers { get; set; } = true;
    }
}
