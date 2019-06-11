namespace BlazorPrettyCode.Themes
{
    public class VisualStudioSolarizedLight : ICodeTheme
    {
        public string Name => "Visual Studio - Solarized Light";
        public string Pre => "background-color: #FDF6E3;";
        public string TagSymbols => "color: #93A1A1;";
        public string TagName => "color: #268BD2;";
        public string AttributeName => "color: #93A1A1;";
        public string AttributeValue => "color: #2AA198;";
        public string Text => "color: #000;";
        public string QuotedString => "color: #2AA198;";
        public string CSHtmlKeyword => @"background-color: yellow;
                                         color: black;";
    }
}
