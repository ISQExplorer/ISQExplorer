import React from "react";
import {avgRating, entries, ISQEntry, ISQQueryType, QueryOrderBy, seasonToString, sortedEntries,} from "./Query";

export interface ISQQueryCourseResultProps {
    parameter: string
}

export interface ISQQueryCourseResultState {
    entries: ISQEntry[];
    orderBy: QueryOrderBy;
    orderByDescending: boolean;
}

export class ISQQueryCourseResult extends React.Component<ISQQueryCourseResultProps, ISQQueryCourseResultState> {
    public constructor(props: ISQQueryCourseResultProps) {
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
        case QueryOrderBy.LastName:
            innerString = "Professor";
            break;
        case QueryOrderBy.Rating:
            innerString = "Average Rating";
        }
        
        return (
            <th onClick={() => this.updateOrder(orderBy)}>
                {innerString}{this.state.orderBy === orderBy && this.state.orderByDescending ? " ▼" : " ▲"}
            </th>
        );
    }

    public render() {
        if (this.state.entries === []) {
            return <h2>Loading...</h2>;
        }

        return (
            <table className="table">
                <thead>
                    <tr>
                        {this.makeHeading(QueryOrderBy.Time)}
                        <th>CRN</th>
                        {this.makeHeading(QueryOrderBy.LastName)}
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
                            <td>{entry.Professor.LastName}</td>
                            <td>{100.0 * entry.NResponded / entry.NEnrolled}</td>
                            <td>{avgRating(entry)}</td>
                            <td>{entry.MeanGpa}</td>
                        </tr>
                    ))}
                </tbody>
            </table>
        );
    }
}

