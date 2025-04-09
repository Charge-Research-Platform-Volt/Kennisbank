import { describe, expect, test, it, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import ListDocuments from '@/components/list-documents';
import ArchivePage from '@/app/(knowledgebank)/archive/page';
import fetchFiles from '@/app/(knowledgebank)/archive/page';

const testData = {
    message: "Success",
    pageIndex: 1,
    pageSize: 10,
    responseType: "ok",
    files: [
        {
            id: "123",
            name: "Test Document",
            description: "A sample test document",
            fileType: "pdf",
            hash: null,
            createdAt: "2024-01-01T12:00:00Z",
            updatedAt: "2024-01-02T12:00:00Z",
        }
    ]
};

describe('ArchivePage', () => {
    it('renders the search component and document list', async () => {

        global.fetch = vi.fn().mockResolvedValue(
            {
                ok: true,
                json() { return testData },
            }
        )

        const { container, getByPlaceholderText } = render(<ArchivePage />)
        expect(container.querySelector(`svg[xmlns="http://www.w3.org/2000/svg"]`))
        expect(getByPlaceholderText("Search")).toBeInTheDocument()
        expect(getByPlaceholderText("nope")).toBeInTheDocument()
    })
})

// describe('ListDocuments', () => {
//     test('renders documents correctly', async () => {
//         render(<ListDocuments data={testData} />);

//         expect(await screen.findByText('Name')).toBeInTheDocument();
//         expect(await screen.findByText('Description')).toBeInTheDocument();
//         expect(await screen.findByText('Type')).toBeInTheDocument();
//         expect(await screen.findByText('Created At')).toBeInTheDocument();
//         expect(await screen.findByText('Updated At')).toBeInTheDocument();
        
//         expect(await screen.findByText('Test Document')).toBeInTheDocument();
//         expect(await screen.findByText('A sample test document')).toBeInTheDocument();
//         expect(await screen.findByText('pdf')).toBeInTheDocument();
//         expect(await screen.findByText('2024-01-01 12:00')).toBeInTheDocument();
//     });
// });

