import { expect, test } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import NewButton from '@/components/left-sidebar/sidebar-new-button'
import { TagArraySchema } from '@/types/tag.type'
import TagSelectionDropdown from '@/components/uploadComponents/TagSelectionDropdown'

const testTags = TagArraySchema.parse([
    {
        name: "bla",
        id: "3b542bc8-9b38-4c40-926d-dcd46c576fdf",
        isStandardized: false,
        isApproved: false,
        approvedOn: null,
        approvedBy: null,
        createdBy: "3b542bc8-9b38-4c40-926d-dcd46c576fdf", // Must be a valid UUID
        createdOn: "2024-01-01T12:00:00Z"
    }
]);

const expectedComponents: string[] = ["Upload New Document", "Create New Project"];
const popUpText: string[] = ["Tags:", "Upload Document", "Document Title:", "Description:", "Author Name:", "Upload"];

describe('popup', () =>
    test('There are buttons that work correctly and render components with correct names', async() =>{
        render(<NewButton tags={testTags} />);
        const buttonName = await screen.queryAllByTestId("button_text").map(elem => elem.textContent ?? '');
        expect(buttonName).toEqual(["New"]); // 1 button

      // THE RADIX DROPDOWNMENU USES KEYDOWN INSTEAD OF CLICK EVENT
      const button = screen.getByTestId("button_text");
      fireEvent.keyDown(button, { key: "Enter" });
      const componentNames = await screen.queryAllByTestId("button_in");

      expect(componentNames).toHaveLength(expectedComponents.length); // 2 buttons

      expectedComponents.forEach((element) => {
        expect(screen.getByText(element)).toBeInTheDocument(); // correct names for buttons
      });

      // CLICKS ON UPLOAD NEW DOCUMENT
      fireEvent.click(componentNames[0]);
      const popupNames = await screen.queryAllByTestId("popup_text");

      expect(popupNames).toHaveLength(popUpText.length);

      popUpText.forEach((element) => {
        expect(screen.getByText(element)).toBeInTheDocument();
      });
    }),

    test('The tag selection dropdown works as expected', async() => {
        render(<TagSelectionDropdown tags={testTags} className="test"></TagSelectionDropdown>);
        const inputTags = await screen.getByTestId("input_tags");

    // SIMULATE INPUT
    fireEvent.change(inputTags, { target: { value: "b" } });

        // GET NEW BUTTONS
        let displayedTags = await screen.getAllByTestId("select_tag"); // there is only 1
        expect(displayedTags).toHaveLength(testTags.length);

    // ADD TAG
    fireEvent.click(displayedTags[0]);
    let deleteTags = await screen.getAllByTestId("delete_tag"); // there is only 1
    expect(deleteTags).toHaveLength(1);
    fireEvent.click(deleteTags[0]);
    deleteTags = await screen.queryAllByTestId("delete_tag");
    displayedTags = await screen.queryAllByTestId("select_tag");
    expect(deleteTags).toEqual([]);
    expect(displayedTags).toEqual([]);
  }),
);
