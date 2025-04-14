'use client'

import React from "react";
import { useState } from "react";
import RangeSlider, { InputEvent } from 'react-range-slider-input';
import 'react-range-slider-input/dist/style.css';
import "./date-filter-slider.css"
import { FInput } from "../ui/Popup";


const curYear = new Date().getUTCFullYear();
const oldestUpload = 1968; // REPLACE WITH ACTUAL OLDEST DOCUMENT

type range = [old: number, new: number];

export default function DateFilterSlider( {className, setStartYearAction, setEndYearAction} : {className?: string, setStartYearAction: (year: number) => void, setEndYearAction: (year: number) => void}) {
    // ranges of the slider and textbox
    const [curRange, changeCurRange] = useState<range>([oldestUpload, curYear]);
    const [curTextboxRange, changeCurTextboxRange] = useState<range>([oldestUpload, curYear]);

    const changeRangeSlider = (event: InputEvent) => {
        // get value of both thumbs of slider
        const newRangeValue = [...(event.values as () => IterableIterator<number>)()];
        const newRange: range = newRangeValue as range;
        // update range and visualize the change by changing the textbox
        changeCurRange(newRange)
        changeCurTextboxRange(newRange)
        setStartYearAction(newRange[0]);
        setEndYearAction(newRange[1]);
    };

    const changeRangeTextBoxMin = (event: React.ChangeEvent<HTMLInputElement>) => {
        // get the new minimum year
        const newMinValue = Number(event.target.value);
        // as long as it is a valid number, the change is seen in the textbox, but not yet in the slider
        if(!isNaN(newMinValue)){
            changeCurTextboxRange([newMinValue, curTextboxRange[1]]);
        }
        // only if the year provided is valid it is shown in the slider
        if (!isNaN(newMinValue) && newMinValue >= oldestUpload && newMinValue <= curYear && newMinValue <= curRange[1]) {
            changeCurRange([newMinValue, curRange[1]]);
        }
    };

    const changeRangeTextBoxMax = (event: React.ChangeEvent<HTMLInputElement>) => {
        // Same as above, but for the other textbox
        const newMaxValue = Number(event.target.value);
        if(!isNaN(newMaxValue)){
            changeCurTextboxRange([curTextboxRange[0], newMaxValue]);
        }
        if (!isNaN(newMaxValue) && newMaxValue >= oldestUpload && newMaxValue <= curYear && newMaxValue <= curRange[1]) {
            changeCurRange([curRange[0], newMaxValue]);
        }
    };

    return <div className={className}>
        <RangeSlider data-testid="slider" min={oldestUpload} max={curYear} value={curRange} onInput={(e) => changeRangeSlider(e)} id="range-slider-purple"/>
        <div className="flex gap-2 mt-6">
            <FInput
            data-testid="min_year"
            className="flex-grow text-white text-center font-ubuntu rounded-lg bg-[#502379] hover:bg-[#6f2aaf]"
            type="text"
            placeholder="Year oldest"
            value={curTextboxRange[0]}
            onChange={changeRangeTextBoxMin}
            />
            <FInput
            data-testid="max_year"
            className="flex-grow text-white text-center font-ubuntu rounded-lg bg-[#502379] hover:bg-[#6f2aaf]"
            type="text"
            placeholder="Year newest"
            value={curTextboxRange[1]}
            onChange={changeRangeTextBoxMax}
            />
        </div>
    </div>
}