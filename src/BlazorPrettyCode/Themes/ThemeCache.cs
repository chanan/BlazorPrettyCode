using System.Collections.Concurrent;

namespace BlazorPrettyCode.Themes
{
    internal class ThemeCache
    {
        public ConcurrentDictionary<string, string> Cache { get; set; } = new ConcurrentDictionary<string, string>();
    }
}
