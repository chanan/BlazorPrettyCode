using BlazorPrettyCode.Internal;
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
    public class PrettyCode : ComponentBase, IObserver<IDefaultSettings>, IDisposable
    {
        [Parameter] public bool? Debug { get; set; } = null;
        [Parameter] public string CodeFile { get; set; }
        [Parameter] public string CodeFileLineNumbers { get; set; }
        [Parameter] public string CodeSectionFile { get; set; }
        [Parameter] public string CodeSectionFileLineNumbers { get; set; }
        [Parameter] public string Theme { get; set; }
        [Parameter] public bool? ShowLineNumbers { get; set; } = null;
        [Parameter] public string HighlightLines { get; set; }
        [Parameter] public bool? ShowException { get; set; } = null;
        [Parameter] public bool? ShowCollapse { get; set; } = null;
        [Parameter] public string Title { get; set; } = null;
        [Parameter] public bool? IsCollapsed { get; set; } = null;
        [Parameter] public bool? AttemptToFixTabs { get; set; } = null;
        [Parameter] public bool? KeepOriginalLineNumbers { get; set; } = false;

        private List<Line> Lines { get; set; } = new List<Line>();
        private int i = 0;
        private List<int> _highlightLines = new List<int>();
        private int _lineNum = 1;
        private List<int> _lineNumbers;
        private List<int> _codeLineNumbers;
        private bool _addedCodeString;

        //Config variables
        private bool _showLineNumbers;
        private bool _showCollapse;
        private bool _isCollapsed;
        private bool _attemptToFixTabs;
        private bool _KeepOriginalLineNumbers;

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
        private string _themeCssProperty;
        private string _themeCssClass;

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
        [Inject] private IDefaultSettings DefaultSettings { get; set; }
        [Inject] private ThemeCache ThemeCache { get; set; }

        private IStyled _styled;
        private IDisposable _unsubscriber;
        private bool _shouldRender;

        protected override async Task OnInitializedAsync()
        {
            _styled = Styled.WithId("pretty-code");
            _unsubscriber = DefaultSettings.Subscribe(this);
            bool debug = Debug ?? DefaultSettings.IsDevelopmentMode;
            bool showException = ShowException ?? DefaultSettings.ShowException;
            InitSettings();

            await InitSourceFile(showException);

            if (debug)
            {
                PrintToConsole();
            }

            InitCSS();
            await InitThemeCss();
            _shouldRender = true;
        }

        private void InitSettings()
        {
            _showCollapse = ShowCollapse ?? DefaultSettings.ShowCollapse;
            _isCollapsed = IsCollapsed ?? DefaultSettings.IsCollapsed;
            _attemptToFixTabs = AttemptToFixTabs ?? DefaultSettings.AttemptToFixTabs;
            _KeepOriginalLineNumbers = KeepOriginalLineNumbers ?? DefaultSettings.KeepOriginalLineNumbers;
        }

        public void OnCompleted()
        {
            _unsubscriber.Dispose();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(IDefaultSettings value)
        {
            DefaultSettings = value;
            InitSettings();
            InvokeAsync(() => InitThemeCss().ContinueWith((obj) => StateHasChanged()));
            _shouldRender = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _unsubscriber.Dispose();
            }
        }

        protected override bool ShouldRender()
        {
            return _shouldRender;
        }

        private async Task InitSourceFile(bool showException)
        {
            string CodeFileString = await GetFromCacheOrNetwork(CodeFile);
            _lineNumbers = GetLineNumbers(CodeFileLineNumbers);
            string codeFileLinesString = GetLines(CodeFileString, _lineNumbers);
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
                _codeLineNumbers = GetLineNumbers(CodeSectionFileLineNumbers);
                codeSectionFileLinesString = GetLines(codeSectionString, _codeLineNumbers);

                if (!codeSectionFileLinesString.StartsWith("@code"))
                {
                    codeSectionFileLinesString = $"@code {{\n{codeSectionFileLinesString}}}";
                    _addedCodeString = true;
                    List<int> temp = new List<int> { _codeLineNumbers.First() - 1 };
                    temp.AddRange(_codeLineNumbers);
                    _codeLineNumbers = temp;
                    _codeLineNumbers.Add(_codeLineNumbers.Last() + 1);
                }
            }

            string str = string.IsNullOrWhiteSpace(codeSectionFileLinesString) ? codeFileLinesString : codeFileLinesString + '\n' + codeSectionFileLinesString;

            Parse(showException, str.TrimEnd());

            if (_attemptToFixTabs)
            {
                FixTabs();
            }
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
                            List<int> lines = GetLineNumbers(te.LineNumber.ToString());
                            string context = GetLines(str, lines);
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
            string themeName = Theme ?? DefaultSettings.DefaultTheme;
            string uri = themeName.ToLower().StartsWith("http") ? themeName : "_content/BlazorPrettyCode/" + themeName + ".json";

            string strJson = await GetFromCacheOrNetwork(uri);

            Themes.Theme theme = JsonSerializer.Deserialize<Themes.Theme>(strJson);
            _showLineNumbers = ShowLineNumbers ?? DefaultSettings.ShowLineNumbers;

            _styled.AddGoogleFonts(GetFonts(theme));

            StringBuilder sb = new StringBuilder();
            sb.Append("overflow-y: auto;");
            sb.Append("margin-bottom: 1em;");
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
            _themeCssProperty = _styled.Css(GetThemeValues(theme, "CSS Propery"));
            _themeCssClass = _styled.Css(GetThemeValues(theme, "CSS Class"));

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

        private Task<string> GetFromCacheOrNetwork(string uri)
        {
            return ThemeCache.GetOrAdd(uri, new Func<Task<string>>(() => HttpClient.GetStringAsync(uri)));
        }

        private void InitCSS()
        {
            int num = _KeepOriginalLineNumbers ? _lineNumbers.First() - 1 : 0;
            _basePreClass = _styled.Css($@"
                label: pre;
                display: table;
                table-layout: fixed;
                width: 100%;
                -webkit-border-radius: 5px;
                line-height: 1.5em;
                counter-reset: linenum {num};
                @media only screen and (min-width: 320px) and (max-width: 480px) {{
                    font-size: 50%;
                    line-height: 1em;
                    margin-bottom: 0.25em;
                }}
                @media (min-width: 481px) and (max-width: 1223px) {{
                    font-size: 80%;
                    line-height: 1.25em;
                    margin-bottom: 0.5em;
                }}
            ");

            //Code Row

            _baseRowSpan = _styled.Css(@"
                label: row;
                display: table-row;
                counter-increment: linenum;
            ");

            _baseLineNumbersCell = _styled.Css(@"
                label: number-cell;
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
                label: code-cell;
                display: table-cell;
                padding-left: 1em;
                line-height: 1.5em;
                @media only screen and (min-width: 320px) and (max-width: 480px) {
                    font-size: 50%;
                    line-height: 1em;
                }
                @media (min-width: 481px) and (max-width: 1223px) {
                    font-size: 80%;
                    line-height: 1.25em;
                }
            ");

            //Title Row

            _baseRowSpanTitle = _styled.Css(@"
                label: title-row;
                display: table-row;
            ");

            _baseCellSpacer = _styled.Css(@"
                label: cell-spacer;
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
                label: cell-title;
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

        private string GetLines(string str, List<int> lineNumbers)
        {
            if (lineNumbers.Count > 0)
            {
                string[] arr = Regex.Split(str, "\r\n|\r|\n");
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < arr.Length; i++)
                {
                    if (lineNumbers.Contains(i + 1))
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
            if (setting != null)
            {
                sb.Append("label:").Append(GetLabel(setting)).Append(';');
            }
            foreach (KeyValuePair<string, string> kvp in dictionary)
            {
                sb.Append(kvp.Key).Append(':').Append(kvp.Value).Append(';');
            }
            return sb.ToString();
        }

        private string GetLabel(string setting)
        {
            return setting.ToLower().Replace(' ', '-');
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
            _shouldRender = false;
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
            string lineNum = GetCounterReset();
            builderLine.OpenElement(Next(), "span");
            builderLine.AddAttribute(Next(), "class", _baseRowSpan + lineNum);
            if (_showLineNumbers)
            {
                builderLine.OpenElement(Next(), "span");
                builderLine.AddAttribute(Next(), "class", _baseLineNumbersCell + highlightClass);
                builderLine.CloseElement();
            }
            builderLine.OpenElement(Next(), "code");
            builderLine.AddAttribute(Next(), "class", _baseCodeCell + highlightClassBorder);
            builderLine.AddContent(Next(), builderMain => BuildRenderMain(builderMain, line.Tokens));
            if (!line.LastLine)
            {
                builderLine.AddContent(Next(), System.Environment.NewLine);
            }

            builderLine.CloseElement();
            builderLine.CloseElement();
            _lineNum++;
        }

        private string GetCounterReset()
        {
            try
            {
                if (!_KeepOriginalLineNumbers)
                {
                    return string.Empty;
                }

                if (_lineNumbers.Count >= _lineNum)
                {
                    return " " + Styled.Css($"counter-reset: linenum {_lineNumbers[_lineNum - 1] - 1};");
                }
                if (_addedCodeString && _lineNum == _lineNumbers.Count + 1)
                {
                    return string.Empty;
                }
                if (_lineNumbers.Count + _codeLineNumbers.Count >= _lineNum)
                {
                    int baseCount = _addedCodeString ? _lineNumbers.Count + 1 : _lineNumbers.Count;
                    int offset = _addedCodeString ? 1 : 0;
                    return " " + Styled.Css($"counter-reset: linenum {_codeLineNumbers[_lineNum - baseCount - 1] - offset};");
                }
            }
            catch (Exception)
            {
                //ignored
            }
            return string.Empty;
        }

        private void BuildRenderMain(RenderTreeBuilder builder, List<IToken> tokens)
        {
            foreach (IToken token in tokens)
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
                    case TokenType.CSSProperty:
                        BuildRenderCSSProperty(builder, (CSSProperty)token);
                        break;
                    case TokenType.CSSValue:
                        BuildRenderCSSValue(builder, (CSSValue)token);
                        break;
                    case TokenType.CSSOpenClass:
                        BuildRenderCSSOpenClass(builder, (CSSOpenClass)token);
                        break;
                    case TokenType.CSSCloseClass:
                        BuildRenderCSSCloseClass(builder, (CSSCloseClass)token);
                        break;
                    default:
                        break;
                }
            }
        }

        private void BuildRenderCSSCloseClass(RenderTreeBuilder builder, CSSCloseClass token)
        {
            builder.OpenElement(Next(), "span");
            builder.AddAttribute(Next(), "class", _themeCssClass);
            builder.AddContent(Next(), "}");
            builder.CloseElement();
        }

        private void BuildRenderCSSOpenClass(RenderTreeBuilder builder, CSSOpenClass token)
        {
            builder.OpenElement(Next(), "span");
            builder.AddAttribute(Next(), "class", _themeCssClass);
            builder.AddContent(Next(), token.Content + " {");
            builder.CloseElement();
        }

        private void BuildRenderCSSValue(RenderTreeBuilder builder, CSSValue token)
        {
            builder.OpenElement(Next(), "span");
            builder.AddContent(Next(), builderMain => BuildRenderMain(builderMain, token.Tokens));
            builder.CloseElement();
        }

        private void BuildRenderCSSProperty(RenderTreeBuilder builder, CSSProperty token)
        {
            builder.OpenElement(Next(), "span");
            builder.AddAttribute(Next(), "class", _themeCssProperty);
            builder.AddContent(Next(), token.Content + ":");
            builder.CloseElement();
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
