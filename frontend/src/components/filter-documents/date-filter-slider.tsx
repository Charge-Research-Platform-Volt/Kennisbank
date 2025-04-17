'use client'

import React, { useEffect } from "react";
import { useState } from "react";
import RangeSlider, { InputEvent } from 'react-range-slider-input';
import 'react-range-slider-input/dist/style.css';
import "./date-filter-slider.css"
import { FInput } from "../ui/Popup";


const curYear = new Date().getUTCFullYear();
type range = [old: number, new: number];

export default function DateFilterSlider( {className, setStartYearAction, setEndYearAction} : {className?: string, setStartYearAction: (year: number) => void, setEndYearAction: (year: number) => void}) {
    // Ranges of the slider and textbox
    const [oldestUpload, setOldestUpload] = useState<number>(1900)
    const [curRange, changeCurRange] = useState<range>([oldestUpload, curYear]);
    const [curTextboxRange, changeCurTextboxRange] = useState<range>([oldestUpload, curYear]);

    // Get oldest uploaded document and set minimum of that slider to that year
    useEffect(() => {
        fetchOldest().then((oldestYear) => {
            setOldestUpload(oldestYear);
            changeCurRange([oldestYear, curYear]);
            changeCurTextboxRange([oldestYear, curYear]);
        });
    }, [setStartYearAction, setEndYearAction]);

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

    async function fetchOldest() {
        try{
            const response = await fetch(`http://localhost:8080/search/get-oldest-document`, {
                credentials: "include",
                method: "GET",
                headers: {
                  "Content-Type": "application/json",
                }
              });
    
              if (response.ok) {
                const data = await response.json();
                return data["fileInfo"] as number
              } 
              else {
                console.error(response.body);
                return 1900
              }
        }
        catch(error){
            console.error(error)
            return 1900
        }
    }

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