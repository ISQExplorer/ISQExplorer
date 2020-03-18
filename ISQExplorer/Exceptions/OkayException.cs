using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ISQExplorer.Exceptions
{
    public class OkayException : Exception
    {
        public OkayException(string reason) : base(reason) { }
        public OkayException(string reason, Exception innerException) : base(reason, innerException) { }
    }
}
