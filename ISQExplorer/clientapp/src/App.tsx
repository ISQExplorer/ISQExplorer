import React from "react";
import "./App.css";
import {EntryTable} from "./EntryTable";
import {QueryType} from "./Query";
import {SearchBar} from "./SearchBar";


export interface AppState {
    query: string;
    type: QueryType;
}

export class App extends React.Component<{}, AppState> {
    public constructor(props: {}) {
        super(props);
        this.doSearch = this.doSearch.bind(this);
        this.state = {query: "", type: QueryType.CourseCode};
    }
    
    private doSearch(parameter: string, type: QueryType) {
        this.setState({query: parameter, type: type});
    }
    
    public render() {
        return (
            <div className="d-flex flex-column align-items-center w-100 p-4 m-4">
                <SearchBar onSubmit={this.doSearch} />
                {this.state.query !== "" && <EntryTable className="w-100" queryType={this.state.type} parameter={this.state.query}/>}
            </div>
        );
    }
}

export default App;
