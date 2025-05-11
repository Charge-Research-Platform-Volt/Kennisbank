import { expect, test } from 'vitest'
import { render, screen, fireEvent, act } from '@testing-library/react'
import DateFilterSlider from '@/components/filter-documents/date-filter-slider';

describe('rangeselector', () => {
    test("The components are rendered successfully", async() => {
        render(<DateFilterSlider setEndYearAction={()=>{}} setStartYearAction={()=>{}}/>);

        // When rendering it should display the current year and the year of the oldest document
        const curYear = new Date().getUTCFullYear();
        const oldYear = "1900"; // Change this to the oldest document, aka 1900 is the default
        expect(screen.getByDisplayValue(`${curYear}`));
        expect(screen.getByDisplayValue(`${oldYear}`));

        // The slider should be on screen
        const slider = await screen.getByTestId("element");
        expect(slider.children[0].ariaValueNow).toEqual(oldYear);
        expect(slider.children[1].ariaValueNow).toEqual(`${curYear}`);
    });

    test("Changing the textbox changes the slider", async() => {
        render(<DateFilterSlider setEndYearAction={()=>{}} setStartYearAction={()=>{}}/>);

        // The textboxes
        const maxBox = await screen.getByTestId("max_year");
        const minBox = await screen.getByTestId("min_year");

        // The first child is the initial left slider, the other is the initial right slider
        const slider = await screen.getByTestId("element").children;

        // Test if changing textboxes affect sliders
        act(() => {
            fireEvent.change(minBox, {target:{value:"1988"}});
        });
        expect(screen.getAllByDisplayValue(`${1988}`));
        expect(slider[0].ariaValueNow).toEqual("1988");

        fireEvent.change(maxBox, {target:{value:"2000"}});
        expect(screen.getAllByDisplayValue(`${2000}`));
        expect(slider[1].ariaValueNow).toEqual("2000");

        // Test if changing textbox to invalid value doesn't change the value
        fireEvent.change(minBox, {target:{value:"-"}});
        expect(screen.getAllByDisplayValue(`${1988}`));
    });
});

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
