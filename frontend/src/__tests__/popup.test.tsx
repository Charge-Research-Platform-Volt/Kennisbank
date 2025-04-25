import { expect, test, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import NewButton from '@/components/sidebar/left-sidebar/sidebar-new-button'
import { TagArraySchema } from '@/types/tag.type'
import TagSelectionDropdown from '@/components/uploadComponents/TagSelectionDropdown'
import { AddUserTag, fetchTagSearch } from '@/actions/tagActions'
import userEvent from '@testing-library/user-event'


// Mock fetchTagSearch once for all tests
vi.mock('@/actions/tagActions', () => ({
    fetchTagSearch: vi.fn(),
    AddUserTag: vi.fn()
  }));

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

const expectedComponents : string[] = ['Upload New Document', 'Upload New Website'];
const popUpFileText : string[] = ['Tags:', 'Upload File', 'Document Title:', 'Description:', 'Author Name:', 'Upload', 'Create'];
const popUpWebsiteText: string[] = ['Upload Website', 'Website URL:'];
const addTagText: string[] = ['Add', 'Cancel']

describe('popup', () => {

    beforeEach(() => {
        vi.clearAllMocks();
    });

    test('There are buttons that work correctly and render components with correct names', async() =>{
        render(<NewButton minimize={false}/>);
        const buttonName = await screen.queryAllByTestId("button_text").map(elem => elem.textContent ?? '');
        expect(buttonName).toEqual(["New"]); // 1 button

        // THE RADIX DROPDOWNMENU USES KEYDOWN INSTEAD OF CLICK EVENT
        const button = screen.getByTestId('button_text');
        fireEvent.keyDown(button, { key: 'Enter' });
        const componentNames = await screen.queryAllByTestId('button_in');

        expect(componentNames).toHaveLength(expectedComponents.length); // 3 buttons

        expectedComponents.forEach(element => {
            expect(screen.getByText(element)).toBeInTheDocument(); // correct names for buttons
        });

        // CLICKS ON UPLOAD NEW DOCUMENT
        fireEvent.click(componentNames[0]);
        const popupNames = await screen.queryAllByTestId('popup_text');

        expect(popupNames).toHaveLength(popUpFileText.length);

        popUpFileText.forEach(element => {
            expect(screen.getByText(element)).toBeInTheDocument();
        });
    });

    test('Upload File button works correctly', async() => {
        render(<NewButton minimize={false}/>);

        const button = screen.getByTestId('button_text');
        fireEvent.keyDown(button, { key: 'Enter' });
        const componentNames = await screen.queryAllByTestId('button_in');
        fireEvent.click(componentNames[0]);

        expect(screen.getByText('Upload File')).toBeInTheDocument();
    });

    test('Upload Website button works correctly', async() => {
        render(<NewButton minimize={false}/>);

        const button = screen.getByTestId('button_text');
        fireEvent.keyDown(button, { key: 'Enter' });
        const componentNames = await screen.queryAllByTestId('button_in');
        fireEvent.click(componentNames[1]);

        popUpWebsiteText.forEach(element => {
            expect(screen.getByText(element)).toBeInTheDocument();
        });
    });

    test('The tag selection dropdown works as expected', async() => {

        const mockedSearch = vi.mocked(fetchTagSearch).mockResolvedValue(testTags);

        // RENDER DROPDOWNBOX
        render(<TagSelectionDropdown className="test"></TagSelectionDropdown>);
        const inputTags = await screen.getByTestId("input_tags");

        // SIMULATE INPUT
        fireEvent.change(inputTags, { target: { value: "b" } });

        // WAIT FOR MOCK TO FINISH
        await waitFor(() => {
            expect(mockedSearch).toHaveBeenCalledWith('b', expect.any(Number)); // Check if fetchTagSearch was called
        });

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
  });

  test('Creating a new tag in the dropdown works', async() => {
    const mockedAdd = vi.mocked(AddUserTag).mockResolvedValue({message: "Tag added successfully", success: true});


    // RENDER DROPDOWNBOX
    render(<TagSelectionDropdown className="test"></TagSelectionDropdown>);
    const createButton = await screen.getByTestId("create_cancel_button");

    // CLICK ON CREATE
    fireEvent.click(createButton);

    // CHECK IF NEW TAB APPEARS WITH CORRECT TEXT
    const createField = await screen.getByTestId("tag_create_name");
    const addButton = await screen.getByTestId("tag_add_button");
    expect(createField).not.toBeNull();
    expect(addButton).not.toBeNull();

    addTagText.forEach(element => {
        expect(screen.getByText(element)).toBeInTheDocument();
    });

    expect(screen.getByPlaceholderText('Enter new tag name')).toBeInTheDocument();

    // CHECK IF ADDING TAG INVOKES CORRECT FUNCTION
    fireEvent.change(createField, { target: { value: "abctestblablabla" } });
    fireEvent.click(addButton);

    const formData = new FormData();
    formData.append("name", "abctestblablabla");
    const prevState = {success: false, message: '', inputs: { name: ''}}

    await waitFor(() => {
        expect(mockedAdd).toHaveBeenCalledWith(
            expect.objectContaining(prevState), // Argument 1
            expect.any(FormData) // Argument 2
        );
    });


    // CHECK IF AFTER CREATING A NEW TAG IT TAKES USER BACK TO NORMAL STATE
    expect(screen.getByText('Create')).toBeInTheDocument();
  });

});


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


