using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace ISQExplorer.Misc
{
    public static class Html
    {
        public static string ToEndpoint(string input)
        {
            return "/" + Regex.Replace(input.Replace(".", "/"), "Controller", "", RegexOptions.IgnoreCase);
        }

        public static string QueryString(IEnumerable<(object Key, object Value)> parameters)
        {
            var list = parameters.ToList();
            if (list.None())
            {
                return "";
            }

            return "?" + list
                       .Select(x =>
                           $"{HttpUtility.UrlEncode(x.Key.ToString())}={HttpUtility.UrlEncode(x.Value.ToString())}")
                       .Join("&");
        }
    }
}