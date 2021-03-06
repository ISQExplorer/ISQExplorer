import React from "react";
// eslint-disable-next-line no-unused-vars
import {entryAvgRating, ISQEntry, Professor, Term} from "./Query";
import {makeColoredSpan} from "./CommonTsx";
import {SortableTable} from "./SortableTable";

export interface ProfessorInfoProps {
    professor: Professor
    entries: ISQEntry[]
}

export const ProfessorInfo: React.FC<ProfessorInfoProps> = (props: ProfessorInfoProps) => {
    const entryGroups: {[courseCode: string]: {entries: ISQEntry[], name: string}} = {};
    const courseObj: {[courseCode: string]: {courseName: string, avgGpa: number, avgRating: number, count: number, lastTaught: Term}} = {};
    
    const avg = (arr: number[]) => arr.reduce((a, c) => a + c) / arr.length;
    
    props.entries.forEach(entry => entryGroups[entry.course.courseCode] ? entryGroups[entry.course.courseCode].entries.push(entry) : entryGroups[entry.course.courseCode] = {entries: [entry], name: entry.course.name});
    for (const code in entryGroups) {
        const x = entryGroups[code].entries;
        const name = entryGroups[code].name;
        const avgGpa = avg(x.map(x => x.meanGpa));
        const avgRating = avg(x.map(x => entryAvgRating(x)));
        const lastTaught = x.map(x => x.term).reduce((a, c) => a.id >= c.id ? a : c);
        
        courseObj[code] = {courseName: name, avgGpa, avgRating, count: x.length, lastTaught};
    }
    
    const avgGpa = avg(props.entries.map(x => x.meanGpa));
    const avgRating = avg(props.entries.map(x => entryAvgRating(x)));
    
    const elems = Object.values(courseObj).map(x => 
        [
            {element: <>{x.courseName}</>, order: x.courseName},
            {element: makeColoredSpan(x.avgGpa.toFixed(2), 0, 4), order: x.avgGpa},
            {element: makeColoredSpan(x.avgRating.toFixed(2), 1, 5), order: x.avgRating},
            {element: <>{x.lastTaught.name}</>, order: x.lastTaught.id}
        ]
    );
    

    return (
        <div className="d-flex w-100 py-4">
            <div className="pr-4 d-flex flex-column align-items-center">
                <h4><u>{props.professor.firstName} {props.professor.lastName}</u></h4>
                <h6>Average GPA: {makeColoredSpan(avgGpa.toFixed(2), 0, 4)}</h6>
                <h6>Average Rating: {makeColoredSpan(avgRating.toFixed(2), 1, 5)}</h6>
            </div>
            <div className="vl"/>
            <div className="pl-4 flex-grow-1 d-flex flex-column align-items-center">
                <h5><u>Courses</u></h5>
                <SortableTable className="flex-grow-1 w-100 table table-striped table-sm" headings={[
                    "Course Name",
                    "Average GPA",
                    "Average Rating",
                    "Last Taught"
                ]} rows={elems} key={props.professor.nNumber}/>
                {/*
                <table className="flex-grow-1 w-100 table table-striped table-sm">
                    <thead>
                    <tr>
                        <th>Course Name</th>
                        <th>Average GPA</th>
                        <th>Average Rating</th>
                        <th>Last Taught</th>
                    </tr>
                    </thead>
                    <tbody>
                    {Object.values(courseObj).sort((a, b) => a.courseName.localeCompare(b.courseName)).map(x => <tr>
                        <td>{x.courseName}</td>
                        {makeColoredCell(x.avgGpa.toFixed(2), 0, 4)}
                        {makeColoredCell(x.avgRating.toFixed(2), 1, 5)}
                        <td>{x.lastTaught.name}</td>
                    </tr>)}
                    </tbody>
                </table>
                */}
            </div>
        </div>
    );
};
