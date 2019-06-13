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
using System.Threading.Tasks;

namespace BlazorPrettyCode
{
    public class PrettyCode : ComponentBase
    {
        [Parameter] private bool? Debug { get; set; } = null;
        [Parameter] private string CodeFile { get; set; }
        [Parameter] private ITheme Theme { get; set; }
        [Parameter] private bool? ShowLineNumbers { get; set; } = null;

        private List<Line> Lines { get; set; } = new List<Line>();
        private bool _isInitDone = false;
        private int i = 0;

        //Theme css
        private string _themePreClass;
        private string _themeTagSymbolsClass;
        private string _themeTagNameClass;
        private string _themeAttributeNameClass;
        private string _themeAttributeValueClass;
        private string _themeQuotedStringClass;
        private string _themeRazorKeywordClass;

        //Non Theme css
        private string _basePreClass;
        private string _baseRowSpan;
        private string _baseLineNumbersCell;
        private string _baseCodeCell;

        private bool _showLineNumbers;

        [Inject] protected HttpClient HttpClient { get; set; }
        [Inject] protected IStyled Styled { get; set; }
        [Inject] protected DefaultSettings DefaultConfig { get; set; }

        protected override async Task OnInitAsync()
        {
            bool debug = Debug ?? DefaultConfig.IsDevelopmentMode;
            string str = await HttpClient.GetStringAsync(CodeFile);
            Lines = Tokenizer.Parse(str);
            if (debug)
            {
                PrintToConsole();
            }

            _basePreClass = await Styled.Css(@"
                display: table;
                table-layout: fixed;
                width: 100%; /* anything but auto, otherwise fixed layout not guaranteed */
                white-space: pre-wrap;
                &:before {
                    counter-reset: linenum;
                }
            ");

            _baseRowSpan = await Styled.Css(@"
                display: table-row;
                counter-increment: linenum;
            ");

            _baseLineNumbersCell = await Styled.Css(@"
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

            _baseCodeCell = await Styled.Css(@"
                display: table-cell;
                padding-left: 1em;
            ");

            ITheme theme = Theme ?? DefaultConfig.DefaultTheme;
            _showLineNumbers = ShowLineNumbers ?? DefaultConfig.ShowLineNumbers;

            /*foreach (string font in theme.Fonts)
            {
                await Styled.Fontface(font);
            }*/

            _themePreClass = await Styled.Css(getThemeValues(theme));
            _themeTagSymbolsClass = await Styled.Css(getThemeValues(theme, "Tag start/end"));
            _themeTagNameClass = await Styled.Css(getThemeValues(theme, "Tag name"));
            _themeAttributeNameClass = await Styled.Css(getThemeValues(theme, "Attribute name"));
            _themeAttributeValueClass = await Styled.Css(getThemeValues(theme, "Attribute value"));
            _themeQuotedStringClass = await Styled.Css(getThemeValues(theme, "String"));
            _themeRazorKeywordClass = await Styled.Css(getThemeValues(theme, "Razor Keyword"));

            _isInitDone = true;
        }

        private string getThemeValues(ITheme theme, string setting = null)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            ISetting settings = (from s in theme.Settings
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
            sb.Append("ToHtml: ");
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
            base.BuildRenderTree(builder);
            if (!_isInitDone)
            {
                return;
            }

            builder.OpenElement(Next(), "pre");
            builder.AddAttribute(Next(), "class", _basePreClass + " " + _themePreClass);
            foreach (Line line in Lines)
            {
                builder.AddContent(Next(), builderLine => BuildRenderLine(builderLine, line));
            }
            builder.CloseElement();
        }

        private void BuildRenderLine(RenderTreeBuilder builderLine, Line line)
        {
            builderLine.OpenElement(Next(), "span");
            builderLine.AddAttribute(Next(), "class", _baseRowSpan);
            if (_showLineNumbers)
            {
                builderLine.OpenElement(Next(), "span");
                builderLine.AddAttribute(Next(), "class", _baseLineNumbersCell);
                builderLine.CloseElement();
            }
            builderLine.OpenElement(Next(), "code");
            builderLine.AddAttribute(Next(), "class", _baseCodeCell);
            builderLine.AddContent(Next(), builderMain => BuildRenderMain(builderMain, line));
            if (!line.LastLine)
            {
                builderLine.AddContent(Next(), System.Environment.NewLine);
            }

            builderLine.CloseElement();
            builderLine.CloseElement();
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
            string functions = csBlockStart.IsFunctions ? "functions " : "";
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

            builder.OpenElement(Next(), "span");
            builder.AddAttribute(Next(), "class", _themeQuotedStringClass);
            builder.AddContent(Next(), quotedTag.Content);
            builder.CloseElement();

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

        private void BuildRenderCSLine(RenderTreeBuilder builder, CSLine csLine)
        {
            builder.OpenElement(Next(), "span");
            builder.AddAttribute(Next(), "class", _themeRazorKeywordClass);
            string lineType = GetLineType(csLine.LineType);
            builder.AddContent(Next(), "@" + lineType);
            builder.CloseElement();

            builder.OpenElement(Next(), "span");
            //builder.AddAttribute(Next(), "class", _textClass);
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
            if (startTag.LineType == TagLineType.SingleLine || startTag.LineType == TagLineType.MultiLineStart)
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


            if (startTag.LineType == TagLineType.SingleLine || startTag.LineType == TagLineType.MultiLineEnd)
            {
                builder.OpenElement(Next(), "span");
                builder.AddAttribute(Next(), "class", _themeTagSymbolsClass);
                if (startTag.IsSelfClosingTag && !startTag.IsGeneric)
                {
                    string spacer =
                        (startTag.LineType == TagLineType.MultiLineEnd && startTag.Attributes.Count == 0)
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
            //builder.AddAttribute(Next(), "class", _textClass);
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
                default:
                    return "";
            }

        }

    }
}
