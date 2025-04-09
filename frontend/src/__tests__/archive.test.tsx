import { describe, expect, test, it, vi } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import ListDocuments from '@/components/list-documents';
import ArchivePage from '@/app/(knowledgebank)/archive/page';
import fetchFiles from '@/app/(knowledgebank)/archive/page';

const testFile = {
    id: "123",
    name: "Test Document",
    description: "A sample test document",
    fileType: "pdf",
    hash: null,
    createdAt: "2024-01-01T12:00:00Z",
    updatedAt: "2024-01-02T12:00:00Z",
}

const testData = {
    message: "Success",
    pageIndex: 1,
    pageSize: 10,
    responseType: "ok",
    files: [
        testFile,
    ],
};

const emptyData = {
    message: "Success",
    pageIndex: 1,
    pageSize: 10,
    responseType: "ok",
    files: [],
}

const testFetch = {
    ok: true,
    json() { return testData },
}

const emptyFetch = {
    ok: true,
    json() { return emptyData },
}

const errorFetch = {
    ok: false,
}

describe('ArchivePage', () => {
    it('renders the search component and document list', async () => {
        global.fetch = vi.fn().mockResolvedValue(emptyFetch)

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

describe('Rendering fetch results', () => {
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
        global.fetch = vi.fn().mockResolvedValue(errorFetch)

        const { getByText } = render(<ArchivePage />);

        await waitFor(() => {
            expect(getByText("An error occurred while fetching initial files.")).toBeInTheDocument();
        })
    })

    test('renders initial, then search finds nothing and finally search finds something', async () => {
        // Render initial documents
        global.fetch = vi.fn().mockResolvedValueOnce(testFetch)

        const { getByText, queryByText } = render(<ArchivePage />);
        const searchInput = screen.getByPlaceholderText("Search")

        // Use await for because we need to be sure only the current results are present
        // So that expect isn't bleeding into other states
        await waitFor(() => {
            expect(fetch).toHaveResolvedWith({testFetch});
        })

        fireEvent.change(searchInput, { target: { value: "nothing" } })

        await waitFor(() => {
            expect(fetch).toHaveResolvedTimes(2);
        })
        expect(queryByText(testFile.name)).not.toBeInTheDocument();

        // Simulate a search that finds the initial document again
        global.fetch = vi.fn().mockResolvedValueOnce({
            ok: true,
            json() { return testData },
        })

        fireEvent.change(searchInput, { target: { value: "test" } })

        await waitFor(() => {
            expect(fetch).toHaveResolvedTimes(3);
        })
        expect(getByText(testFile.name)).toBeInTheDocument();
    })

    test('renders error message when search fetch fails', async () => {
        global.fetch = vi.fn().mockResolvedValueOnce({
            ok: true,
            json() { return emptyData },
        })

        const { getByText, queryByText } = render(<ArchivePage />);

        global.fetch = vi.fn().mockResolvedValueOnce({
            ok: false,
        })

        const searchInput = screen.getByPlaceholderText("Search")
        fireEvent.change(searchInput, { target: { value: "test" } })

        await waitFor(() => {
            expect(getByText("An error occurred while fetching search results.")).toBeInTheDocument();
        })
    })
});

function getCompareString(dateString: string) {
    return new Date(dateString).toLocaleString("sv-SE", { dateStyle: "short", timeStyle: "short" });
}

