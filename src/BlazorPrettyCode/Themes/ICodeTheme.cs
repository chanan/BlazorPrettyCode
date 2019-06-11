namespace BlazorPrettyCode.Themes
{
    public interface ICodeTheme
    {
        string Name { get; }
        string Pre { get; }
        string TagSymbols { get; }
        string TagName { get; }
        string AttributeName { get; }
        string AttributeValue { get; }
        string Text { get; }
        string QuotedString { get; }
        string CSHtmlKeyword { get; }
    }
}
