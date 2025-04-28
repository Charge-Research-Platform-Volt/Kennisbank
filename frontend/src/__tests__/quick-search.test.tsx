import type React from "react";
import type { LinkProps } from "next/link";
import { toast } from "sonner";

// Testing library
import "@testing-library/jest-dom";
import { vi, describe, it, expect, beforeEach, afterEach } from "vitest";
import { render, screen, act } from "@testing-library/react";
import userEvent from "@testing-library/user-event";

// Component to test
import QuickSearch from "@/components/quick-search";
import { QuickSearchProvider } from "@/context/quick-search-provider";
import * as openFileActions from "@/actions/openFileActions";

// Mock dependencies
vi.mock("sonner", () => ({
  toast: {
    error: vi.fn(),
  },
}));

// Mock next/link
vi.mock("next/link", () => ({
  default: ({ children, ...props }: { children: React.ReactNode } & LinkProps) => {
    const { href, ...rest } = props;
    return (
      <a href={href.toString()} {...rest}>
        {children}
      </a>
    );
  },
}));

// Mocking the fetch API
const mockSearchResults = {
  message: "Success",
  pageIndex: 1,
  pageSize: 10,
  responseType: "SearchFullTextResponse",
  resources: [
    // Changed from 'files' to 'resources'
    {
      title: "Document 1",
      id: "doc1",
      description: "Test description",
      hash: "hash1",
      fileType: "pdf",
      typeId: "type1",
      languageCode: "en",
      publicationCode: null,
      license: null,
      note: null,
      creationDate: "2025-01-01",
      publicationDate: "2025-01-02",
    },
    {
      title: "File 1",
      id: "doc2",
      description: "",
      hash: null,
      fileType: "docx",
      typeId: "type1",
      languageCode: "en",
      publicationCode: null,
      license: null,
      note: null,
      creationDate: "2025-02-01",
      publicationDate: "2025-02-02",
    },
  ],
};

// Mocking the openFileActions module
vi.mock("@/actions/openFileActions", () => ({
  handleOpenFile: vi.fn(),
}));

// Types
interface CustomWindow extends Window {
  hotkeyCallback: () => void;
}

