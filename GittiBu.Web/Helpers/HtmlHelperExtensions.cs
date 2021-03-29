using GittiBu.Common.Extensions;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Text.RegularExpressions;

namespace GittiBu.Web.Helpers
{
    public static class HtmlHelperExtensions
    {
        public static string TitleFormat(this IHtmlHelper htmlHelper, string title)
        {
            if (!string.IsNullOrEmpty(title))
            {
                string tempTitle = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(title?.ToLower());
                tempTitle = tempTitle?.Replace("&", " ");
                return tempTitle;
            }
            return title;
        }
        public static string StripHTML(string input)
        {
            return Regex.Replace(input, "<.*?>", String.Empty);
        }
    }
}
