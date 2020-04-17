import React from "react";
import {QueryType, queryTypeToString, Suggestion, suggestions} from "./Query";

export interface SearchBarProps {
    className: string;
    onSubmit: (parameter: string, type: QueryType) => void;
   
}

export interface SearchBarState {
    hideSuggestions: boolean;
    suggestions: Suggestion[];
}

export class SearchBar extends React.Component<SearchBarProps, SearchBarState> {
    public static defaultProps: Partial<SearchBarProps> = {
        className: ""
    };

    public constructor(props: SearchBarProps) {
        super(props);
        this.keyUp = this.keyUp.bind(this);
        this.state = {hideSuggestions: true, suggestions: []};
    }

    public async keyUp(e: React.KeyboardEvent<HTMLInputElement>) {
        const val = e.currentTarget.value;

        if (e.key === "Enter") {
            this.setState({hideSuggestions: true});

            if (val === "") {
                return;
            } 

            if (this.state.suggestions.length === 0) {
                return;
            }

            const s = this.state.suggestions[0];
            e.currentTarget.value = s.value;
            this.props.onSubmit(s.value, s.type);
            
            return;
        }

        if (e.key === "Escape") {
            e.currentTarget.blur();
            this.setState({hideSuggestions: true});
            return;
        }

        if (val === "") {
            this.setState({hideSuggestions: true, suggestions: []});
            return;
        }

        const sugg = await suggestions(val);
        this.setState({hideSuggestions: false, suggestions: sugg.slice(0, 7)});
    }

    public clickSuggestionFactory(parameter: string, type: QueryType) {
        return () => {
            parameter = this.state.suggestions[0].value;
            this.setState({hideSuggestions: true});
        };
    }
    
    

    public render() {
        return (<>
            <input
                className="w-75 py-2 px-4 m-2"
                onKeyUp={this.keyUp}
                placeholder="Enter Runescape username and password"
                onFocus={e => this.setState({hideSuggestions: e.currentTarget.value === ""})}
                onBlur={() => this.setState({hideSuggestions: true})}
            />
                
            <div className={`position-relative w-75 ${this.state.hideSuggestions ? "d-none" : ""}`}>
                <div className="position-absolute w-100 d-flex flex-column bg-light border py-2 my-2">
                    {this.state.suggestions.map(s => <div
                        className="d-flex suggestion-link px-2 my-1 py-2"
                        key={`${s.type}-${s.value}`}
                        onMouseDown={this.clickSuggestionFactory(s.value, s.type)}>
                        
                        <b>{s.value}</b>
                        <div className="flex-grow-1"/>
                        <i>{queryTypeToString(s.type)}</i>
                        
                    </div>)}
                </div>
            </div>
        </>);
    }
}