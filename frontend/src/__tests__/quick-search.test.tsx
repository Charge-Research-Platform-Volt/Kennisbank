import React from "react";
import "@testing-library/jest-dom";
import { vi, describe, it, expect, beforeEach, afterEach } from "vitest";
import { render, screen, fireEvent, act, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";

import { toast } from "sonner";

// Component to test
import QuickSearch from "@/components/quick-search";

// Mock dependencies
vi.mock("sonner", () => ({
  toast: {
    error: vi.fn(),
  },
}));

vi.mock("next/link", () => ({
  default: ({ children, href, ...props }: any) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
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
      name: "Document 2",
      id: "doc2",
      description: "",
      hash: null,
      fileType: "docx",
      createdAt: "2025-02-01",
      updatedAt: "2025-02-02",
    },
  ],
};

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
        (window as any).hotkeyCallback = callback;
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
    await userEvent.click(searchButton);

    expect(screen.getByPlaceholderText("Search")).toBeInTheDocument();
    expect(screen.getByText("No results found.")).toBeInTheDocument();
  });

  // Test-3
  it("opens the dialog when Cmd+K shortcut is used", async () => {
    render(<QuickSearch />);

    // Trigger the hotkey callback directly
    act(() => {
      (window as any).hotkeyCallback();
    });

    expect(screen.getByPlaceholderText("Search")).toBeInTheDocument();
  });

  //Test-4
  it("fetches search results when dialog is opened", async () => {
    render(<QuickSearch />);

    // Open the dialog
    const searchButton = screen.getByText("Search");
    userEvent.click(searchButton);

    expect(fetch).toHaveBeenCalledTimes(1);
    expect(fetch).toHaveBeenCalledWith("http://localhost:8080/Search/search-full-text?pageIndex=1&pageSize=10", { headers: { "Content-Type": "application/json" }, method: "GET" });

    // Advance timers to resolve any pending promises
    await act(async () => {
      await Promise.resolve();
    });

    // Now check if the results are displayed
    expect(screen.getByText("Document 1")).toBeInTheDocument();
    expect(screen.getByText("Document 2")).toBeInTheDocument();

    // Test that document description is properly displayed
    expect(screen.getByText("Test description")).toBeInTheDocument();

    // Test the links are properly generated
    const links = screen.getAllByRole("link");
    expect(links[0]).toHaveAttribute("href", "/file/doc1");
    expect(links[1]).toHaveAttribute("href", "/file/doc2");
  }, 10000);

  // Test-5
  it("handles search input and triggers search", async () => {
    render(<QuickSearch />);

    await userEvent.click(screen.getByText("Search"));

    // Type in the search input
    const searchInput = screen.getByPlaceholderText("Search");
    const query = "Document 1";
    await userEvent.type(searchInput, query);

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

    // Check that Document 2 is no longer in the document
    expect(screen.queryByText("Document 2")).not.toBeInTheDocument;

    // Check that the "No results found" message is not displayed
    expect(screen.queryByText("No results found.")).not.toBeInTheDocument;
  }, 10000);

  // Test-6
  it("unavailable search results", async () => {
    render(<QuickSearch />);

    await userEvent.click(screen.getByText("Search"));

    // Type in the search input
    const searchInput = screen.getByPlaceholderText("Search");
    const query = "wefwefwopifwef09iqfopm09uf028r02394jfpo2jfp9023ur";
    await userEvent.type(searchInput, query);

    vi.advanceTimersByTime(300);

    expect(fetch).toHaveBeenCalledTimes(2); //One when the dialog is opened and one when the search input is typed
    expect(fetch).toHaveBeenCalledWith(`http://localhost:8080/Search/search-full-text?query=${query}&pageIndex=1&pageSize=10`, { headers: { "Content-Type": "application/json" }, method: "GET" });

    await act(async () => {
      await Promise.resolve();
    });

    // Check that Document 1 is no longer in the document
    expect(screen.queryByText("Document 1")).not.toBeInTheDocument;

    // Verify the correct description is still shown
    expect(screen.queryByText("Test description")).not.toBeInTheDocument;

    // Check that Document 2 is no longer in the document
    expect(screen.queryByText("Document 2")).not.toBeInTheDocument;
  }, 10000);
});
