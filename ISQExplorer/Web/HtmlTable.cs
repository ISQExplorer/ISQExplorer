using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Html.Dom;
using ISQExplorer.Exceptions;
using ISQExplorer.Functional;
using ISQExplorer.Misc;

namespace ISQExplorer.Web
{
    public class HtmlTable
    {
        private readonly IList<IHtmlTableRowElement> _rows;
        public readonly IReadOnlyList<string> ColumnTitles;
        private readonly IDictionary<string, int> _columnTitleToIndex;

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
                .Where(x => x.Children.All(y => y is IHtmlTableHeaderCellElement))
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
                    .Select(x => x.Select(y => y.TextContent.Trim()).Join(" ").Trim())
                    .ToList();

                _rows = e.QuerySelectorAll("tr").Skip(headings.Count).Select(row => (IHtmlTableRowElement) row).ToList();
            }
            else
            {
                ColumnTitles = Array.Empty<string>();

                _rows = e.QuerySelectorAll("tr").Select(row => (IHtmlTableRowElement) row).ToList();
            }

            _columnTitleToIndex = ColumnTitles.Enumerate()
                .Distinct((i1, i2) => i1.Index == i2.Index)
                .ToDictionary(tup => tup.Elem, tup => tup.Index);
        }

        public IEnumerable<IHtmlTableCellElement> this[int index]
        {
            get
            {
                return _rows
                    .Select(row => RowChildren(row).Skip(index).FirstOrDefault())
                    .Where(res => res != null);
            }
        }

        public IEnumerable<IDictionary<string, IHtmlTableCellElement>> Rows =>
            _rows.Select(RowChildren).Select(x => x.Zip(ColumnTitles).ToDictionary(y => y.Second, y => y.First));

        public IEnumerable<IHtmlTableCellElement> this[string columnName] => this[_columnTitleToIndex[columnName]];
    }
}