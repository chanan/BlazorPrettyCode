using BlazorPrettyCode.Themes;
using BlazorStyled;
using CSHTMLTokenizer;
using CSHTMLTokenizer.Tokens;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BlazorPrettyCode
{
    public class PrettyCode : ComponentBase
    {
        [Parameter] private bool? Debug { get; set; } = null;
        [Parameter] private string CodeFile { get; set; }
        [Parameter] private string CodeFileLineNumbers { get; set; }
        [Parameter] private string CodeSectionFile { get; set; }
        [Parameter] private string CodeSectionFileLineNumbers { get; set; }
        [Parameter] private string Theme { get; set; }
        [Parameter] private bool? ShowLineNumbers { get; set; } = null;
        [Parameter] private string HighlightLines { get; set; }
        [Parameter] private bool? ShowException { get; set; } = null;
        [Parameter] private bool? ShowCollapse { get; set; } = null;
        [Parameter] private string Title { get; set; } = null;
        [Parameter] private bool? IsCollapsed { get; set; } = null;
        [Parameter] private bool? AttemptToFixTabs { get; set; } = null;

        private List<Line> Lines { get; set; } = new List<Line>();
        private int i = 0;
        private List<int> _highlightLines = new List<int>();
        private int _lineNum = 1;

        //Config variables
        private bool _showLineNumbers;
        private bool _showCollapse;
        private bool _isCollapsed;
        private bool _attemptToFixTabs;

        //Theme css
        private string _themePreClass;
        private string _themeTagSymbolsClass;
        private string _themeTagNameClass;
        private string _themeAttributeNameClass;
        private string _themeAttributeValueClass;
        private string _themeQuotedStringClass;
        private string _themeRazorKeywordClass;
        private string _themeTextClass;
        private string _themeRowHighlight;
        private string _themeRowHighlightBorder;

        //Non Theme css
        private string _basePreClass;
        private string _baseRowSpan;
        private string _baseLineNumbersCell;
        private string _baseCodeCell;
        private string _baseRowSpanTitle;
        private string _baseCellSpacer;
        private string _baseCellTitle;
        private string _baseCollapse;
        private string _baseDiv;

        [Inject] private HttpClient HttpClient { get; set; }
        [Inject] private IStyled Styled { get; set; }
        [Inject] private DefaultSettings DefaultConfig { get; set; }
        [Inject] private ThemeCache ThemeCache { get; set; }

        private IStyled _styled;

        protected override async Task OnInitAsync()
        {
            _styled = Styled.WithId("pretty-code");
            bool debug = Debug ?? DefaultConfig.IsDevelopmentMode;
            bool showException = ShowException ?? DefaultConfig.ShowException;
            _showCollapse = ShowCollapse ?? DefaultConfig.ShowCollapse;
            _isCollapsed = IsCollapsed ?? DefaultConfig.IsCollapsed;
            _attemptToFixTabs = AttemptToFixTabs ?? DefaultConfig.AttemptToFixTabs;

            await InitSourceFile(showException);

            if (debug)
            {
                PrintToConsole();
            }

            InitCSS();
            await InitThemeCss();
        }

        private async Task InitSourceFile(bool showException)
        {
            string CodeFileString = await GetFromCacheOrNetwork(CodeFile);
            string codeFileLinesString = GetLines(CodeFileString, CodeFileLineNumbers);
            string codeSectionFileLinesString = string.Empty;
            if (!string.IsNullOrWhiteSpace(CodeSectionFile) || !string.IsNullOrWhiteSpace(CodeSectionFileLineNumbers))
            {
                string codeSectionString;
                if (!string.IsNullOrWhiteSpace(CodeSectionFile))
                {
                    codeSectionString = await GetFromCacheOrNetwork(CodeSectionFile);
                }
                else
                {
                    codeSectionString = CodeFileString;
                }
                codeSectionFileLinesString = GetLines(codeSectionString, CodeSectionFileLineNumbers);
                if (!codeSectionFileLinesString.StartsWith("@code"))
                {
                    codeSectionFileLinesString = $"@code {{\n{codeSectionFileLinesString}}}";
                }
            }

            string str = string.IsNullOrWhiteSpace(codeSectionFileLinesString) ? codeFileLinesString : codeFileLinesString + '\n' + codeSectionFileLinesString;

            Parse(showException, str.TrimEnd());

            if (_attemptToFixTabs)
            {
                FixTabs();
            }
        }

        private async Task<string> GetFromCacheOrNetwork(string uri)
        {
            if (ThemeCache.Cache.TryGetValue(uri, out string result))
            {
                return result;
            }
            string str = await HttpClient.GetStringAsync(uri);
            ThemeCache.Cache.TryAdd(uri, str);
            return str;
        }

        private void FixTabs()
        {
            StringBuilder ignored = new StringBuilder();
            if (Lines[0].Tokens[0].TokenType == TokenType.Text)
            {
                bool foundChar = false;
                foreach (char ch in ((Text)Lines[0].Tokens[0]).Content)
                {
                    if (!foundChar && char.IsWhiteSpace(ch))
                    {
                        ignored.Append(ch);
                    }
                    else
                    {
                        foundChar = true;
                    }
                }
                if (ignored.Length > 0)
                {
                    foreach (Line line in Lines)
                    {
                        if (line.Tokens.Count > 0 && line.Tokens[0].TokenType == TokenType.Text && ((Text)line.Tokens[0]).Content.StartsWith(ignored.ToString()))
                        {
                            string content = ((Text)line.Tokens[0]).Content.ReplaceFirst(ignored.ToString(), "");
                            Text text = new Text();
                            foreach (char ch in content)
                            {
                                text.Append(ch);
                            }
                            line.Tokens[0] = text;
                        }
                    }
                }
            }
        }

        private void Parse(bool showException, string str)
        {
            try
            {
                Lines = Tokenizer.Parse(str);
            }
            catch (Exception e)
            {
                Line line = new Line();
                if (e is TokenizationException te)
                {
                    try
                    {
                        if (te.LineNumber != 0)
                        {
                            string context = GetLines(str, te.LineNumber.ToString());
                            line = new Line() { Tokens = new List<IToken> { new Text() } };
                            foreach (char ch in context)
                            {
                                line.Tokens[0].Append(ch);
                            }
                        }
                    }
                    catch (Exception) { }
                }
                if (showException)
                {
                    try
                    {
                        Lines.Clear();
                        if (line.Tokens.Count > 0)
                        {
                            Lines.Add(line);
                        }
                        List<Line> lines = Tokenizer.Parse(e.ToString());
                        Lines.AddRange(lines);

                    }
                    catch (Exception)
                    {
                        Console.Error.WriteLine(e);
                    }
                }
                else
                {
                    Console.Error.WriteLine(e);
                }
            }
        }

        private async Task InitThemeCss()
        {
            string themeName = Theme ?? DefaultConfig.DefaultTheme;
            string uri = themeName.ToLower().StartsWith("http") ? themeName : "_content/BlazorPrettyCode/" + themeName + ".json";

            string strJson = await GetFromCacheOrNetwork(uri);

            Themes.Theme theme = JsonSerializer.Deserialize<Themes.Theme>(strJson);
            _showLineNumbers = ShowLineNumbers ?? DefaultConfig.ShowLineNumbers;

            _styled.AddGoogleFonts(GetFonts(theme));

            StringBuilder sb = new StringBuilder();
            sb.Append("overflow-y: auto;");
            IDictionary<string, string> settings = GetThemeValuesDictionary(theme);
            if (settings.ContainsKey("background-color"))
            {
                sb.Append("background-color: ").Append(settings["background-color"]).Append(';');
            }

            _baseDiv = _styled.Css(sb.ToString());
            _themePreClass = _styled.Css(GetThemeValues(theme));
            _themeTagSymbolsClass = _styled.Css(GetThemeValues(theme, "Tag symbols"));
            _themeTagNameClass = _styled.Css(GetThemeValues(theme, "Tag name"));
            _themeAttributeNameClass = _styled.Css(GetThemeValues(theme, "Attribute name"));
            _themeAttributeValueClass = _styled.Css(GetThemeValues(theme, "Attribute value"));
            _themeQuotedStringClass = _styled.Css(GetThemeValues(theme, "String"));
            _themeRazorKeywordClass = _styled.Css(GetThemeValues(theme, "Razor Keyword"));
            _themeTextClass = _styled.Css(GetThemeValues(theme, "Text"));

            _highlightLines = GetLineNumbers(HighlightLines);
            if (_highlightLines.Count > 0)
            {
                _themeRowHighlight = _styled.Css(GetThemeValues(theme, "Row Highlight"));

                IDictionary<string, string> dictionary = GetThemeValuesDictionary(theme, "Row Highlight");
                string color = dictionary.ContainsKey("background-color") ? dictionary["background-color"] : "rgba(0, 0, 0, 0.9)";
                string border = $"border-left: 0.5rem solid {color};";
                _themeRowHighlightBorder = _styled.Css(border);
            }
        }

        private void InitCSS()
        {
            _basePreClass = _styled.Css(@"
                display: table;
                table-layout: fixed;
                width: 100%;
                -webkit-border-radius: 5px;
                @media only screen and (min-width: 320px) and (max-width: 480px) {
                    font-size: 50%;
                }
                @media (min-width: 481px) and (max-width: 1223px) {
                    font-size: 80%;
                }
                &:before {
                    counter-reset: linenum;
                }
            ");

            //Code Row

            _baseRowSpan = _styled.Css(@"
                display: table-row;
                counter-increment: linenum;
            ");

            _baseLineNumbersCell = _styled.Css(@"
                display: table-cell;
                user-select: none;
                -moz-user-select: none;
                -webkit-user-select: none;
                width: 4em;
                border-right-style: solid;
                border-right-width: 1px;
                border-right-color: rgb(223, 225, 230);
                &:before {
                    content: counter(linenum) '.';
                    text-align: right;
                    display: block;
                }
            ");

            _baseCodeCell = _styled.Css(@"
                display: table-cell;
                padding-left: 1em;
                @media only screen and (min-width: 320px) and (max-width: 480px) {
                    font-size: 50%;
                }
                @media (min-width: 481px) and (max-width: 1223px) {
                    font-size: 80%;
                }
            ");

            //Title Row

            _baseRowSpanTitle = _styled.Css(@"
                display: table-row;
            ");

            _baseCellSpacer = _styled.Css(@"
                display: table-cell;
                user-select: none;
                -moz-user-select: none;
                -webkit-user-select: none;
                width: 4em;
                border-bottom-style: solid;
                border-bottom-width: 1px;
                border-bottom-color: rgb(223, 225, 230);
            ");

            _baseCellTitle = _styled.Css(@"
                display: table-cell;
                padding: 0.4em 1em 0.4em;
                font-weight: bold;
                border-bottom-style: solid;
                border-bottom-width: 1px;
                border-bottom-color: rgb(223, 225, 230);
            ");

            // Expand / Collapse

            _baseCollapse = _styled.Css(@"
                font-weight: normal;
                font-size: 0.8em;
                display: table-cell;
                width: 10em;
                &:hover {
                    text-decoration: underline;
                    cursor: pointer;
                }
            ");
        }

        private string GetLines(string str, string lineNumbers)
        {
            List<int> codeFileLines = GetLineNumbers(lineNumbers);
            if (codeFileLines.Count > 0)
            {
                string[] arr = Regex.Split(str, "\r\n|\r|\n");
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < arr.Length; i++)
                {
                    if (codeFileLines.Contains(i + 1))
                    {
                        sb.AppendLine(arr[i]);
                    }
                }
                return sb.ToString();
            }
            else
            {
                return str;
            }
        }

        private List<int> GetLineNumbers(string strLineNumbers)
        {
            List<int> list = new List<int>();
            if (string.IsNullOrWhiteSpace(strLineNumbers))
            {
                return list;
            }

            string[] arr = strLineNumbers.Split(',');
            foreach (string str in arr)
            {
                if (str.Contains("-"))
                {
                    string[] toFromArr = str.Split('-');
                    if (int.TryParse(toFromArr[0].Trim(), out int to) && int.TryParse(toFromArr[1].Trim(), out int from))
                    {
                        foreach (int num in Enumerable.Range(to, from - to + 1))
                        {
                            list.Add(num);
                        }
                    }
                }
                else
                {
                    if (int.TryParse(str.Trim(), out int num))
                    {
                        list.Add(num);
                    }
                }
            }
            return list;
        }

        private List<GoogleFont> GetFonts(Themes.Theme theme)
        {
            List<GoogleFont> list = new List<GoogleFont>();
            List<Setting> fonts = (from s in theme.Settings
                                   where s.Name != null && s.Name.ToLower() == "font"
                                   select s).ToList();

            foreach (Setting font in fonts)
            {
                GoogleFont googleFont = new GoogleFont
                {
                    Name = font.Settings["font-family"],
                    Styles = font.Settings["font-weight"].Split(',').ToList()
                };
                list.Add(googleFont);
            }

            return list;
        }

        private IDictionary<string, string> GetThemeValuesDictionary(Themes.Theme theme, string setting = null)
        {
            IDictionary<string, string> dictionary = new Dictionary<string, string>();
            Setting settings = (from s in theme.Settings
                                where s.Name == null
                                select s).SingleOrDefault();

            if (settings != null)
            {
                foreach (KeyValuePair<string, string> kvp in settings.Settings)
                {
                    dictionary.Add(kvp.Key.ToLower(), kvp.Value);
                }
            }

            if (setting != null)
            {
                settings = (from s in theme.Settings
                            where s.Name != null && s.Name.ToLower() == setting.ToLower()
                            select s).SingleOrDefault();

                if (settings != null)
                {
                    foreach (KeyValuePair<string, string> kvp in settings.Settings)
                    {
                        if (!dictionary.ContainsKey(kvp.Key.ToLower()))
                        {
                            dictionary.Add(kvp.Key.ToLower(), kvp.Value);
                        }
                        else
                        {
                            dictionary[kvp.Key.ToLower()] = kvp.Value;
                        }
                    }
                }
            }
            return dictionary;
        }

        private string GetThemeValues(Themes.Theme theme, string setting = null)
        {
            IDictionary<string, string> dictionary = GetThemeValuesDictionary(theme, setting);
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, string> kvp in dictionary)
            {
                sb.Append(kvp.Key).Append(':').Append(kvp.Value).Append(';');
            }
            return sb.ToString();
        }

        private void PrintToConsole()
        {
            StringBuilder sb = new StringBuilder();
            foreach (Line line in Lines)
            {
                sb.Append(line.ToHtml());
            }
            Console.WriteLine(sb.ToString());
        }

        private int Next()
        {
            return i++;
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(Next(), "div");
            builder.AddAttribute(Next(), "class", _baseDiv);
            builder.OpenElement(Next(), "pre");
            builder.AddAttribute(Next(), "class", _basePreClass + " " + _themePreClass);
            if (!string.IsNullOrWhiteSpace(Title) || _showCollapse)
            {
                builder.AddContent(Next(), builderTitle => BuildRenderTitle(builderTitle));
            }
            if (!_isCollapsed)
            {
                foreach (Line line in Lines)
                {
                    builder.AddContent(Next(), builderLine => BuildRenderLine(builderLine, line));
                }
            }
            builder.CloseElement();
            builder.CloseElement();
        }

        private void BuildRenderTitle(RenderTreeBuilder builderTitle)
        {
            builderTitle.OpenElement(Next(), "span");
            builderTitle.AddAttribute(Next(), "class", _baseRowSpanTitle);
            builderTitle.OpenElement(Next(), "span");
            builderTitle.AddAttribute(Next(), "class", _baseCellSpacer);
            builderTitle.CloseElement();
            builderTitle.OpenElement(Next(), "span");
            builderTitle.AddAttribute(Next(), "class", _baseCellTitle);
            if (!string.IsNullOrWhiteSpace(Title))
            {
                builderTitle.AddContent(Next(), Title);
            }
            builderTitle.CloseElement();
            if (_showCollapse)
            {
                builderTitle.OpenElement(Next(), "span");
                builderTitle.AddAttribute(Next(), "class", _baseCollapse);
                builderTitle.AddAttribute(3, "onclick", EventCallback.Factory.Create<UIMouseEventArgs>(this, OnClick));
                builderTitle.AddContent(Next(), _isCollapsed ? "Expand source" : "Collapse source");
                builderTitle.CloseElement();
            }
            builderTitle.CloseElement();
        }

        private void OnClick()
        {
            _isCollapsed = !_isCollapsed;
        }

        private void BuildRenderLine(RenderTreeBuilder builderLine, Line line)
        {
            string highlightClass = _highlightLines.Contains(_lineNum) ? " " + _themeRowHighlight : string.Empty;
            string highlightClassBorder = _highlightLines.Contains(_lineNum) && !_showLineNumbers ? " " + _themeRowHighlightBorder : string.Empty;
            builderLine.OpenElement(Next(), "span");
            builderLine.AddAttribute(Next(), "class", _baseRowSpan);
            if (_showLineNumbers)
            {
                builderLine.OpenElement(Next(), "span");
                builderLine.AddAttribute(Next(), "class", _baseLineNumbersCell + highlightClass);
                builderLine.CloseElement();
            }
            builderLine.OpenElement(Next(), "code");
            builderLine.AddAttribute(Next(), "class", _baseCodeCell + highlightClassBorder);
            builderLine.AddContent(Next(), builderMain => BuildRenderMain(builderMain, line));
            if (!line.LastLine)
            {
                builderLine.AddContent(Next(), System.Environment.NewLine);
            }

            builderLine.CloseElement();
            builderLine.CloseElement();
            _lineNum++;
        }

        private void BuildRenderMain(RenderTreeBuilder builder, Line line)
        {
            foreach (IToken token in line.Tokens)
            {
                switch (token.TokenType)
                {
                    case TokenType.Text:
                        BuildRenderText(builder, (Text)token);
                        break;
                    case TokenType.StartTag:
                        BuildRenderStartTag(builder, (StartTag)token);
                        break;
                    case TokenType.EndTag:
                        BuildRenderEndTag(builder, (EndTag)token);
                        break;
                    case TokenType.QuotedString:
                        BuildRendeQuotedTag(builder, (QuotedString)token);
                        break;
                    case TokenType.CSLine:
                        BuildRenderCSLine(builder, (CSLine)token);
                        break;
                    case TokenType.CSBlockStart:
                        BuildRenderCSBlockStart(builder, (CSBlockStart)token);
                        break;
                    case TokenType.CSBlockEnd:
                        BuildRenderCSBlockEnd(builder, (CSBlockEnd)token);
                        break;
                    default:
                        break;
                }
            }
        }

        private void BuildRenderCSBlockEnd(RenderTreeBuilder builder, CSBlockEnd token)
        {
            builder.OpenElement(Next(), "span");
            builder.AddAttribute(Next(), "class", _themeRazorKeywordClass);
            builder.AddContent(Next(), '}');
            builder.CloseElement();
        }

        private void BuildRenderCSBlockStart(RenderTreeBuilder builder, CSBlockStart csBlockStart)
        {
            builder.OpenElement(Next(), "span");
            builder.AddAttribute(Next(), "class", _themeRazorKeywordClass);
            string functions = csBlockStart.IsFunctions ? csBlockStart.IsCode ? "code " : "functions " : "";
            builder.AddContent(Next(), "@" + functions);
            if (csBlockStart.IsOpenBrace)
            {
                builder.AddContent(Next(), '{');
            }

            builder.CloseElement();
        }

        private void BuildRendeQuotedTag(RenderTreeBuilder builder, QuotedString quotedTag)
        {
            string quote = GetQuoteChar(quotedTag.QuoteMark);
            if (quotedTag.LineType == LineType.SingleLine || quotedTag.LineType == LineType.MultiLineStart)
            {
                if (quotedTag.IsMultiLineStatement)
                {
                    builder.OpenElement(Next(), "span");
                    builder.AddAttribute(Next(), "class", _themeQuotedStringClass);
                    builder.AddContent(Next(), '@');
                    builder.CloseElement();
                }

                builder.OpenElement(Next(), "span");
                builder.AddAttribute(Next(), "class", _themeQuotedStringClass);
                builder.AddContent(Next(), quote);
                builder.CloseElement();


                if (quotedTag.IsCSStatement)
                {
                    builder.OpenElement(Next(), "span");
                    builder.AddAttribute(Next(), "class", _themeRazorKeywordClass);
                    builder.AddContent(Next(), '@');
                    if (quotedTag.HasParentheses)
                    {
                        builder.AddContent(Next(), "(");
                    }

                    builder.CloseElement();
                }
            }

            builder.OpenElement(Next(), "span");
            builder.AddAttribute(Next(), "class", _themeQuotedStringClass);
            builder.AddContent(Next(), quotedTag.Content);
            builder.CloseElement();

            if (quotedTag.LineType == LineType.SingleLine || quotedTag.LineType == LineType.MultiLineEnd)
            {
                if (quotedTag.HasParentheses)
                {
                    builder.OpenElement(Next(), "span");
                    builder.AddAttribute(Next(), "class", _themeRazorKeywordClass);
                    builder.AddContent(Next(), ')');
                    builder.CloseElement();
                }

                builder.OpenElement(Next(), "span");
                builder.AddAttribute(Next(), "class", _themeQuotedStringClass);
                builder.AddContent(Next(), quote);
                builder.CloseElement();
            }
        }

        private void BuildRenderCSLine(RenderTreeBuilder builder, CSLine csLine)
        {
            builder.OpenElement(Next(), "span");
            builder.AddAttribute(Next(), "class", _themeRazorKeywordClass);
            string lineType = GetLineType(csLine.LineType);
            builder.AddContent(Next(), "@" + lineType);
            builder.CloseElement();

            builder.OpenElement(Next(), "span");
            builder.AddAttribute(Next(), "class", _themeTextClass);
            builder.AddContent(Next(), csLine.Line);
            builder.CloseElement();
        }

        private void BuildRenderEndTag(RenderTreeBuilder builder, EndTag endTag)
        {
            builder.OpenElement(Next(), "span");
            builder.AddAttribute(Next(), "class", _themeTagSymbolsClass);
            builder.AddContent(Next(), "</");
            builder.CloseElement();

            builder.OpenElement(Next(), "span");
            builder.AddAttribute(Next(), "class", _themeTagNameClass);
            builder.AddContent(Next(), endTag.Name);
            builder.CloseElement();

            builder.OpenElement(Next(), "span");
            builder.AddAttribute(Next(), "class", _themeTagSymbolsClass);
            builder.AddContent(Next(), ">");
            builder.CloseElement();
        }

        private void BuildRenderStartTag(RenderTreeBuilder builder, StartTag startTag)
        {
            if (startTag.LineType == LineType.SingleLine || startTag.LineType == LineType.MultiLineStart)
            {
                builder.OpenElement(Next(), "span");
                builder.AddAttribute(Next(), "class", _themeTagSymbolsClass);
                builder.AddContent(Next(), "<");
                builder.CloseElement();

                builder.OpenElement(Next(), "span");
                builder.AddAttribute(Next(), "class", _themeTagNameClass);
                builder.AddContent(Next(), startTag.Name);
                builder.CloseElement();
            }

            BuildRenderAttributes(builder, startTag.Attributes);


            if (startTag.LineType == LineType.SingleLine || startTag.LineType == LineType.MultiLineEnd)
            {
                builder.OpenElement(Next(), "span");
                builder.AddAttribute(Next(), "class", _themeTagSymbolsClass);
                if (startTag.IsSelfClosingTag && !startTag.IsGeneric)
                {
                    string spacer =
                        (startTag.LineType == LineType.MultiLineEnd && startTag.Attributes.Count == 0)
                        ? string.Empty : " ";
                    builder.AddContent(Next(), spacer + "/");
                }
                builder.AddContent(Next(), ">");
                builder.CloseElement();
            }
        }

        private void BuildRenderText(RenderTreeBuilder builder, Text text)
        {
            builder.OpenElement(Next(), "span");
            builder.AddAttribute(Next(), "class", _themeTextClass);
            builder.AddContent(Next(), text.Content);
            builder.CloseElement();
        }

        private void BuildRenderAttributes(RenderTreeBuilder builder, List<IToken> attributes)
        {
            foreach (IToken token in attributes)
            {
                switch (token.TokenType)
                {
                    case TokenType.Text:
                        BuildRenderText(builder, (Text)token);
                        break;
                    case TokenType.Attribute:
                        BuildRenderAttribute(builder, (AttributeToken)token);
                        break;
                    default:
                        break;
                }
            }
        }

        private void BuildRenderAttribute(RenderTreeBuilder builder, AttributeToken attribute)
        {
            builder.OpenElement(Next(), "span");
            builder.AddAttribute(Next(), "class", _themeAttributeNameClass);
            builder.AddContent(Next(), " " + attribute.Name);
            builder.CloseElement();

            if (!attribute.NameOnly)
            {
                builder.OpenElement(Next(), "span");
                builder.AddAttribute(Next(), "class", _themeAttributeValueClass);
                builder.AddContent(Next(), "=");
                builder.CloseElement();

                BuildRendeQuotedTag(builder, attribute.Value);
            }
        }

        private string GetQuoteChar(QuoteMarkType quoteMark)
        {
            switch (quoteMark)
            {
                case QuoteMarkType.Unquoted:
                    return string.Empty;
                case QuoteMarkType.DoubleQuote:
                    return "\"";
                case QuoteMarkType.SingleQuote:
                    return "'";
                default:
                    return string.Empty;
            }
        }


        //Replace with Enum string value ToLowerFirstChar()
        private string GetLineType(CSLineType lineType)
        {
            switch (lineType)
            {
                case CSLineType.AddTagHelper:
                    return "addTagHelper";
                case CSLineType.Implements:
                    return "implements";
                case CSLineType.Inherit:
                    return "inherit";
                case CSLineType.Inject:
                    return "inject";
                case CSLineType.Layout:
                    return "layout";
                case CSLineType.Page:
                    return "page";
                case CSLineType.Using:
                    return "using";
                case CSLineType.Attribute:
                    return "attribute";
                case CSLineType.Namespace:
                    return "namespace";
                default:
                    return "";
            }

        }

    }
}
