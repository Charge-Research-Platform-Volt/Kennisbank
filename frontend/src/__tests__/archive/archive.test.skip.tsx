import { afterEach, describe, expect, test, it, vi, beforeAll } from "vitest";
import { render, fireEvent, waitFor, act } from "@testing-library/react";
import ArchivePage from "@/app/(knowledgebank)/archive/page";
import { LeftSidebarProvider } from "@/context/left-sidebar-provider";
import { ArchiveProvider, useArchive } from "@/context/archive-provider";

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
    
  });
});

describe("Rendering fetch results", () => {
  test("renders initial documents correctly", async () => {
  });

  test("renders error message when fetch fails", async () => {
  });

  test("renders error message when search fetch fails", async () => {
  });

  test("renders different documents when search is changed", async () => {
  });

  test("renders different documents when filters are applied", async () => {
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
  });
  // The fetch response is a Promise, so we need to pass priority to the Promise queue, this can be done with this "hack" (resolving a nothing Promise)
  await Promise.resolve();
}

// Converts a datestring into a string stored inside the backend
function getCompareString(dateString: string) {
  return new Date(dateString).toLocaleString("sv-SE", { dateStyle: "short", timeStyle: "short" });
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


