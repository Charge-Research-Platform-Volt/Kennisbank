import type React from "react";

// Testing library
import "@testing-library/jest-dom";
import { vi, describe, it, expect, beforeEach } from "vitest";
import { render, screen } from "@testing-library/react";
import RightSidebar, { NoDocumentSelected } from "@/components/sidebar/right-sidebar/right-sidebar";
import { useSidebar } from "@/context/sidebar-provider";
import { fireEvent } from "@testing-library/react";

// Mock the hooks and components
vi.mock("@/context/sidebar-provider", () => ({
  useSidebar: vi.fn(),
}));

describe("RightSidebar", () => {
  const mockToggleRightSidebar = vi.fn();

  const mockTagRelation1 = {
    resourceId: "123",
    tagId: "456",
    isApproved: false,
    approvedOn: null,
    approvedBy: null,
    tag: {
      id: "456",
      name: "Tag 1",
      isStandardized: false,
      isApproved: false,
      approvedOn: null,
      approvedBy: null,
      createdBy: "User1",
      createdOn: "2024-01-01T12:00:00Z"
    }
  }

  const mockTagRelation2 = {
    resourceId: "123",
    tagId: "789",
    isApproved: false,
    approvedOn: null,
    approvedBy: null,
    tag: {
      id: "789",
      name: "Tag 2",
      isStandardized: false,
      isApproved: false,
      approvedOn: null,
      approvedBy: null,
      createdBy: "User1",
      createdOn: "2024-01-01T12:00:00Z"
    }
  }

  const mockDocument = {
    id: "123",
    title: "Test Document",
    description: "This is a test document",
    fileType: "pdf",
    hash: null,
    typeId: "type1",
    languageCode: "en",
    publicationCode: null,
    license: null,
    note: null,
    creationDate: "2024-01-01T12:00:00Z",
    publicationDate: "2024-01-02T12:00:00Z",
    tagRelations: [
      mockTagRelation1,
      mockTagRelation2
    ],
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders NoDocumentSelected when no document is selected", () => {
    (useSidebar as ReturnType<typeof vi.fn>).mockReturnValue({
      selectedDocument: null,
      toggleRightSidebar: mockToggleRightSidebar,
    });

    render(<RightSidebar />);

    expect(screen.getByText("No Document Selected")).toBeInTheDocument();
    expect(screen.getByText("Please select a document to view details.")).toBeInTheDocument();
  });

  it("renders document details when document is selected", () => {
    (useSidebar as ReturnType<typeof vi.fn>).mockReturnValue({
      selectedDocument: mockDocument,
      toggleRightSidebar: mockToggleRightSidebar,
    });

    render(<RightSidebar />);

    // Check if document details are rendered
    expect(screen.getByText("Test Document")).toBeInTheDocument();
    expect(screen.getByText("This is a test document")).toBeInTheDocument();
    expect(screen.getByText("Tag 1")).toBeInTheDocument();
    expect(screen.getByText("Tag 2")).toBeInTheDocument();

    // Check date formatting
    expect(screen.getByText(/Created At:/)).toBeInTheDocument();
  });

  it("renders document without tags when tags are empty", () => {
    const documentWithoutTags = { ...mockDocument, tags: [] };

    (useSidebar as ReturnType<typeof vi.fn>).mockReturnValue({
      selectedDocument: documentWithoutTags,
      toggleRightSidebar: mockToggleRightSidebar,
    });

    render(<RightSidebar />);

    // Tags section should not be rendered
    expect(screen.queryByTestId("divider-tags")).not.toBeInTheDocument();
  });

  it("renders document without description when description is empty", () => {
    const documentWithoutDescription = { ...mockDocument, description: "" };

    (useSidebar as ReturnType<typeof vi.fn>).mockReturnValue({
      selectedDocument: documentWithoutDescription,
      toggleRightSidebar: mockToggleRightSidebar,
    });

    render(<RightSidebar />);

    // Description should not be rendered
    expect(screen.queryByText("This is a test document")).not.toBeInTheDocument();
  });

  it("calls toggleRightSidebar when close button is clicked", () => {
    (useSidebar as ReturnType<typeof vi.fn>).mockReturnValue({
      selectedDocument: mockDocument,
      toggleRightSidebar: mockToggleRightSidebar,
    });

    render(<RightSidebar />);

    // Click close button
    fireEvent.click(screen.getByText("Close"));

    expect(mockToggleRightSidebar).toHaveBeenCalledWith(null);
  });

  it("renders NoDocumentSelected component correctly", () => {
    render(<NoDocumentSelected />);

    expect(screen.getByText("No Document Selected")).toBeInTheDocument();
    expect(screen.getByText("Please select a document to view details.")).toBeInTheDocument();
  });
});


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


