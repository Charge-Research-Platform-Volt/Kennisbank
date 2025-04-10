import { afterEach, describe, expect, test, it, vi } from "vitest";
import { render, screen, fireEvent, waitFor, act } from "@testing-library/react";
import ListDocuments from "@/components/list-documents";
import ArchivePage from "@/app/(knowledgebank)/archive/page";
import fetchFiles from "@/app/(knowledgebank)/archive/page";

afterEach(() => {
  vi.clearAllMocks();
})

const testFile = {
  id: "123",
  name: "Test Document",
  description: "A sample test document",
  fileType: "pdf",
  hash: null,
  createdAt: "2024-01-01T12:00:00Z",
  updatedAt: "2024-01-02T12:00:00Z",
};

const testData = {
  message: "Success",
  pageIndex: 1,
  pageSize: 10,
  responseType: "ok",
  files: [testFile],
};

const emptyData = {
  message: "Success",
  pageIndex: 1,
  pageSize: 10,
  responseType: "ok",
  files: [],
};

function newTestFetch() { return new Response(
    JSON.stringify(testData),
    { status: 200 },
)}

function newEmptyFetch() { return new Response(
    JSON.stringify(emptyData),
    { status: 201 },
)}

function newErrorFetch() { return new Response(
    JSON.stringify({ message: "Error" }),
    { status: 500 },
)}

describe('ArchivePage', () => {
    it('renders the search component and document list', async () => {
        global.fetch = vi.fn().mockResolvedValueOnce(newEmptyFetch())

        const { container, getByText, getByRole, getByPlaceholderText } = render(<ArchivePage />)
        expect(container.querySelector(`svg[xmlns="http://www.w3.org/2000/svg"]`))
        expect(getByPlaceholderText("Search")).toBeInTheDocument()
        expect(getByRole("grid")).toBeInTheDocument()

        await waitFor(() => {
            expect(getByText('Name')).toBeInTheDocument();
            expect(getByText('Description')).toBeInTheDocument();
            expect(getByText('Type')).toBeInTheDocument();
            expect(getByText('Created At')).toBeInTheDocument();
            expect(getByText('Updated At')).toBeInTheDocument();
            }
        )
    })
})

describe("Rendering fetch results", () => {
  test('renders initial documents correctly', async () => {
      const { getByText } = render(<ListDocuments data={testData} />);

      await waitFor(() => {
          expect(getByText(testFile.name)).toBeInTheDocument();
          expect(getByText(testFile.description)).toBeInTheDocument();
          expect(getByText(testFile.fileType)).toBeInTheDocument();
          expect(getByText(getCompareString(testFile.createdAt))).toBeInTheDocument();
          expect(getByText(getCompareString(testFile.updatedAt))).toBeInTheDocument();
      })
  });

  test('renders error message when fetch fails', async () => {
      global.fetch = vi.fn().mockResolvedValue(newErrorFetch())

      const { getByText } = render(<ArchivePage />);

      await waitFor(() => {
          expect(getByText("An error occurred while fetching initial files.")).toBeInTheDocument();
      })
  })

  test("renders initial, then search finds nothing and finally search finds something", async () => {
    // Simulate the initial fetch
    global.fetch = vi.fn().mockResolvedValueOnce(newTestFetch())

    const { getByText } = render(<ArchivePage />);
    const searchInput = screen.getByPlaceholderText("Search");

    // Use await for because we need to be sure only the current results are present
    // So that expect isn't bleeding into other states
    await waitFor(async () => {
      expect(fetch).toHaveResolved();
    });

    expect(getByText(testFile.name)).toBeInTheDocument();

    // Simulate a search with no results
    global.fetch = vi.fn().mockResolvedValueOnce(newEmptyFetch())

    await act(async () => {
      vi.useFakeTimers();
      fireEvent.change(searchInput, { target: { value: "nothing" } });
      vi.advanceTimersByTime(300);
      vi.useRealTimers();
    });

    await waitFor(async () => {
      expect(fetch).toHaveResolved();
    });

    expect(getByText("No Rows To Show")).toBeInTheDocument();

    // Simulate a search with results
    global.fetch = vi.fn().mockResolvedValueOnce(newTestFetch())

    await act(async () => {
      vi.useFakeTimers();
      fireEvent.change(searchInput, { target: { value: "test" } });
      vi.advanceTimersByTime(300);
      vi.useRealTimers();
    });

    await waitFor(async () => {
      expect(fetch).toHaveResolved();
    });

    expect(getByText(testFile.name)).toBeInTheDocument();
  });

  test('renders error message when search fetch fails', async () => {
      global.fetch = vi.fn().mockResolvedValueOnce(newEmptyFetch())

      const { getByText } = render(<ArchivePage />);

      await waitFor(async() => {
        expect(fetch).toHaveResolved();
      })

      global.fetch = vi.fn().mockResolvedValueOnce(newErrorFetch())

      const searchInput = screen.getByPlaceholderText("Search")

      await act(async () => {
        vi.useFakeTimers();
        fireEvent.change(searchInput, { target: { value: "error" } });
        vi.advanceTimersByTime(300);
        vi.useRealTimers();
      });

      await waitFor(async() => {
        expect(fetch).toHaveResolved();
      })

      expect(getByText("An error occurred while fetching search results.")).toBeInTheDocument();
  })
});

function getCompareString(dateString: string) {
  return new Date(dateString).toLocaleString("sv-SE", { dateStyle: "short", timeStyle: "short" });
}
