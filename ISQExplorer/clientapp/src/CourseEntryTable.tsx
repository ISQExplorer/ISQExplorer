import React from "react";
// eslint-disable-next-line no-unused-vars
import {entryAvgRating, entries, ISQEntry, EntryOrderBy, entrySort, QueryType} from "./Query";

export interface CourseEntryTableProps{
    className: string;
    parameter: string;
}

export interface CourseEntryTableState {
    entries: ISQEntry[];
    orderBy: EntryOrderBy;
    orderByDescending: boolean;
}

export class CourseEntryTable extends React.Component<CourseEntryTableProps, CourseEntryTableState> {
    public static defaultProps: Partial<CourseEntryTableProps> = {
        className: ""
    };
    
    public constructor(props: CourseEntryTableProps) {
        super(props);

        this.state = {
            entries: [],
            orderBy: EntryOrderBy.Time,
            orderByDescending: true
        };

        this.updateOrder = this.updateOrder.bind(this);
        this.makeColoredCell = this.makeColoredCell.bind(this);
        this.makeHeading = this.makeHeading.bind(this);

        const res = entries(this.props.parameter,
            /[A-Za-z]{3}[0-9]{4,}/.test(this.props.parameter) ?
                QueryType.CourseCode :
                QueryType.CourseName);
        res.then(ent => {
            this.setState({entries: entrySort(ent, this.state.orderBy, this.state.orderByDescending)});
        });
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
        case EntryOrderBy.LastName:
            innerString = "Professor";
            break;
        case EntryOrderBy.Rating:
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
                        {this.makeHeading(EntryOrderBy.Time)}
                        <th>CRN</th>
                        {this.makeHeading(EntryOrderBy.LastName)}
                        <th>Percent Responded</th>
                        {this.makeHeading(EntryOrderBy.Rating)}
                        {this.makeHeading(EntryOrderBy.Gpa)}
                    </tr>
                </thead>
                <tbody>
                    {this.state.entries.map(entry => (
                        <tr key={entry.crn}>
                            <td>{entry.term.name}</td>
                            <td>{entry.crn}</td>
                            <td>{entry.professor.lastName}</td>
                            {this.makeColoredCell(100.0 * entry.nResponded / entry.nEnrolled, 0, 100)}
                            {this.makeColoredCell(entryAvgRating(entry), 1, 5)}
                            {this.makeColoredCell(entry.meanGpa, 0, 4)}
                        </tr>
                    ))}
                </tbody>
            </table>
        );
    }
}
