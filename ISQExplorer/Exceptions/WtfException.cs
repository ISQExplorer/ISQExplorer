using System;

namespace ISQExplorer.Exceptions
{
    public class WtfException : Exception
    {
        public WtfException(string reason) : base(reason)
        {
        }

        public WtfException(string reason, Exception innerException) : base(reason, innerException)
        {
        }
    }
}