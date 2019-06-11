using BlazorPrettyCode.Themes;
using BlazorStyled;
using CSHTMLTokenizer;
using CSHTMLTokenizer.Tokens;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BlazorPrettyCode
{
    public class PrettyCode : ComponentBase
    {
        [Parameter] private bool Debug { get; set; }
        [Parameter] private string CodeFile { get; set; }

        private List<IToken> Tokens { get; set; } = new List<IToken>();
        private bool _isInitDone = false;
        private int i = 0;

        private readonly ICodeTheme _theme = new PrettyCodeDefault();
        private string _preClass;
        private string _tagSymbolsClass;
        private string _tagNameClass;
        private string _attributeNameClass;
        private string _attributeValueClass;
        private string _textClass;
        private string _quotedStringClass;
        private string _cshtmlKeywordClass;

        [Inject]
        protected HttpClient HttpClient { get; set; }

        [Inject]
        protected IStyled Styled { get; set; }

        protected override async Task OnInitAsync()
        {
            string str = await HttpClient.GetStringAsync(CodeFile);
            Tokens = Tokenizer.Parse(str);
            if (Debug)
            {
                PrintToConsole();
            }

            _preClass = await Styled.Css(_theme.Pre);
            _tagSymbolsClass = await Styled.Css(_theme.TagSymbols);
            _tagNameClass = await Styled.Css(_theme.TagName);
            _attributeNameClass = await Styled.Css(_theme.AttributeName);
            _attributeValueClass = await Styled.Css(_theme.AttributeValue);
            _textClass = await Styled.Css(_theme.Text);
            _quotedStringClass = await Styled.Css(_theme.QuotedString);
            _cshtmlKeywordClass = await Styled.Css(_theme.CSHtmlKeyword);

            _isInitDone = true;
        }

        private void PrintToConsole()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("ToHtml: ");
            foreach (IToken token in Tokens)
            {
                sb.Append(token.ToHtml());
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
            builder.AddAttribute(Next(), "class", _preClass);
            builder.OpenElement(Next(), "code");
            builder.AddContent(Next(), builderMain => BuildRenderMain(builderMain));
            builder.CloseElement();
            builder.CloseElement();
        }

        private void BuildRenderMain(RenderTreeBuilder builder)
        {
            foreach (IToken token in Tokens)
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
            builder.AddAttribute(Next(), "class", _cshtmlKeywordClass);
            builder.AddContent(Next(), '}');
            builder.CloseElement();
        }

        private void BuildRenderCSBlockStart(RenderTreeBuilder builder, CSBlockStart csBlockStart)
        {
            builder.OpenElement(Next(), "span");
            builder.AddAttribute(Next(), "class", _cshtmlKeywordClass);
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
            builder.AddAttribute(Next(), "class", _quotedStringClass);
            builder.AddContent(Next(), quote);
            builder.CloseElement();

            if (quotedTag.IsCSStatement)
            {
                builder.OpenElement(Next(), "span");
                builder.AddAttribute(Next(), "class", _cshtmlKeywordClass);
                builder.AddContent(Next(), '@');
                if (quotedTag.HasParentheses)
                {
                    builder.AddContent(Next(), "(");
                }

                builder.CloseElement();
            }

            builder.OpenElement(Next(), "span");
            builder.AddAttribute(Next(), "class", _quotedStringClass);
            builder.AddContent(Next(), quotedTag.Content);
            builder.CloseElement();

            if (quotedTag.HasParentheses)
            {
                builder.OpenElement(Next(), "span");
                builder.AddAttribute(Next(), "class", _cshtmlKeywordClass);
                builder.AddContent(Next(), ')');
                builder.CloseElement();
            }

            builder.OpenElement(Next(), "span");
            builder.AddAttribute(Next(), "class", _quotedStringClass);
            builder.AddContent(Next(), quote);
            builder.CloseElement();
        }

        private void BuildRenderCSLine(RenderTreeBuilder builder, CSLine csLine)
        {
            builder.OpenElement(Next(), "span");
            builder.AddAttribute(Next(), "class", _cshtmlKeywordClass);
            string lineType = GetLineType(csLine.LineType);
            builder.AddContent(Next(), "@" + lineType);
            builder.CloseElement();

            builder.OpenElement(Next(), "span");
            builder.AddAttribute(Next(), "class", _textClass);
            builder.AddContent(Next(), csLine.Line);
            builder.CloseElement();
        }

        private void BuildRenderEndTag(RenderTreeBuilder builder, EndTag endTag)
        {
            builder.OpenElement(Next(), "span");
            builder.AddAttribute(Next(), "class", _tagSymbolsClass);
            builder.AddContent(Next(), "</");
            builder.CloseElement();

            builder.OpenElement(Next(), "span");
            builder.AddAttribute(Next(), "class", _tagNameClass);
            builder.AddContent(Next(), endTag.Name);
            builder.CloseElement();

            builder.OpenElement(Next(), "span");
            builder.AddAttribute(Next(), "class", _tagSymbolsClass);
            builder.AddContent(Next(), ">");
            builder.CloseElement();
        }

        private void BuildRenderStartTag(RenderTreeBuilder builder, StartTag startTag)
        {
            builder.OpenElement(Next(), "span");
            builder.AddAttribute(Next(), "class", _tagSymbolsClass);
            builder.AddContent(Next(), "<");
            builder.CloseElement();

            builder.OpenElement(Next(), "span");
            builder.AddAttribute(Next(), "class", _tagNameClass);
            builder.AddContent(Next(), startTag.Name);
            builder.CloseElement();

            BuildRenderAttributes(builder, startTag.Attributes);

            builder.OpenElement(Next(), "span");
            builder.AddAttribute(Next(), "class", _tagSymbolsClass);
            if (startTag.IsSelfClosingTag && !startTag.IsGeneric)
            {
                builder.AddContent(Next(), " /");
            }
            builder.AddContent(Next(), ">");
            builder.CloseElement();
        }

        private void BuildRenderText(RenderTreeBuilder builder, Text text)
        {
            builder.OpenElement(Next(), "span");
            builder.AddAttribute(Next(), "class", _textClass);
            builder.AddContent(Next(), text.Content);
            builder.CloseElement();
        }

        private void BuildRenderAttributes(RenderTreeBuilder builder, List<IToken> attributes)
        {
            foreach (IToken token in attributes)
            {
                BuildRenderAttribute(builder, (AttributeToken)token);
            }
        }

        private void BuildRenderAttribute(RenderTreeBuilder builder, AttributeToken attribute)
        {
            builder.OpenElement(Next(), "span");
            builder.AddAttribute(Next(), "class", _attributeNameClass);
            builder.AddContent(Next(), " " + attribute.Name);
            builder.CloseElement();

            if (!attribute.NameOnly)
            {
                builder.OpenElement(Next(), "span");
                builder.AddAttribute(Next(), "class", _attributeValueClass);
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
