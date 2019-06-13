using System.Collections.Generic;

namespace BlazorPrettyCode.Themes
{
    public interface ITheme
    {
        string Name { get; }
        List<ISetting> Settings { get; }
    }
}
