import { describe, expect, test } from "vitest";
import { render, screen } from "@testing-library/react";
import ListDocuments from "@/components/list-documents";
import { SidebarProvider } from "@/context/sidebar-provider";

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
        publicationDate: "2024-01-02T12:00:00Z",
        tagRelations: null
      }
    ]
  };

  describe('ListDocuments', () => {
    test('renders documents correctly', async () => {
        render(<SidebarProvider><ListDocuments data={testData} /></SidebarProvider>);

        expect(await screen.findByText('Title')).toBeInTheDocument();
        expect(await screen.findByText('Description')).toBeInTheDocument();
        expect(await screen.findByText('Type')).toBeInTheDocument();
        // Updated column header expectations
        expect(await screen.findByText('Creation Date')).toBeInTheDocument();
        expect(await screen.findByText('Publication Date')).toBeInTheDocument();
        
        expect(await screen.findByText('Test Document')).toBeInTheDocument();
        expect(await screen.findByText('A sample test document')).toBeInTheDocument();
        expect(await screen.findByText('pdf')).toBeInTheDocument();
        
        expect(await screen.findByText('Creation Date')).toBeInTheDocument();
        expect(await screen.findByText('Publication Date')).toBeInTheDocument();
    });
});
