using Blazorous;
using BlazorPrettyCode.Themes;
using CSHTMLTokenizer;
using CSHTMLTokenizer.Tokens;
using Microsoft.AspNetCore.Blazor;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.RenderTree;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BlazorPrettyCode
{
    public class PrettyCode : BlazorComponent
    {
        [Parameter] private string CodeId { get; set; }
        [Parameter] private bool Debug { get; set; }
        [Parameter] private string CodeFile { get; set; }

        private List<IToken> Tokens { get; set; } = new List<IToken>();
        private int i = 0;
        private ICodeTheme theme = new PrettyCodeDefault();
        private string preCss;
        private string tagSymbolsCss;
        private string tagNameCss;
        private string attributeNameCss;
        private string attributeValueCss;
        private string textCss;
        private string quotedStringCss;
        private string cshtmlKeywordCss;
        private bool isInitDone = false;

        [Inject]
        protected HttpClient HttpClient { get; set; }

        protected async override Task OnInitAsync()
        {
            preCss = await theme.Pre.ToCss();
            tagSymbolsCss = await theme.TagSymbols.ToCss();
            tagNameCss = await theme.TagName.ToCss();
            attributeNameCss = await theme.AttributeName.ToCss();
            attributeValueCss = await theme.AttributeValue.ToCss();
            textCss = await theme.Text.ToCss();
            quotedStringCss = await theme.QuotedString.ToCss();
            cshtmlKeywordCss = await theme.CSHtmlKeyword.ToCss();

            var str = await HttpClient.GetStringAsync(CodeFile);
            Tokens = Tokenizer.Parse(str);
            if (Debug) PrintToConsole();
            isInitDone = true;
        }

        private void PrintToConsole()
        {
            var sb = new StringBuilder();
            sb.Append("ToHtml: ");
            foreach (var token in Tokens)
            {
                sb.Append(token.ToHtml());
            }
            Console.WriteLine(sb.ToString());
        }

        private int Next() => i++;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            base.BuildRenderTree(builder);
            if (!isInitDone) return;
            builder.OpenComponent<Dynamic>(Next());
            builder.AddAttribute(Next(), "TagName", "pre");
            builder.AddAttribute(Next(), "css", preCss);
            builder.AddAttribute(Next(), "ChildContent", (RenderFragment)(builderMain => BuildRenderMain(builderMain)));
            builder.CloseComponent();
        }

        private void BuildRenderMain(RenderTreeBuilder builder)
        {
            foreach (var token in Tokens)
            {
                switch (token.TokenType)
                {
                    case TokenType.Text:
                        var text = (Text)token;
                        BuildRenderText(builder, text);
                        break;
                    case TokenType.StartTag:
                        var startTag = (StartTag)token;
                        BuildRenderStartTag(builder, startTag);
                        break;
                    case TokenType.EndTag:
                        var endTag = (EndTag)token;
                        BuildRenderEndTag(builder, endTag);
                        break;
                    default:
                        break;
                }
            }
        }

        private void BuildRenderText(RenderTreeBuilder builder, Text text, string css = null)
        {
            if (css == null) css = textCss;
            if (text.Tokens.Count == 0)
            {
                builder.OpenComponent<Dynamic>(Next());
                builder.AddAttribute(Next(), "TagName", "span");
                builder.AddAttribute(Next(), "css", css);
                builder.AddAttribute(Next(), "ChildContent", (RenderFragment)((builder2) =>
                {
                    builder2.AddContent(Next(), text.Content);
                }));
                builder.CloseComponent();
            }
            else
            {
                foreach(var token in text.Tokens)
                {
                    switch (token.TokenType)
                    {
                        case TokenType.Text:
                            var innerText = (Text)token;
                            BuildRenderText(builder, innerText);
                            break;
                        case TokenType.QuotedString:
                            var quotedTag = (QuotedString)token;
                            BuildRendeQuotedTag(builder, quotedTag);
                            break;
                        default:
                            break;
                    }

                }
            }
        }

        private void BuildRendeQuotedTag(RenderTreeBuilder builder, QuotedString quotedTag)
        {
            builder.OpenComponent<Dynamic>(Next());
            builder.AddAttribute(Next(), "TagName", "span");
            builder.AddAttribute(Next(), "css", quotedStringCss);
            builder.AddAttribute(Next(), "ChildContent", (RenderFragment)((builder2) =>
            {
                var quote = GetQuoteChar(quotedTag.QuoteMark);
                builder2.AddContent(Next(), quote + quotedTag.Content + quote);
            }));
            builder.CloseComponent();
        }

        private void BuildRenderStartTag(RenderTreeBuilder builder, StartTag startTag)
        {
            builder.OpenComponent<Dynamic>(Next());
            builder.AddAttribute(Next(), "TagName", "span");
            builder.AddAttribute(Next(), "css", tagSymbolsCss);
            builder.AddAttribute(Next(), "ChildContent", (RenderFragment)((builder2) =>
            {
                builder2.AddContent(Next(), "<");
            }));
            builder.CloseComponent();

            builder.OpenComponent<Dynamic>(Next());
            builder.AddAttribute(Next(), "TagName", "span");
            builder.AddAttribute(Next(), "css", tagNameCss);
            builder.AddAttribute(Next(), "ChildContent", (RenderFragment)((builder2) =>
            {
                builder2.AddContent(Next(), startTag.Name);
            }));
            builder.CloseComponent();

            BuildRenderAttributes(builder, startTag.Attributes);

            if (startTag.IsSelfClosingTag)
            {
                builder.OpenComponent<Dynamic>(Next());
                builder.AddAttribute(Next(), "TagName", "span");
                builder.AddAttribute(Next(), "css", tagSymbolsCss);
                builder.AddAttribute(Next(), "ChildContent", (RenderFragment)((builder2) =>
                {
                    builder2.AddContent(Next(), " /");
                }));
                builder.CloseComponent();
            }

            builder.OpenComponent<Dynamic>(Next());
            builder.AddAttribute(Next(), "TagName", "span");
            builder.AddAttribute(Next(), "css", tagSymbolsCss);
            builder.AddAttribute(Next(), "ChildContent", (RenderFragment)((builder2) =>
            {
                builder2.AddContent(Next(), ">");
            }));
            builder.CloseComponent();
        }

        private void BuildRenderAttributes(RenderTreeBuilder builder, List<IToken> attributes)
        {
            foreach (var token in attributes)
            {
                switch (token.TokenType)
                {
                    case TokenType.AttributeName:
                        var attributeName = (AttributeName)token;
                        BuildRenderAttributeName(builder, attributeName);
                        break;
                    case TokenType.AttributeValue:
                        var attributeValue = (AttributeValue)token;
                        BuildRenderAttributeValue(builder, attributeValue);
                        break;
                    default:
                        break;
                }
            }
        }

        private void BuildRenderAttributeValue(RenderTreeBuilder builder, AttributeValue attributeValue)
        {
            builder.OpenComponent<Dynamic>(Next());
            builder.AddAttribute(Next(), "TagName", "span");
            builder.AddAttribute(Next(), "css", attributeValueCss);
            builder.AddAttribute(Next(), "ChildContent", (RenderFragment)((builder2) =>
            {
                var quote = GetQuoteChar(attributeValue.QuoteMark);
                builder2.AddContent(Next(), "=" + quote);
            }));
            builder.CloseComponent();

            if (attributeValue.Tokens.Count == 0)
            {
                builder.OpenComponent<Dynamic>(Next());
                builder.AddAttribute(Next(), "TagName", "span");
                builder.AddAttribute(Next(), "css", attributeValueCss);
                builder.AddAttribute(Next(), "ChildContent", (RenderFragment)((builder2) =>
                {
                    builder2.AddContent(Next(), attributeValue.Value);
                }));
                builder.CloseComponent();
            }
            else
            {
                foreach(var token in attributeValue.Tokens)
                {
                    switch (token.TokenType)
                    {
                        case TokenType.Text:
                            var innerText = (Text)token;
                            BuildRenderText(builder, innerText, attributeValueCss);
                            break;
                        case TokenType.AttributeValueStatement:
                            var attributeValueStatement = (AttributeValueStatement)token;
                            BuildRendeAttributeValueStatement(builder, attributeValueStatement);
                            break;
                        default:
                            break;
                    }
                }
            }
            builder.OpenComponent<Dynamic>(Next());
            builder.AddAttribute(Next(), "TagName", "span");
            builder.AddAttribute(Next(), "css", attributeValueCss);
            builder.AddAttribute(Next(), "ChildContent", (RenderFragment)((builder2) =>
            {
                var quote = GetQuoteChar(attributeValue.QuoteMark);
                builder2.AddContent(Next(), quote);
            }));
            builder.CloseComponent();
        }

        private void BuildRendeAttributeValueStatement(RenderTreeBuilder builder, AttributeValueStatement attributeValueStatement)
        {
            builder.OpenComponent<Dynamic>(Next());
            builder.AddAttribute(Next(), "TagName", "span");
            builder.AddAttribute(Next(), "css", cshtmlKeywordCss);
            builder.AddAttribute(Next(), "ChildContent", (RenderFragment)((builder2) =>
            {
                var parens = attributeValueStatement.HasParentheses ? "(" : "";
                builder2.AddContent(Next(), "@" + parens);
                
            }));
            builder.CloseComponent();

            builder.OpenComponent<Dynamic>(Next());
            builder.AddAttribute(Next(), "TagName", "span");
            builder.AddAttribute(Next(), "css", attributeValueCss);
            builder.AddAttribute(Next(), "ChildContent", (RenderFragment)((builder2) =>
            {
                builder2.AddContent(Next(), attributeValueStatement.Content);

            }));
            builder.CloseComponent();

            if(attributeValueStatement.HasParentheses)
            {
                builder.OpenComponent<Dynamic>(Next());
                builder.AddAttribute(Next(), "TagName", "span");
                builder.AddAttribute(Next(), "css", cshtmlKeywordCss);
                builder.AddAttribute(Next(), "ChildContent", (RenderFragment)((builder2) =>
                {
                    builder2.AddContent(Next(), ")");

                }));
                builder.CloseComponent();
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

        private void BuildRenderAttributeName(RenderTreeBuilder builder, AttributeName attributeName)
        {
            builder.OpenComponent<Dynamic>(Next());
            builder.AddAttribute(Next(), "TagName", "span");
            builder.AddAttribute(Next(), "css", attributeNameCss);
            builder.AddAttribute(Next(), "ChildContent", (RenderFragment)((builder2) =>
            {
                builder2.AddContent(Next(), " " +attributeName.Name);
            }));
            builder.CloseComponent();
        }

        private void BuildRenderEndTag(RenderTreeBuilder builder, EndTag endTag)
        {
            builder.OpenComponent<Dynamic>(Next());
            builder.AddAttribute(Next(), "TagName", "span");
            builder.AddAttribute(Next(), "css", tagSymbolsCss);
            builder.AddAttribute(Next(), "ChildContent", (RenderFragment)((builder2) => {
                builder2.AddContent(Next(), "</");
            }));
            builder.CloseComponent();

            builder.OpenComponent<Dynamic>(Next());
            builder.AddAttribute(Next(), "TagName", "span");
            builder.AddAttribute(Next(), "css", tagNameCss);
            builder.AddAttribute(Next(), "ChildContent", (RenderFragment)((builder2) => {
                builder2.AddContent(Next(), endTag.Name);
            }));
            builder.CloseComponent();

            builder.OpenComponent<Dynamic>(Next());
            builder.AddAttribute(Next(), "TagName", "span");
            builder.AddAttribute(Next(), "css", tagSymbolsCss);
            builder.AddAttribute(Next(), "ChildContent", (RenderFragment)((builder2) => {
                builder2.AddContent(Next(), ">");
            }));
            builder.CloseComponent();
        }
    }
}
