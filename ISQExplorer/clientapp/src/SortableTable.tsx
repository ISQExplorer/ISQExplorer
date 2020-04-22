import React from "react";
// eslint-disable-next-line no-unused-vars
import {entryAvgRating, entries, ISQEntry, EntryOrderBy, entrySort, QueryType, Professor, Term} from "./Query";

export interface SortableTableProps {
    headings: string[];
    rows: { element: JSX.Element, order?: string | number }[][];
    index: number;
    descending: boolean;
    className: string;
    key: string;
}

export interface SortableTableState {
    index: number;
    descending: boolean;
    rows: { elements: { element: JSX.Element, order?: string | number }[], id: number }[];
}

export class SortableTable extends React.Component<SortableTableProps, SortableTableState> {
    public static defaultProps: Partial<SortableTableProps> = {
        className: "",
        index: 0,
        descending: true
    };

    public constructor(props: SortableTableProps) {
        super(props);

        const rowLen = props.headings.length;
        if (rowLen === 0) {
            throw new Error("Cannot make a table out of no headings");
        }

        for (const row of props.rows) {
            if (row.length != rowLen) {
                throw new Error("All rows must be the same length as the heading");
            }
        }

        for (let i = 0; i < rowLen; ++i) {
            const type = typeof props.rows[0][i].order;
            for (let j = 1; j < props.rows.length; ++j) {
                if (typeof props.rows[j][i].order !== type) {
                    throw new Error("All columns must have the same order type.");
                }
            }
        }

        this.state = {
            index: props.index, descending: props.descending, rows:
                props.rows.map((x, i) => ({elements: x, id: i}))
        };
        this.changeOrder = this.changeOrder.bind(this);

        this.changeOrder(this.state.index, this.state.descending);
    }

    public componentDidUpdate(prev: Readonly<SortableTableProps>): void {
        if (prev.rows !== this.props.rows ||
        prev.descending !== this.props.descending ||
        prev.index !== this.props.index ||
        prev.headings !== this.props.headings ||
        prev.className !== this.props.className) {
            const rowLen = this.props.headings.length;
            if (rowLen === 0) {
                throw new Error("Cannot make a table out of no headings");
            }

            for (const row of this.props.rows) {
                if (row.length != rowLen) {
                    throw new Error("All rows must be the same length as the heading");
                }
            }

            for (let i = 0; i < rowLen; ++i) {
                const type = typeof this.props.rows[0][i].order;
                for (let j = 1; j < this.props.rows.length; ++j) {
                    if (typeof this.props.rows[j][i].order !== type) {
                        throw new Error("All columns must have the same order type.");
                    }
                }
            }

            this.setState({
                index: this.props.index, descending: this.props.descending, rows:
                    this.props.rows.map((x, i) => ({elements: x, id: i}))
            }, () => this.changeOrder(this.state.index, this.state.descending));
        }
    } 

    private changeOrder(index: number, desc?: boolean) {
        if (desc == null) {
            desc = index !== this.state.index || !this.state.descending;
        }

        this.setState({
            index: index, descending: desc, rows: this.state.rows.sort((a, b) => {
                const at = a.elements[index].order;
                const bt = b.elements[index].order;
                
                if (at === undefined || bt === undefined) {
                    return 0;
                }

                if (typeof at === "number" || typeof bt === "number") {
                    return (desc ? -1 : 1) * (Number(at) - Number(bt));
                }

                return (desc ? -1 : 1) * at.localeCompare(bt);
            })
        });
    }

    public render() {
        return (
            <table className={this.props.className}>
                <thead>
                    {this.props.headings.map((x, i) =>
                        typeof this.state.rows[0].elements[i].order === "undefined" ?
                            <th>{x}</th>
                            :
                            <th className="link" onClick={() => this.changeOrder(i)} key={x}>
                                {x}{this.state.index === i && ((this.state.descending ? " ▼" : " ▲"))}
                            </th>)}
                </thead>
                <tbody>
                    {this.state.rows.map(x =>
                        <tr key={x.id}>
                            {x.elements.map((y, i) => <td key={this.props.key + i}>{y.element}</td>)}
                        </tr>)}
                </tbody>
            </table>
        );
    }
}
