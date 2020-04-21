import React from "react";

export const makeColoredStyle = (val: string, min: number, max: number, minHue: number = 0, maxHue: number = 100, saturation: number = 90, luminance: number = 35) => {
    const pct = (maxHue - minHue) * ((parseFloat(val) - min) / (max - min)) + minHue;
    return {color: `hsl(${Math.round(pct).toFixed(2)}, ${Math.round(saturation)}%, ${Math.round(luminance)}%)`};
};

export const makeColoredCell = (val: string, min: number, max: number, minHue: number = 0, maxHue: number = 100, saturation: number = 90, luminance: number = 35) => {
    const pct = (maxHue - minHue) * ((parseFloat(val) - min) / (max - min)) + minHue;
    return <td style={{color: `hsl(${Math.round(pct).toFixed(2)}, ${Math.round(saturation)}%, ${Math.round(luminance)}%)`}}>
        {val}
    </td>;
};
