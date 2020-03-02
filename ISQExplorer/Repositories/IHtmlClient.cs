using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ISQExplorer.Functional;

namespace ISQExplorer.Repositories
{
    public interface IHtmlClient
    {
        Task<Try<string, IOException>> GetAsync(Either<Uri, string> url);
        Task<Try<string, IOException>> PostAsync(Either<Uri, string> url, string postData);
        Task<Try<string, IOException>> PostAsync(Either<Uri, string> url, IDictionary<string, string?> postParams);
    }
}