namespace BlazorPrettyCode.Themes
{
    public class PrettyCodeDefault : ICodeTheme
    {
        public string Name => "Pretty Code - Default";
        public string Pre => "background-color: rgba(238,238,238,0.92);";
        public string TagSymbols => "color: #03c;";
        public string TagName => @"color: #03c;
                                   font-weight: bold;";
        public string AttributeName => @"color: #36c;
                                         font-style: italic;";
        public string AttributeValue => "color: #093;";
        public string Text => "color: #000;";
        public string QuotedString => "color: #093;";
        public string CSHtmlKeyword => @"background-color: yellow;
                                         color: black;";

    }
}