describe("QuickSearch Component test", () => {
  // ---------------------------------------------------------------------------
  // This code runs before each test
  // ---------------------------------------------------------------------------
  beforeEach(() => {
    vi.clearAllMocks();

    // Mock fetch
    global.fetch = vi.fn().mockImplementation(() => {
      return Promise.resolve({
        ok: true,
        json: () => Promise.resolve(mockSearchResults),
      });
    });

    // Mock hotkeys
    vi.mock("react-hotkeys-hook", () => ({
      useHotkeys: (key: string, callback: () => void) => {
        // Store the callback to trigger it in tests
        (window as unknown as CustomWindow).hotkeyCallback = callback;
      },
    }));

    // Reset timer mocks
    vi.useFakeTimers();
  });

  // ---------------------------------------------------------------------------
  // This code runs after each test
  // ---------------------------------------------------------------------------
  afterEach(() => {
    vi.useRealTimers();
  });

  // ---------------------------------------------------------------------------
  // Test cases
  // ---------------------------------------------------------------------------

  // Test-1
  it("renders the search button correctly", () => {
    render(
      <QuickSearchProvider>
        <QuickSearch />
      </QuickSearchProvider>,
    );

    // Check if the buttons are rendered
    expect(screen.getByText("Search")).toBeInTheDocument();
  });

  // Test-2
  it("opens the dialog when search button is clicked", async () => {
    render(
      <QuickSearchProvider>
        <QuickSearch />
      </QuickSearchProvider>,
    );

    const searchButton = screen.getByText("Search");

    // Click the search button
    await act(async () => {
      userEvent.click(searchButton);
    });

    // Check if search input is rendered
    expect(screen.getByPlaceholderText("Search")).toBeInTheDocument();

    // Check if the dialog is open
    const dialog = document.getElementById("radix-:rg:");
    expect(dialog).not.toBeInTheDocument();
  });

  // Test-3
  it("opens the dialog when hotkey is pressed", async () => {
    render(
      <QuickSearchProvider>
        <QuickSearch />
      </QuickSearchProvider>,
    );

    // Trigger the hotkey
    await act(async () => {
      (window as unknown as CustomWindow).hotkeyCallback();
    });

    // Check if search input is rendered
    expect(screen.getByPlaceholderText("Search")).toBeInTheDocument();

    // Check if the dialog is open
    const dialog = document.getElementById("radix-:rg:");
    expect(dialog).not.toBeInTheDocument();
  });

  //Test-4
  it("fetches search results when dialog is opened", async () => {
    render(
      <QuickSearchProvider>
        <QuickSearch />
      </QuickSearchProvider>,
    );

    const searchButton = screen.getByText("Search");

    // Click the search button
    await act(async () => {
      userEvent.click(searchButton);
    });

    // useEffect trigger fetch after dialog opens
    expect(fetch).toHaveBeenCalledTimes(1);
    expect(fetch).toHaveBeenCalledWith("http://localhost:8080/Search/search-full-text?pageIndex=1&pageSize=10", {
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      method: "POST",
    });

    // Resolve any pending promises
    await act(async () => {
      await Promise.resolve();
    });

    // Check if the results are displayed
    expect(screen.getByText("Document 1")).toBeInTheDocument();
    expect(screen.getByText("File 1")).toBeInTheDocument();

    // Test that document description is displayed
    expect(screen.getByText("Test description")).toBeInTheDocument();

    // Test if the buttons triggers the action
    await userEvent.click(screen.getByText("Document 1"));
    expect(openFileActions.handleOpenFile).toHaveBeenCalledWith(mockSearchResults.resources[0]);

    await userEvent.click(screen.getByText("File 1"));
    expect(openFileActions.handleOpenFile).toHaveBeenCalledWith(mockSearchResults.resources[1]);
  });

  // Test-5
  it("handles search input and triggers search", async () => {
    render(
      <QuickSearchProvider>
        <QuickSearch />
      </QuickSearchProvider>,
    );

    const searchButton = screen.getByText("Search");

    // Click the search button
    await act(async () => {
      userEvent.click(searchButton);
    });

    // Type in the search input "Document 1"
    const searchInput = screen.getByPlaceholderText("Search");
    const query = "Document 1";
    userEvent.type(searchInput, query);

    vi.advanceTimersByTime(300);

    expect(fetch).toHaveBeenCalledTimes(2); //One when the dialog is opened and one when the search input is typed
    expect(fetch).toHaveBeenCalledWith(`http://localhost:8080/Search/search-full-text?query=${query}&pageIndex=1&pageSize=10`, {
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      method: "POST",
    });

    // Resolve any pending promises
    await act(async () => {
      await Promise.resolve();
    });

    // Check that Document 1 is still in the document
    expect(screen.getByText("Document 1")).toBeInTheDocument();

    // Verify the correct description is still shown
    expect(screen.getByText("Test description")).toBeInTheDocument();

    // Check that the "No results found" message is not displayed
    expect(screen.queryByText("No results found.")).not.toBeInTheDocument();
  });

  // Test-6
  it("unavailable search results", async () => {
    render(
      <QuickSearchProvider>
        <QuickSearch />
      </QuickSearchProvider>,
    );

    const searchButton = screen.getByText("Search");

    await act(async () => {
      userEvent.click(searchButton);
    });

    // Mock fetch to return empty results for this specific query
    const query = "wefwefwopifwef09iqfopm09uf028r02394jfpo2jfp9023ur";
    global.fetch = vi.fn().mockImplementation((url) => {
      if (url.includes(query)) {
        return Promise.resolve({
          ok: true,
          json: () =>
            Promise.resolve({
              message: "Success",
              pageIndex: 1,
              pageSize: 10,
              responseType: "SearchFullTextResponse",
              files: [],
            }),
        });
      }

      return Promise.resolve({
        ok: true,
        json: () => Promise.resolve(mockSearchResults),
      });
    });

    // Type in the search input
    const searchInput = screen.getByPlaceholderText("Search");
    userEvent.type(searchInput, query);

    vi.advanceTimersByTime(300);
    expect(fetch).toHaveBeenCalledWith(`http://localhost:8080/Search/search-full-text?query=${query}&pageIndex=1&pageSize=10`, {
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      method: "POST",
    });

    await act(async () => {
      await Promise.resolve();
    });

    // Check that Document 1 is no longer in the document
    expect(screen.queryByText("Document 1")).not.toBeInTheDocument();

    // Verify the description is not in the document
    expect(screen.queryByText("Test description")).not.toBeInTheDocument();

    // Check that File 1 is no longer in the document
    expect(screen.queryByText("File 1")).not.toBeInTheDocument();
  });

  // Test-7
  it("shows error toast when fetch fails", async () => {
    // Mock fetch to return a failed response
    global.fetch = vi.fn().mockImplementation(() => {
      return Promise.resolve({
        ok: false,
        status: 500,
        json: () => Promise.resolve({}),
      });
    });

    render(
      <QuickSearchProvider>
        <QuickSearch />
      </QuickSearchProvider>,
    );

    const searchButton = screen.getByText("Search");

    // Click the search button to open dialog
    await act(async () => {
      userEvent.click(searchButton);

      // Wait for useEffect to trigger fetch after dialog opens
      await Promise.resolve();
    });

    // Verify the toast error was called with the expected error message
    expect(toast.error).toHaveBeenCalledWith("An error occurred while fetching search results.");
  });

  // Test-8
  it("shows generic error toast when a network error occurs", async () => {
    // Mock fetch to throw a network error
    global.fetch = vi.fn().mockImplementation(() => {
      throw new Error("Network error");
    });

    render(
      <QuickSearchProvider>
        <QuickSearch />
      </QuickSearchProvider>,
    );

    const searchButton = screen.getByText("Search");

    // Click the search button to open dialog
    await act(async () => {
      userEvent.click(searchButton);

      // Wait for useEffect to trigger fetch after dialog opens
      await Promise.resolve();
    });

    // Verify the toast error was called with the generic error message
    expect(toast.error).toHaveBeenCalledWith("An error occurred.");
  });

  // Test-9
  it("closes the dialog when clicking outside", async () => {
    render(
      <QuickSearchProvider>
        <QuickSearch />
      </QuickSearchProvider>,
    );

    const searchButton = screen.getByText("Search");

    // Click the search button to open dialog
    await act(async () => {
      userEvent.click(searchButton);
    });

    // Verify the dialog is open
    expect(screen.getByPlaceholderText("Search")).toBeInTheDocument();

    const dialogOverlay = document.querySelector('[data-slot="dialog-overlay"]');
    userEvent.click(dialogOverlay as HTMLElement);

    // Wait for any state updates to complete
    await act(async () => {
      await Promise.resolve();
    });

    const dialog = document.getElementById("radix-:rg:");
    expect(dialog).not.toBeInTheDocument();
  });
});

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
