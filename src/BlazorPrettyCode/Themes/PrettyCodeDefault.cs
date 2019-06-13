using System.Collections.Generic;

namespace BlazorPrettyCode.Themes
{
    public class PrettyCodeDefault : ITheme
    {
        public string Name => "Pretty Code - Default";
        public List<ISetting> Settings => new List<ISetting>
        {
            new Setting
            {
                Settings = new Dictionary<string, string>
                {
                    { "background-color", "rgba(238,238,238,0.92)" },
                    { "color", "#000" }
                }
            },
            new Setting
            {
                Name = "Tag start/end",
                Settings = new Dictionary<string, string>
                {
                    { "color", "#03c" }
                }
            },
            new Setting
            {
                Name = "Tag name",
                Settings = new Dictionary<string, string>
                {
                    { "color", "#03c" }
                }
            },
            new Setting
            {
                Name = "Attribute name",
                Settings = new Dictionary<string, string>
                {
                    { "color", "#36c" },
                    { "font-style", "italic" }
                }
            },
            new Setting
            {
                Name = "Attribute value",
                Settings = new Dictionary<string, string>
                {
                    { "color", "#093" }
                }
            },
            new Setting
            {
                Name = "String",
                Settings = new Dictionary<string, string>
                {
                    { "color", "#093" }
                }
            },
            new Setting
            {
                Name = "Razor Keyword",
                Settings = new Dictionary<string, string>
                {
                    { "background-color", "yellow" },
                    { "color", "black" }
                }
            }
        };

        /*public string Pre => "background-color: rgba(238,238,238,0.92);font-family: Fira Code;";
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
        public List<string> Fonts => new List<string>
        {
            @"font-family: Fira Code;
              font-style: normal;
              font-weight: 400;
              src: url('https://cdn.jsdelivr.net/gh/tonsky/FiraCode@1.206/distr/eot/FiraCode-Regular.eot');
              src: url('https://cdn.jsdelivr.net/gh/tonsky/FiraCode@1.206/distr/eot/FiraCode-Regular.eot') format('embedded-opentype'), 
                   url('https://cdn.jsdelivr.net/gh/tonsky/FiraCode@1.206/distr/woff2/FiraCode-Regular.woff2') format('woff2'), 
                   url('https://cdn.jsdelivr.net/gh/tonsky/FiraCode@1.206/distr/woff/FiraCode-Regular.woff') format('woff'), 
                   url('https://cdn.jsdelivr.net/gh/tonsky/FiraCode@1.206/distr/ttf/FiraCode-Regular.ttf') format('truetype');
              unicode-range: U+0000-00FF, U+0131, U+0152-0153, U+02BB-02BC, U+02C6, U+02DA, U+02DC, U+2000-206F, U+2074, U+20AC, U+2122, U+2191, U+2193, U+2212, U+2215, U+FEFF, U+FFFD;",
            @"font-family: Fira Code;
              font-style: normal;
              font-weight: 700;
              src: url('https://cdn.jsdelivr.net/gh/tonsky/FiraCode@1.206/distr/eot/FiraCode-Bold.eot');
              src: url('https://cdn.jsdelivr.net/gh/tonsky/FiraCode@1.206/distr/eot/FiraCode-Bold.eot') format('embedded-opentype'), 
              url('https://cdn.jsdelivr.net/gh/tonsky/FiraCode@1.206/distr/woff2/FiraCode-Bold.woff2') format('woff2'), 
              url('https://cdn.jsdelivr.net/gh/tonsky/FiraCode@1.206/distr/woff/FiraCode-Bold.woff') format('woff'), 
              url('https://cdn.jsdelivr.net/gh/tonsky/FiraCode@1.206/distr/ttf/FiraCode-Bold.ttf') format('truetype');
              unicode-range: U+0000-00FF, U+0131, U+0152-0153, U+02BB-02BC, U+02C6, U+02DA, U+02DC, U+2000-206F, U+2074, U+20AC, U+2122, U+2191, U+2193, U+2212, U+2215, U+FEFF, U+FFFD;"
        };

        public string Code => "font-family: Fira Code;";*/


    }
}