using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ISQExplorer.Functional;
using ISQExplorer.Web;

namespace ISQExplorer.Repositories
{
    public interface IHtmlClient
    {
        Task<Try<HtmlPage, IOException>> GetAsync(Either<Uri, string> url);
        Task<Try<HtmlPage, IOException>> PostAsync(Either<Uri, string> url, string postData);
        Task<Try<HtmlPage, IOException>> PostAsync(Either<Uri, string> url, IReadOnlyDictionary<string, string?> postParams);
    }
}