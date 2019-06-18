using System.Collections.Generic;

namespace BlazorPrettyCode.Themes
{
    public class Setting
    {
        public string Name { get; set; }
        public string Scope { get; set; }
        public Dictionary<string, string> Settings { get; set; }
    }
}