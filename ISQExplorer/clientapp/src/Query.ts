export interface Department {
    id: number;
    name: string;
    lastUpdated: string;
}

export interface Course {
    courseCode: string;
    department: Department;
    name: string;
}

export interface Professor {
    firstName: string
    department: Department;
    lastName: string;
    nNumber: string;
}

export interface Term {
    id: number;
    name: string;
}

export interface ISQEntry {
    course: Course;
    term: Term;
    professor: Professor;
    crn: number;
    nResponded: number;
    nEnrolled: number;
    pct5: number;
    pct4: number;
    pct3: number;
    pct2: number;
    pct1: number;
    pctNa: number;
    pctA: number;
    pctAMinus: number;
    pctBPlus: number;
    pctB: number;
    pctCPlus: number;
    pctC: number;
    pctD: number;
    pctF: number;
    pctWithdraw: number;
    meanGpa: number;
}

export enum QueryType {
    // eslint-disable-next-line no-unused-vars
    CourseCode = 1 << 0,
    // eslint-disable-next-line no-unused-vars
    CourseName = 1 << 1,
    // eslint-disable-next-line no-unused-vars
    ProfessorName = 1 << 2
}

export const queryTypeToString = (q: QueryType): string => {
    switch (q) {
    case QueryType.CourseCode:
        return "Course Code";
    case QueryType.CourseName:
        return "Course Name";
    case QueryType.ProfessorName:
        return "Professor Name";
    default:
        throw new Error(`${q} is not a valid query type.`);
    }
};

export interface Suggestion {
    type: QueryType;
    value: string;
    altText: string | null;
}

const queryString = (params: { [key: string]: any }): string =>
    Object.keys(params).length > 0 ?
        "?" + Object.keys(params).map(param => `${param}=${params[param]}`).join("&") :
        "";

export const suggestions = async (parameter: string): Promise<Suggestion[]> => {
    const res = await fetch(`/Query/Suggestions/${parameter}`);
    return await res.json();
};

export const entries = async (parameter: string, queryType: QueryType, since?: Term, until?: Term): Promise<ISQEntry[]> => {
    const params: { [key: string]: any } = {};
    if (since != null) {
        params["since"] = since.id;
    }
    if (until != null) {
        params["until"] = until.id;
    }

    const res = await fetch(`/Query/Entries/${queryType}/${parameter}${queryString(params)}`);
    if (res.ok) {
        return await res.json();
    }
    throw new Error(await res.text());
};

export const entryAvgRating = (entry: ISQEntry): number => {
    return entry.pct1 * 0.01 + entry.pct2 * 0.02 + entry.pct3 * 0.03 + entry.pct4 * 0.04 + entry.pct5 * 0.05;
};

export enum EntryOrderBy {
    // eslint-disable-next-line no-unused-vars
    Gpa,
    // eslint-disable-next-line no-unused-vars
    LastName,
    // eslint-disable-next-line no-unused-vars
    Rating,
    // eslint-disable-next-line no-unused-vars
    Time,
    // eslint-disable-next-line no-unused-vars
    Course
}

export const terms = async (): Promise<Term[]> => {
    const res = await fetch("/Query/Terms");
    return await res.json();   
};

export const entrySort = (entries: ISQEntry[], orderBy: EntryOrderBy | EntryOrderBy[], descending: boolean = false): ISQEntry[] => {
    const entryComparator = (a: ISQEntry, b: ISQEntry, order: EntryOrderBy): number => {
        let comp: number = 0;
        switch (order) {
        case EntryOrderBy.Gpa:
            comp = a.meanGpa - b.meanGpa;
            break;
        case EntryOrderBy.LastName:
            comp = a.professor.lastName.localeCompare(b.professor.lastName);
            break;
        case EntryOrderBy.Rating:
            comp = entryAvgRating(a) - entryAvgRating(b);
            break;
        case EntryOrderBy.Time:
            comp = a.term.id - b.term.id;
            break;
        case EntryOrderBy.Course:
            comp = a.course.courseCode.localeCompare(b.course.courseCode);
            break;
        }
        return comp * (descending ? -1 : 1);
    };

    const ordArr = Array.isArray(orderBy) ? orderBy : [orderBy];

    return [...entries].sort((x, y) => ordArr.reduce((a, c) => a !== 0 ? a : entryComparator(x, y, c), 0));
};