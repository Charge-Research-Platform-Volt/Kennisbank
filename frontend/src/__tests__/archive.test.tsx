import { afterEach, describe, expect, test, it, vi, beforeAll } from "vitest";
import { render, fireEvent, waitFor, act } from "@testing-library/react";
import ArchivePage from "@/app/(knowledgebank)/archive/page";
import { SidebarProvider } from "@/context/sidebar-provider";

const filterButtonApplyMock = vi.fn();

beforeAll(() => {
  vi.mock("@/app/(knowledgebank)/archive/components/filter-button", () => ({
    default: (children: { onApplyAction: (tagFilters: string[], startDate: number, endDate: number) => void }) => {
      filterButtonApplyMock.mockImplementation(children.onApplyAction);
      return <>Filter Button Mock</>;
    },
  }));
});

afterEach(() => {
  vi.clearAllMocks();
});

const testFile = newFile("Test Document Title");

function newFile(name: string, publicationDate: string = "2024-01-02T12:00:00Z") {
  return {
    id: "123",
    title: name,
    description: "A sample test document",
    fileType: "pdf",
    hash: null,
    typeId: "type1",
    languageCode: "en",
    publicationCode: null,
    license: null,
    note: null,
    creationDate: "2024-01-01T12:00:00Z",
    publicationDate: publicationDate,
  };
}

function newData(data: unknown) {
  return {
    message: "Success",
    pageIndex: 1,
    pageSize: 10,
    responseType: "ok",
    resources: data,
  };
}

function newDataResponsePromise(data: Array<unknown>) {
  return Promise.resolve(new Response(JSON.stringify(newData(data)), { status: 200 }));
}

function newErrorResponsePromise() {
  return Promise.resolve(new Response(JSON.stringify({ message: "Error" }), { status: 500 }));
}

describe("ArchivePage", () => {
  it("renders the search component and document list", async () => {
    global.fetch = vi.fn().mockResolvedValueOnce(newDataResponsePromise([]));

    const { container, getByText, getByRole, getByPlaceholderText } = render(<SidebarProvider leftSidebarDefaultState={true}><ArchivePage /></SidebarProvider>);
    expect(container.querySelector(`svg[xmlns="http://www.w3.org/2000/svg"]`)).toBeInTheDocument();
    expect(getByPlaceholderText("Search")).toBeInTheDocument();
    expect(getByText("Filter Button Mock")).toBeInTheDocument();
    expect(getByRole("grid")).toBeInTheDocument();

    await waitFor(() => {
      expect(getByText("Title")).toBeInTheDocument();
      expect(getByText("Description")).toBeInTheDocument();
      expect(getByText("Type")).toBeInTheDocument();
      expect(getByText("Creation Date")).toBeInTheDocument();
      expect(getByText("Publication Date")).toBeInTheDocument();
    });
  });
});

