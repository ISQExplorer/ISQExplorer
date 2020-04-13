import React from "react";
import "./App.css";
import {EntryTable} from "./EntryTable";
import {QueryType} from "./Query";


export interface AppState {
    query: string;
}

export class App extends React.Component<{}, AppState> {
    public constructor(props: {}) {
        super(props);
        this.doSearch = this.doSearch.bind(this);
        this.state = {query: ""};
    }
    
    private doSearch(e: React.KeyboardEvent<HTMLInputElement>) {
        if (e.key !== "Enter") {
            return;
        }
       
        this.setState({query: e.currentTarget.value});
    }
    
    public render() {
        return (
            <div className="d-flex flex-column align-items-center w-100 p-4 m-4">
                <input className="w-75 py-2 px-4 m-2" onKeyDown={this.doSearch} placeholder="Enter a course code..." />
                {this.state.query !== "" && <EntryTable className="w-100" queryType={QueryType.CourseCode} parameter={this.state.query}/>}
            </div>
        );
    }
}

export default App;
