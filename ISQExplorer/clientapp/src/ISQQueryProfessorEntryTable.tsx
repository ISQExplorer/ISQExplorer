import {avgRating, entries, ISQEntry, ISQQueryType, QueryOrderBy, seasonToString, sortedEntries} from "./Query";
import React from "react";

export interface ISQQueryProfessorEntryTableProps {
    className: string;
    parameter: string;
}

export interface ISQQueryProfessorEntryTableState {
    entries: ISQEntry[];
    orderBy: QueryOrderBy;
    orderByDescending: boolean;
}

export class ISQQueryProfessorEntryTable extends React.Component<ISQQueryProfessorEntryTableProps, ISQQueryProfessorEntryTableState> {
    public static defaultProps: Partial<ISQQueryProfessorEntryTableProps> = {
        className: ""
    };

    public constructor(props: ISQQueryProfessorEntryTableProps) {
        super(props);

        this.state = {
            entries: [],
            orderBy: QueryOrderBy.Time,
            orderByDescending: true
        };

        this.updateOrder = this.updateOrder.bind(this);

        const res = entries(this.props.parameter,
            /[A-Za-z]{3}[0-9]{4,}/.test(this.props.parameter) ?
                ISQQueryType.CourseCode :
                ISQQueryType.CourseName);
        res.then(entries => {
            this.setState({entries: sortedEntries(entries, this.state.orderBy, this.state.orderByDescending)});
        });
    }

    private updateOrder(clicked: QueryOrderBy) {
        if (clicked === this.state.orderBy) {
            this.setState({
                entries: sortedEntries(this.state.entries, this.state.orderBy, !this.state.orderByDescending),
                orderByDescending: !this.state.orderByDescending
            });
        } else {
            this.setState({
                entries: sortedEntries(this.state.entries, clicked, clicked === QueryOrderBy.Time),
                orderBy: clicked,
                orderByDescending: clicked === QueryOrderBy.Time
            });
        }
    }

    private makeHeading(orderBy: QueryOrderBy) {
        let innerString = "";
        switch (orderBy) {
        case QueryOrderBy.Gpa:
            innerString = "Average GPA";
            break;
        case QueryOrderBy.Time:
            innerString = "Semester";
            break;
        case QueryOrderBy.Course:
            innerString = "Course";
            break;
        case QueryOrderBy.Rating:
            innerString = "Average Rating";
            break;
        }

        return (
            <th onClick={() => this.updateOrder(orderBy)}>
                {innerString}{this.state.orderBy === orderBy && this.state.orderByDescending ? " ▼" : " ▲"}
            </th>
        );
    }

    // noinspection JSMethodCanBeStatic
    private makeColoredCell(val: number, min: number, max: number, minHue: number = 0, maxHue: number = 100, saturation: number = 90, luminance: number = 35) {
        const pct = (maxHue - minHue) * ((val - min) / (max - min)) + minHue;
        return <td style={{color: `hsl(${Math.round(pct)}, ${Math.round(saturation)}%, ${Math.round(saturation)}, ${Math.round(luminance)})`}}>
            {val}
        </td>;
    }

    public render() {
        if (this.state.entries === []) {
            return <h2>Loading...</h2>;
        }

        return (
            <table className={this.props.className}>
                <thead>
                    <tr>
                        {this.makeHeading(QueryOrderBy.Time)}
                        <th>CRN</th>
                        {this.makeHeading(QueryOrderBy.Course)}
                        <th>Percent Responded</th>
                        {this.makeHeading(QueryOrderBy.Rating)}
                        {this.makeHeading(QueryOrderBy.Gpa)}
                    </tr>
                </thead>
                <tbody>
                    {this.state.entries.map(entry => (
                        <tr key={entry.Crn}>
                            <td>{`${seasonToString(entry.Season)} ${entry.Year}`}</td>
                            <td>{entry.Crn}</td>
                            <td>{entry.Course.CourseCode}</td>
                            {this.makeColoredCell(100.0 * entry.NResponded / entry.NEnrolled, 0, 100)}
                            {this.makeColoredCell(avgRating(entry), 1, 5)}
                            {this.makeColoredCell(entry.MeanGpa, 0, 4)}
                        </tr>
                    ))}
                </tbody>
            </table>
        );
    }
}