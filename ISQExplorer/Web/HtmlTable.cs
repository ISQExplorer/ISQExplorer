using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using ISQExplorer.Exceptions;
using ISQExplorer.Functional;
using ISQExplorer.Misc;
using Microsoft.EntityFrameworkCore.Internal;

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

        public static Try<HtmlTable, HtmlElementException> Create(IHtmlTableElement e) =>
            new Try<HtmlTable, HtmlElementException>(() => new HtmlTable(e));

        private HtmlTable(IHtmlTableElement e)
        {
            var headings = e.QuerySelectorAll("tr")
                .Where(x => x.Children.All(y => y is IHtmlHeadingElement))
                .Select(x => (IHtmlTableRowElement) x)
                .ToList();

            if (headings.Any())
            {
                var rowChildren = headings.Select(x => (Row: x, Children: RowChildren(x).ToList())).ToList();

                var num = rowChildren.First().Children.Count;
                foreach (var (row, children) in rowChildren.Skip(1))
                {
                    if (children.Count != num)
                    {
                        throw new HtmlElementException(row,
                            $"Expected all of the rows to have the same amount of cells ({num}). But this one has {RowChildren(row).Count()}");
                    }
                }

                ColumnTitles = Linq.Range(num)
                    .Select(i => rowChildren.Select(x => x.Children[i]))
                    .Select(x => x.Select(y => y.TextContent).Join(" "))
                    .ToList();
            }

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