using System.Collections.Generic;

namespace BlazorPrettyCode.Themes
{
    public interface ISetting
    {
        string Name { get; set; }
        string Scope { get; set; }
        Dictionary<string, string> Settings { get; set; }
    }
}