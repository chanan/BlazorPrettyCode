using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace BlazorPrettyCode.Themes
{
    internal class ThemeCache
    {
        public ConcurrentDictionary<string, string> Cache { get; set; } = new ConcurrentDictionary<string, string>();
    }
}
