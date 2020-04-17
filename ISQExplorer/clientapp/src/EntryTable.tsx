import React from "react";
// eslint-disable-next-line no-unused-vars
import {entryAvgRating, entries, ISQEntry, EntryOrderBy, entrySort, QueryType} from "./Query";

export interface EntryTableProps {
    className: string;
    parameter: string;
    queryType: QueryType;
}

export interface EntryTableState {
    entries: ISQEntry[];
    orderBy: EntryOrderBy;
    orderByDescending: boolean;
}

export class EntryTable extends React.Component<EntryTableProps, EntryTableState> {
    public static defaultProps: Partial<EntryTableProps> = {
        className: ""
    };

    public constructor(props: EntryTableProps) {
        super(props);

        this.state = {
            entries: [],
            orderBy: EntryOrderBy.Time,
            orderByDescending: true
        };

        this.updateOrder = this.updateOrder.bind(this);
        this.makeColoredCell = this.makeColoredCell.bind(this);
        this.makeHeading = this.makeHeading.bind(this);

        const res = entries(this.props.parameter, this.props.queryType);
        res.then(entries => {
            this.setState({entries: entrySort(entries, this.state.orderBy, this.state.orderByDescending)});
        });
    }
    
    
    
    public componentDidUpdate(prev: Readonly<EntryTableProps>): void {
        if (prev.parameter !== this.props.parameter || prev.queryType !== this.props.queryType) {
            const res = entries(this.props.parameter, this.props.queryType);
            res.then(entries => {
                this.setState({entries: entrySort(entries, this.state.orderBy, this.state.orderByDescending)});
            });
        }
    }

    private updateOrder(clicked: EntryOrderBy) {
        if (clicked === this.state.orderBy) {
            this.setState({
                entries: entrySort(this.state.entries, this.state.orderBy, !this.state.orderByDescending),
                orderByDescending: !this.state.orderByDescending
            });
        } else {
            this.setState({
                entries: entrySort(this.state.entries, clicked, clicked === EntryOrderBy.Time),
                orderBy: clicked,
                orderByDescending: clicked === EntryOrderBy.Time
            });
        }
    }

    private makeHeading(orderBy: EntryOrderBy) {
        let innerString = "";
        switch (orderBy) {
        case EntryOrderBy.Gpa:
            innerString = "Average GPA";
            break;
        case EntryOrderBy.Time:
            innerString = "Semester";
            break;
        case EntryOrderBy.Course:
            innerString = "Course";
            break;
        case EntryOrderBy.Rating:
            innerString = "Average Rating";
            break;
        case EntryOrderBy.LastName:
            innerString = "Last Name";
            break;
        }

        return (
            <th onClick={() => this.updateOrder(orderBy)}>
                <u className="link">{innerString}{this.state.orderBy === orderBy && (this.state.orderByDescending ? " ▼" : " ▲")}</u>
            </th>
        );
    }

    // noinspection JSMethodCanBeStatic
    private makeColoredCell(val: string, min: number, max: number, minHue: number = 0, maxHue: number = 100, saturation: number = 90, luminance: number = 35) {
        const pct = (maxHue - minHue) * ((parseFloat(val) - min) / (max - min)) + minHue;
        return <td style={{color: `hsl(${Math.round(pct)}, ${Math.round(saturation)}%, ${Math.round(luminance)}%)`}}>
            {val}
        </td>;
    }

    public render() {
        if (this.state.entries === []) {
            return <h2>Loading...</h2>;
        }

        return (
            <>
                <table className={this.props.className}>
                    <thead>
                        <tr>
                            {this.makeHeading(EntryOrderBy.Time)}
                            <th>CRN</th>
                            {this.makeHeading(EntryOrderBy.Course)}
                            {this.makeHeading(EntryOrderBy.LastName)}
                            <th>Percent Responded</th>
                            {this.makeHeading(EntryOrderBy.Rating)}
                            {this.makeHeading(EntryOrderBy.Gpa)}
                        </tr>
                    </thead>
                    <tbody>
                        {this.state.entries.map(entry => (
                            <tr key={`${entry.crn}|${entry.term.id}`}>
                                <td>{entry.term.name}</td>
                                <td>{entry.crn}</td>
                                <td>{entry.course.courseCode}</td>
                                <td>{entry.professor.lastName}</td>
                                {this.makeColoredCell(((100.0 * entry.nResponded) / entry.nEnrolled).toFixed(2), 0, 100)}
                                {this.makeColoredCell(entryAvgRating(entry).toFixed(2), 1, 5)}
                                {this.makeColoredCell((entry.meanGpa).toFixed(2), 0, 4)}
                            </tr>
                        ))}
                    </tbody>
                </table>
            </>
        );
    }
}