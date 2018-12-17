using Blazorous;
using Blazorous.Components;
using BlazorPrettyCode.Themes;
using CSHTMLTokenizer;
using CSHTMLTokenizer.Tokens;
using Microsoft.AspNetCore.Blazor;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.RenderTree;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static CSHTMLTokenizer.Tokens.AttributeValue;

namespace BlazorPrettyCode
{
    public class PrettyCode : BlazorComponent
    {
        [Parameter] private string CodeId { get; set; }
        [Parameter] private bool Debug { get; set; }
        [Parameter] private bool ReplaceLTForComponents { get; set; } = true;
        private List<IToken> Tokens { get; set; } = new List<IToken>();
        private bool rendered = false;
        private int i = 0;
        private ICodeTheme theme = new PrettyCodeDefault();
        private string preCss;
        private string tagSymbolsCss;
        private string tagNameCss;
        private string attributeNameCss;
        private string attributeValueCss;
        private string textCss;
        private bool isInitDone = false;

        protected async override Task OnInitAsync()
        {
            //Setup theme
            preCss = await theme.Pre.ToCss();
            tagSymbolsCss = await theme.TagSymbols.ToCss();
            tagNameCss = await theme.TagName.ToCss();
            attributeNameCss = await theme.AttributeName.ToCss();
            attributeValueCss = await theme.AttributeValue.ToCss();
            textCss = await theme.Text.ToCss();
            isInitDone = true;
        }

        protected async override Task OnAfterRenderAsync()
        {
            if (rendered) return;
            rendered = true;
            var str = await JSInterop.GetAndHide(CodeId);
            if (ReplaceLTForComponents) str = str.Replace("&lt;", "<").Replace("&gt;", ">");
            if (Debug) Console.WriteLine("Input: " + str);
            Tokens = Tokenizer.Parse(str);
            if (Debug) PrintToConsole();
            StateHasChanged();
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
                        if(text.Content.Length != 0) BuildRenderText(builder, text);
                        break;
                    case TokenType.StartTag:
                        var startTag = (StartTag)token;
                        if (startTag.Name.Length != 0) BuildRenderStartTag(builder, startTag);
                        break;
                    case TokenType.EndTag:
                        var endTag = (EndTag)token;
                        if (endTag.Name.Length != 0) BuildRenderEndTag(builder, endTag);
                        break;
                    default:
                        break;
                }
            }
        }

        private void BuildRenderText(RenderTreeBuilder builder, Text text)
        {
            builder.OpenComponent<Dynamic>(Next());
            builder.AddAttribute(Next(), "TagName", "span");
            builder.AddAttribute(Next(), "css", textCss);
            builder.AddAttribute(Next(), "ChildContent", (RenderFragment)((builder2) => {
                builder2.AddContent(Next(), text.Content);
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
                        if (attributeName.Name.Length != 0) buildRenderAttributeName(builder, attributeName);
                        break;
                    case TokenType.AttributeValue:
                        var attributeValue = (AttributeValue)token;
                        if (attributeValue.Value.Length != 0) buildRenderAttributeValue(builder, attributeValue);
                        break;
                    default:
                        break;
                }
            }
        }

        private void buildRenderAttributeValue(RenderTreeBuilder builder, AttributeValue attributeValue)
        {
            builder.OpenComponent<Dynamic>(Next());
            builder.AddAttribute(Next(), "TagName", "span");
            builder.AddAttribute(Next(), "css", attributeValueCss);
            builder.AddAttribute(Next(), "ChildContent", (RenderFragment)((builder2) =>
            {
                var quote = GetQuoteChar(attributeValue);
                builder2.AddContent(Next(), "=" + quote + attributeValue.Value + quote);
            }));
            builder.CloseComponent();
        }

        private string GetQuoteChar(AttributeValue attributeValue)
        {
            switch (attributeValue.QuoteMark)
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

        private void buildRenderAttributeName(RenderTreeBuilder builder, AttributeName attributeName)
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
