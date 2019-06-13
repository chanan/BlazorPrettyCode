using System.Collections.Generic;

namespace BlazorPrettyCode.Themes
{
    internal class Setting : ISetting
    {
        public string Name { get; set; }
        public string Scope { get; set; }
        public Dictionary<string, string> Settings { get; set; }
    }
}
