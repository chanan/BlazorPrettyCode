using Blazorous;

namespace BlazorPrettyCode.Themes
{
    public interface ICodeTheme
    {
        string Name { get; }
        ICss Pre { get; }
        ICss TagSymbols { get; }
        ICss TagName { get; }
        ICss AttributeName { get; }
        ICss AttributeValue { get; }
        ICss Text { get; }
        ICss QuotedString { get; }
        ICss CSHtmlKeyword { get; }
    }
}
