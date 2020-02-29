using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using ISQExplorer.Functional;
using ISQExplorer.Misc;

namespace ISQExplorer.Web
{
    public class HtmlTable
    {
        private IList<IHtmlTableRowElement> _rows;
        public IReadOnlyList<string> ColumnTitles;
        private IDictionary<string, int> _columnTitleToIndex;

        private static IEnumerable<IHtmlTableCellElement> RowChildren(IHtmlTableRowElement re)
        {
            return re.Children<IHtmlTableCellElement>()
                .Value.SelectMany(child => Linq.Range(child.ColumnSpan), (child, _) => child);
        }

        public HtmlTable(IHtmlTableElement e)
        {
            ColumnTitles = e.QuerySelectorAll("th").Select(head => head.TextContent).ToList();
            _columnTitleToIndex = ColumnTitles.Enumerate().ToDictionary(tup => tup.Elem, tup => tup.Index);

            _rows = ColumnTitles.None()
                ? e.QuerySelectorAll("tr").Select(row => (IHtmlTableRowElement) row).ToList()
                : e.QuerySelectorAll("tr").Skip(1).Select(row => (IHtmlTableRowElement) row).ToList();
        }

        public IEnumerable<IHtmlTableCellElement> this[int index]
        {
            get
            {
                return _rows
                    .Select(row => Try.Of(() => RowChildren(row).Skip(index).First()))
                    .Where(res => res.HasValue)
                    .Select(res => res.Value);
            }
        }

        public IEnumerable<IHtmlTableCellElement> this[string columnName] => this[_columnTitleToIndex[columnName]];
    }
}