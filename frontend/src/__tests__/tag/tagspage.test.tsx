import { expect, test, vi, beforeEach, describe } from "vitest";
import { render, screen, fireEvent, act } from "@testing-library/react";
import CreateStandardizedTag from "@/app/(knowledgebank)/tags/_components/create-standardized-tag";
import { AddStandardizedTag, AddUserTag } from "@/actions/tagActions";
import CreateUserTag from "@/app/(knowledgebank)/tags/_components/create-tag";

// RENDERING THE TAG PAGE DOES NOT WORK DUE TO COOKIE / AUTH STUFF, SAME WITH LISTTAGS

// Mock fetchTagSearch once for all tests
vi.mock("@/actions/tagActions", () => ({
  AddStandardizedTag: vi.fn(),
  AddUserTag: vi.fn(),
}));

describe("tags page", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  test("Test if the standardized tag component displays the correct placeholder and it adds correctly", async () => {
    // CREATE MOCK OF ADDING STANDARDIZED TAG
    const mockedSAdd = vi.mocked(AddStandardizedTag).mockResolvedValue({ message: "Tag added successfully", success: true });

    // RENDER COMPONENT
    render(<CreateStandardizedTag></CreateStandardizedTag>);
    expect(screen.getByPlaceholderText("New standardized tag")).toBeInTheDocument();

    const inputfield: HTMLElement = screen.getByTestId("input");
    const addbutton: HTMLElement = screen.getByTestId("button");

    // CHECK IF ADDING TAG INVOKES CORRECT FUNCTION
    fireEvent.change(inputfield, { target: { value: "testestestestest" } });
    fireEvent.click(addbutton);

    const prevState = { success: false, message: "" }; // for some reason the prevState call differs between the popup and here
    const args = mockedSAdd.mock.calls[0]; // CHECK CALL 1

    expect(args[0]).toEqual(expect.objectContaining(prevState));

    const formDataArgument: FormData = args[1];

    expect(formDataArgument).toBeInstanceOf(FormData); // CHECKS PREVSTATE
    expect(formDataArgument.get("name")).toBe("testestestestest"); // CHECKS FORMDATA
  });

  test("Test if user tag component displays correct placeholder and it adds correctly", async () => {
    // CREATE MOCK OF ADDING USER TAG
    const mockedUserAdd = vi.mocked(AddUserTag).mockResolvedValue({ message: "Tag added successfully", success: true });

    // RENDER COMPONENT
    render(<CreateUserTag></CreateUserTag>);
    expect(screen.getByPlaceholderText("New tag")).toBeInTheDocument();

    const inputField: HTMLElement = screen.getByTestId("input");
    const addButton: HTMLElement = screen.getByTestId("button");

    // CHECK IF ADDING TAG INVOKES CORRECT FUNCTION
    fireEvent.change(inputField, { target: { value: "testestestestest" } });
    await act(async () => {
      fireEvent.click(addButton);
    });

    const prevState = { success: false, message: "" }; // for some reason the prevState call differs between the popup and here
    const args = mockedUserAdd.mock.calls[0]; // CHECK CALL 1

    expect(args[0]).toEqual(expect.objectContaining(prevState));

    const formDataArgument: FormData = args[1];

    expect(formDataArgument).toBeInstanceOf(FormData); // CHECKS PREVSTATE
    expect(formDataArgument.get("name")).toBe("testestestestest"); // CHECKS FORMDATA
  });
});

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
