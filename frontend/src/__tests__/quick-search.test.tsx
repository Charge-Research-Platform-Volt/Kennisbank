import type React from "react";
import "@testing-library/jest-dom";
import { vi, describe, it, expect, beforeEach, afterEach } from "vitest";
import { render, screen, act } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { LinkProps } from "next/link";

import { toast } from "sonner";

// Component to test
import QuickSearch from "@/components/quick-search";

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

const mockSearchResults = {
  message: "Success",
  pageIndex: 1,
  pageSize: 10,
  responseType: "SearchFullTextResponse",
  files: [
    {
      name: "Document 1",
      id: "doc1",
      description: "Test description",
      hash: "hash1",
      fileType: "pdf",
      createdAt: "2025-01-01",
      updatedAt: "2025-01-02",
    },
    {
      name: "File 1",
      id: "doc2",
      description: "",
      hash: null,
      fileType: "docx",
      createdAt: "2025-02-01",
      updatedAt: "2025-02-02",
    },
  ],
};

interface CustomWindow extends Window {
  hotkeyCallback: () => void;
}

describe("QuickSearch Component", () => {
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

  afterEach(() => {
    vi.useRealTimers();
  });

  // ---------------------------------------------------------------------------

  // Test-1
  it("renders the search button correctly", () => {
    render(<QuickSearch />);

    expect(screen.getByText("Search")).toBeInTheDocument();
    expect(screen.getByText("Cmd + K")).toBeInTheDocument();
  });

  // Test-2
  it("opens the dialog when search button is clicked", async () => {
    render(<QuickSearch />);

    const searchButton = screen.getByText("Search");

    await act(async () => {
      // Click the search button
      userEvent.click(searchButton);
    });

    expect(screen.getByPlaceholderText("Search")).toBeInTheDocument();

    const dialog = document.getElementById("radix-:rg:");
    expect(dialog).not.toBeInTheDocument();
  });

  // Test-3
  it("opens the dialog when hotkey is pressed", async () => {
    render(<QuickSearch />);

    // Trigger the hotkey callback directly with proper act wrapping
    await act(async () => {
      (window as unknown as CustomWindow).hotkeyCallback();
    });

    expect(screen.getByPlaceholderText("Search")).toBeInTheDocument();

    const dialog = document.getElementById("radix-:rg:");
    expect(dialog).not.toBeInTheDocument();
  });

  //Test-4
  it("fetches search results when dialog is opened", async () => {
    render(<QuickSearch />);

    // Open the dialog
    const searchButton = screen.getByText("Search");
    await act(async () => {
      userEvent.click(searchButton);
    });

    expect(fetch).toHaveBeenCalledTimes(1);
    expect(fetch).toHaveBeenCalledWith("http://localhost:8080/Search/search-full-text?pageIndex=1&pageSize=10", { headers: { "Content-Type": "application/json" }, method: "GET" });

    // Advance timers to resolve any pending promises
    await act(async () => {
      await Promise.resolve();
    });

    // Now check if the results are displayed
    expect(screen.getByText("Document 1")).toBeInTheDocument();
    expect(screen.getByText("File 1")).toBeInTheDocument();

    // Test that document description is properly displayed
    expect(screen.getByText("Test description")).toBeInTheDocument();

    // Test the links are properly generated
    const links = screen.getAllByRole("link");
    expect(links[0]).toHaveAttribute("href", "/file/doc1");
    expect(links[1]).toHaveAttribute("href", "/file/doc2");
  });

  // Test-5
  it("handles search input and triggers search", async () => {
    render(<QuickSearch />);

    await act(async () => {
      userEvent.click(screen.getByText("Search"));
    });

    // Type in the search input
    const searchInput = screen.getByPlaceholderText("Search");
    const query = "Document 1";

    userEvent.type(searchInput, query);

    vi.advanceTimersByTime(300);

    expect(fetch).toHaveBeenCalledTimes(2); //One when the dialog is opened and one when the search input is typed
    expect(fetch).toHaveBeenCalledWith(`http://localhost:8080/Search/search-full-text?query=${query}&pageIndex=1&pageSize=10`, { headers: { "Content-Type": "application/json" }, method: "GET" });

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
    render(<QuickSearch />);

    await act(async () => {
      userEvent.click(screen.getByText("Search"));
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
      } else {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve(mockSearchResults),
        });
      }
    });

    // Type in the search input
    const searchInput = screen.getByPlaceholderText("Search");
    userEvent.type(searchInput, query);

    vi.advanceTimersByTime(300);

    expect(fetch).toHaveBeenCalledWith(`http://localhost:8080/Search/search-full-text?query=${query}&pageIndex=1&pageSize=10`, { headers: { "Content-Type": "application/json" }, method: "GET" });

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

    render(<QuickSearch />);

    // Click the search button to open dialog
    userEvent.click(screen.getByText("Search"));

    // Wait for useEffect to trigger fetch after dialog opens
    await act(async () => {
      await Promise.resolve();
    });

    // Verify the toast error was called with the expected error message
    expect(toast.error).toHaveBeenCalledWith("An error occurred while fetching search results.");
  });

  // Test-8
  it("shows generic error toast when a network error occurs", async () => {
    // Reset fetch mock before this test
    vi.resetAllMocks();

    // Mock fetch to throw a network error
    global.fetch = vi.fn().mockImplementation(() => {
      throw new Error("Network error");
    });

    render(<QuickSearch />);

    // Click the search button to open dialog
    userEvent.click(screen.getByText("Search"));

    // Wait for useEffect to trigger fetch after dialog opens
    await act(async () => {
      await Promise.resolve();
    });

    // Verify the toast error was called with the generic error message
    expect(toast.error).toHaveBeenCalledWith("An error occurred.");
  });

  // Test-9
  it("closes the dialog when clicking outside", async () => {
    render(<QuickSearch />);

    // Open the dialog
    await act(async () => {
      userEvent.click(screen.getByText("Search"));
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
