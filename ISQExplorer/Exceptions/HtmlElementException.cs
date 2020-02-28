using System;
using System.Reflection;
using AngleSharp.Dom;

namespace ISQExplorer.Exceptions
{
    public class HtmlElementException : Exception
    {
        public readonly IElement Element;

        public HtmlElementException(IElement element, MemberInfo expectedType) : base(
            $"Expected an element of type {expectedType.Name}. Got one with OuterHTML {element.OuterHtml}")
        {
            Element = element;
        }
        
        public HtmlElementException(IElement element, string reason) : base(reason)
        {
            Element = element;
        }

        public HtmlElementException(IElement element, string reason, Exception innerException) : base(reason,
            innerException)
        {
            Element = element;
        }
    }
}