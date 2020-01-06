using System;

namespace ISQExplorer.Exceptions
{
    public class MalformedPageException : Exception
    {
        public MalformedPageException(string message) : base(message)
        {
        }

        public MalformedPageException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}