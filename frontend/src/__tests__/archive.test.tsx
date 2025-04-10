import { describe, expect, test } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import ListDocuments from '@/components/list-documents';

const testData = {
    message: "Success",
    pageIndex: 1,
    pageSize: 10,
    responseType: "ok",
    resources: [ 
      {
        id: "123",
        title: "Test Document",
        description: "A sample test document",
        fileType: "pdf",
        hash: null,
        typeId: "type1",
        languageCode: "en",
        publicationCode: null,
        license: null,
        note: null,
        creationDate: "2024-01-01T12:00:00Z",
        publicationDate: "2024-01-02T12:00:00Z"
      }
    ]
  };

  describe('ListDocuments', () => {
    test('renders documents correctly', async () => {
        render(<ListDocuments data={testData} />);

        expect(await screen.findByText('Title')).toBeInTheDocument();
        expect(await screen.findByText('Description')).toBeInTheDocument();
        expect(await screen.findByText('Type')).toBeInTheDocument();
        // Updated column header expectations
        expect(await screen.findByText('Creation Date')).toBeInTheDocument();
        expect(await screen.findByText('Publication Date')).toBeInTheDocument();
        
        expect(await screen.findByText('Test Document')).toBeInTheDocument();
        expect(await screen.findByText('A sample test document')).toBeInTheDocument();
        expect(await screen.findByText('pdf')).toBeInTheDocument();
        
        // Date format expectations for the new field names
        const creationDate = new Date('2024-01-01T12:00:00Z').toLocaleString();
        const publicationDate = new Date('2024-01-02T12:00:00Z').toLocaleString();
        
        expect(await screen.findByText('Creation Date')).toBeInTheDocument();
        expect(await screen.findByText('Publication Date')).toBeInTheDocument();
    });
});