describe("Rendering fetch results", () => {
  test("renders initial documents correctly", async () => {
    global.fetch = vi.fn(() => (newDataResponsePromise([testFile])));

    const { getByText } = render(<SidebarProvider leftSidebarDefaultState={true}><ArchivePage /></SidebarProvider>);

    await awaitFetchResolve(1);

    expect(getByText(testFile.title)).toBeInTheDocument();
    expect(getByText(testFile.description)).toBeInTheDocument();
    expect(getByText(testFile.fileType)).toBeInTheDocument();
    expect(getByText(getCompareString(testFile.creationDate))).toBeInTheDocument();
    expect(getByText(getCompareString(testFile.publicationDate))).toBeInTheDocument();
  });

  test("renders error message when fetch fails", async () => {
    global.fetch = vi.fn(() => (newErrorResponsePromise()));

    const { getByText } = render(<SidebarProvider leftSidebarDefaultState={true}><ArchivePage /></SidebarProvider>);

    await awaitFetchResolve(1);

    expect(getByText("An error occurred while fetching initial files.")).toBeInTheDocument();
  });

  test("renders error message when search fetch fails", async () => {
    global.fetch = vi.fn(() => (newErrorResponsePromise()));

    const { getByText, getByPlaceholderText } = render(<SidebarProvider leftSidebarDefaultState={true}><ArchivePage /></SidebarProvider>);

    const searchInput = getByPlaceholderText("Search");

    await awaitDebouncedChange(() => fireEvent.change(searchInput, { target: { value: "error" } }));

    await waitFor(() => expect(getByText("An error occurred while fetching search results.")).toBeInTheDocument());
  });

  test("renders different documents when search is changed", async () => {
    const files = [testFile];

    // Simulate the initial fetch
    global.fetch = vi.fn(async (input: unknown) => {
      if (typeof input === "string") {
        if (input.includes("list-all")) {
          return newDataResponsePromise([testFile]);
        } else if (input.includes("search-full-text")) {
          return newDataResponsePromise(files.filter((file) => file.title.toLowerCase().includes(new URL(input).searchParams.get("query")!.toLowerCase())));
        }
      }
      throw new Error("Unexpected fetch call");
    });

    const { getByText, getByPlaceholderText } = render(<SidebarProvider leftSidebarDefaultState={true}><ArchivePage /></SidebarProvider>);

    const searchInput = getByPlaceholderText("Search");

    await awaitFetchResolve(1);

    expect(getByText(testFile.title)).toBeInTheDocument();

    await awaitDebouncedChange(() => fireEvent.change(searchInput, { target: { value: "nothing" } }), 2);

    expect(getByText("No Rows To Show")).toBeInTheDocument();

    await awaitDebouncedChange(() => fireEvent.change(searchInput, { target: { value: "test" } }), 3);

    expect(getByText(testFile.title)).toBeInTheDocument();
  });

  test("renders different documents when filters are applied", async () => {
    // Define files all with different properties
    const title1 = testFile.title;
    const title2 = "Tagged Document 1";
    const title3 = "Tagged Document 2";
    const title4 = "Tagged Document 3";
    const files = [testFile, newFile("Tagged Document 1"), newFile("Tagged Document 2", "2028-01-02T12:00:00Z"), newFile("Tagged Document 3", "2026-01-02T12:00:00Z")];

    // Set up the fetch mock, that will return documents based on filters
    // Since our documents themselves dont store tags, just treat "Tagged" in the title as having every tag
    global.fetch = vi.fn((input: unknown, init: RequestInit | undefined) => {
      if (typeof input === "string") {
        if (input.includes("list-all")) {
          return newDataResponsePromise(files);
        } else if (input.includes("search-full-text")) {
          console.log(init!.body!);
          const filter = JSON.parse(init!.body! as string);
          return newDataResponsePromise(
              files.filter(
                (file) =>
                  file.title.toLowerCase().includes(new URL(input).searchParams.get("query")!.toLowerCase()) &&
                  (filter.tagFilters.length > 0 ? file.title.toLowerCase().includes("tagged") : true) &&
                  (filter.startDate ? new Date(file.publicationDate) >= new Date(filter.startDate) : true) &&
                  (filter.endDate ? new Date(file.publicationDate) <= new Date(filter.endDate) : true),
              ),
            )
        }
      }
      throw new Error("Unexpected fetch call");
    });

    const { getByText, queryByText } = render(<SidebarProvider leftSidebarDefaultState={true}><ArchivePage /></SidebarProvider>);

    await awaitFetchResolve(1);

    expect(getByText(title1)).toBeInTheDocument();
    expect(getByText(title2)).toBeInTheDocument();
    expect(getByText(title3)).toBeInTheDocument();
    expect(getByText(title4)).toBeInTheDocument();

    await awaitDebouncedChange(() => filterButtonApplyMock(["tag1"], null, null), 2);

    await waitFor(() => expect(queryByText(title1)).not.toBeInTheDocument());
    expect(getByText(title2)).toBeInTheDocument();
    expect(getByText(title3)).toBeInTheDocument();
    expect(getByText(title4)).toBeInTheDocument();

    await awaitDebouncedChange(() => filterButtonApplyMock(["tag1"], 2025, null), 3);

    await waitFor(() => expect(queryByText(title1)).not.toBeInTheDocument());
    await waitFor(() => expect(queryByText(title2)).not.toBeInTheDocument());
    expect(getByText(title3)).toBeInTheDocument();
    expect(getByText(title4)).toBeInTheDocument();

    await awaitDebouncedChange(() => filterButtonApplyMock(["tag1"], 2025, 2027), 4);

    await waitFor(() => expect(queryByText(title1)).not.toBeInTheDocument());
    await waitFor(() => expect(queryByText(title2)).not.toBeInTheDocument());
    await waitFor(() => expect(queryByText(title3)).not.toBeInTheDocument());
    expect(getByText(title4)).toBeInTheDocument();
  });
});

// Make a change that has to be "debounced", aka time has to pass since the last change before the change is confirmed
async function awaitDebouncedChange(action: () => void, fetchTimes: number = -1) {
    await act(async () => {
    vi.useFakeTimers();
    action();
    // Although 300 somewhat a magic number here, its based on the debounce timer set on the archive page
    vi.advanceTimersByTime(300);
    vi.useRealTimers();
  });
  await awaitFetchResolve(fetchTimes);
}

// Wait for the fetch to resolve, and if given, the nth resolve, then let the Promise queue resolve
async function awaitFetchResolve(times: number = -1) {
  await waitFor(async () => {
    if (times === -1) {
      expect(fetch).toHaveResolved();
    } else {
      expect(fetch).toHaveResolvedTimes(times);
    }
    // The fetch response is a Promise, so we need to pass priority to the Promise queue, this can be done with this "hack" (resolving a nothing Promise)
    await Promise.resolve();
  });
}

// Converts a datestring into a string stored inside the backend
function getCompareString(dateString: string) {
  return new Date(dateString).toLocaleString("sv-SE", { dateStyle: "short", timeStyle: "short" });
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


