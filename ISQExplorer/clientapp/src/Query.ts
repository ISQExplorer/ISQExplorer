import {ISQQueryCourseResultOrderBy} from "./ISQQueryResult";

export interface Department {
    Name: string;
}

export interface Course {
    CourseCode: string;
    Department: Department;
    Name: string;
}

export interface Professor {
    FirstName: string
    Department: Department;
    LastName: string;
    NNumber: string;
}

export enum Season {
    Spring = 0,
    Summer = 1,
    Fall = 2
}

export const seasonToString = (s: Season) => {
    switch (s) {
    case Season.Spring:
        return "Spring";
    case Season.Summer:
        return "Summer";
    case Season.Fall:
        return "Fall";
    }
};

export interface ISQEntry {
    Season: Season;
    Year: number;
    Professor: Professor;
    Crn: number;
    NResponded: number;
    NEnrolled: number;
    Pct5: number;
    Pct4: number;
    Pct3: number;
    Pct2: number;
    Pct1: number;
    PctNa: number;
    PctA: number;
    PctAMinus: number;
    PctBPlus: number;
    PctB: number;
    PctCPlus: number;
    PctC: number;
    PctD: number;
    PctF: number;
    PctWithdraw: number;
    MeanGpa: number;
}

export enum ISQQueryType {
    CourseCode = 0,
    CourseName = 1,
    ProfessorName = 2
}

export interface Term {
    Season: Season,
    Year: number
}

export enum QueryOrderBy {
    Time,
    LastName,
    Gpa,
    Rating
}

export const suggestions = async (parameter: string): Promise<ISQEntry[]> => {
    const res = await fetch(`/Query/Suggestions?parameter=${parameter}`);
    return await res.json();
};

const queryString = (params: { [key: string]: any }): string =>
    Object.keys(params).map(param => `${param}=${params[param]}`).join("&");

export const entries = async (parameter: string, queryType: ISQQueryType, since: Term | null = null, until: Term | null = null): Promise<ISQEntry[]> => {
    const params: { [key: string]: any } = {"parameter": parameter, "QueryType": queryType};
    if (since != null) {
        params["SinceSeason"] = since.Season;
        params["SinceYear"] = since.Year;
    }
    if (until != null) {
        params["UntilSeason"] = until.Season;
        params["UntilYear"] = until.Year;
    }

    const res = await fetch(`/Query/Suggestions?${queryString(params)}`);
    return await res.json();
};

export const avgRating = (entry: ISQEntry): number => {
    return entry.Pct1 * 0.01 + entry.Pct2 * 0.02 + entry.Pct3 * 0.03 + entry.Pct4 * 0.04 + entry.Pct5 * 0.05;
};

export const termCompare = (t1: Term, t2: Term): number => {
    return (t2.Year - t1.Year) * 3 + t1.Season - t2.Season;
};

export const sortedEntries = (entries: ISQEntry[], orderBy: QueryOrderBy, descending: boolean = false): ISQEntry[] => {
    return [...entries].sort((a, b) => {
        let comp: number = 0;
        switch (orderBy) {
        case QueryOrderBy.Gpa:
            comp = a.MeanGpa - b.MeanGpa;
            break;
        case QueryOrderBy.LastName:
            comp = a.Professor.LastName.localeCompare(b.Professor.LastName);
            break;
        case QueryOrderBy.Rating:
            comp = avgRating(a) - avgRating(b);
            break;
        case QueryOrderBy.Time:
            comp = termCompare({Season: a.Season, Year: a.Year}, {Season: a.Season, Year: a.Year});
            break;
        }
        return comp * (descending ? -1 : 1);
    });
};