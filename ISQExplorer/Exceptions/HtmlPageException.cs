using System;
using ISQExplorer.Web;

namespace ISQExplorer.Exceptions
{
    public class HtmlPageException : Exception
    {
        public readonly HtmlPage Document;


        public HtmlPageException(HtmlPage element, string reason) : base(reason)
        {
            Document = element;
        }

        public HtmlPageException(HtmlPage element, string reason, Exception innerException) : base(reason,
            innerException)
        {
            Document = element;
        }
    }
}