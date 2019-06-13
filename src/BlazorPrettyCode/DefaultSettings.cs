using BlazorPrettyCode.Themes;

namespace BlazorPrettyCode
{
    public class DefaultSettings
    {
        public bool IsDevelopmentMode { get; set; }
        public ITheme DefaultTheme { get; set; } = new PrettyCodeDefault();
        public bool ShowLineNumbers { get; set; } = true;
    }
}
