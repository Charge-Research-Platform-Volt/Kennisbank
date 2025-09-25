import type React from "react";

// Testing library
import "@testing-library/jest-dom";
import { vi, describe, it, expect, beforeEach } from "vitest";
import { render, screen } from "@testing-library/react";
import RightSidebar  from "@/components/sidebars/right-sidebar/right-sidebar";
import { useSidebar, MetadataTypeEnum } from "@/context/sidebar-provider";
import { useArchive } from "@/context/archive-provider";
import { fireEvent } from "@testing-library/react";
import { getProperties, getRelation, addRelation, newRelationSearchResults } from "@/actions/right-sidebarActions";


// Use partial mocking to keep the real enum while mocking the hook
vi.mock("@/context/sidebar-provider", async (importOriginal) => {
  const actual: object = await importOriginal();
  return {
    ...actual,
    useSidebar: vi.fn(),
    SidebarProvider: ({ children }: { children: React.ReactNode }) => children
  };
});

vi.mock("@/context/archive-provider", async (importOriginal) => {
  const actual: object = await importOriginal();
  return {
    ...actual,
    useArchive: vi.fn()
  };
});


vi.mock("@/actions/right-sidebarActions", () => ({
  getProperties: vi.fn(),
  getRelation: vi.fn(),
}));


describe("RightSidebar", () => {
  const mockToggleRightSidebar = vi.fn();
  const mockIsEmptyPrevs = vi.fn();
  const mockIsEmptyNexts = vi.fn();
  const mockSetPublicationDate = vi.fn();
  const mockSetCreationDate = vi.fn();
  const mockCurrentId = "0123456789";
  const mockCurrentTypeResource = MetadataTypeEnum.RESOURCE;
  const mockCurrentTypeOrganisation = MetadataTypeEnum.ORGANISATION;
  const mockCurrentTypePerson = MetadataTypeEnum.PERSON;
  const mockPublicationDate = new Date();
  const mockCreationDate = new Date();
  const mockTagList = [
    {id: "1", name: "tag1", type: "tag"}, 
    {id: "2", name: "tag2", type: "tag"}, 
    {id: "3", name: "tag3", type: "tag"}
  ];
  const mockUrl = "https://google.com";
  const mockDescription = "this is a mock description";
  const mockTitle = "Mock Resource Title";
  const mockNote = "These are mock notes";


   // Define a reusable mock for getRelation
  const setupMockGetRelation = (id: string) => {
    const relationResponses = {
      "website": {
        body: { url: mockUrl }
      },
      "author": {
        body: [
          { id: "a1", name: "Author 1" },
          { id: "a2", name: "Author 2" }
        ]
      },
      "tag": {
        body: [
          { id: "1", name: "tag1" },
          { id: "2", name: "tag2" },
          { id: "3", name: "tag3" }
        ]
      },
      "organisation": {
        body: [{ id: "o1", name: "Organisation 1" }]
      },
      "relatedPerson": {
        body: [{ id: "p1", name: "Person 1" }]
      },
      "relatedOrganisation": {
        body: [{ id: "o2", name: "Organisation 2" }]
      },
      "source": {
        body: [
          { url: "https://source1.com", resourceid: "s1" },
          { url: "https://source2.com", resourceid: "s2" }
        ]
      },
      "resource-related-resources": {
        body: [
          { id:"id1", title:"MockRelatedResource1", fileType:"pdf"}, 
          { id:"id2", title:"MockRelatedResource2", fileType:"pdf"}
        ]
      }
    };

    (getRelation as ReturnType<typeof vi.fn>).mockImplementation((id, type, relationType) => {
      return Promise.resolve(relationResponses[relationType as keyof typeof relationResponses] || { body: [] });
    });
  };



  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders website correctly with data", async () => {
    // Mock the useSidebar hook
    (useSidebar as ReturnType<typeof vi.fn>).mockReturnValue({
      currentId: mockCurrentId,
      currentType: mockCurrentTypeResource,
      isEmptyPrevs: mockIsEmptyPrevs,
      isEmptyNexts: mockIsEmptyNexts,
      toggleRightSidebar: mockToggleRightSidebar,
      setPublicationDate: mockSetPublicationDate,
      setCreationDate: mockSetCreationDate,
      rightSidebarOpen: true,

    });

    (useArchive as ReturnType<typeof vi.fn>).mockReturnValue({
      tagFilters: [],
    });

    // Set up mock API responses
    (getProperties as ReturnType<typeof vi.fn>).mockResolvedValue({
      body: {
        fileType: "website",
        title: mockTitle,
        description: mockDescription,
        note: mockNote,
        publicationCode: null,
        license: null,
        creationDate: mockCreationDate,
        publicationDate: mockPublicationDate,
        languageCode: "TestCode"
      }
    });
    
    setupMockGetRelation(mockCurrentId);

    render(<RightSidebar />);

    await vi.waitFor(() => {
      expect(screen.getByText(mockTitle)).toBeInTheDocument();
    });

    
    expect(screen.getByText(mockUrl)).toBeInTheDocument();
    expect(screen.getByText("Description")).toBeInTheDocument();
    expect(screen.getByText(mockDescription)).toBeInTheDocument();
    expect(screen.getByText("Tags")).toBeInTheDocument();
    expect(screen.getByText("Authors")).toBeInTheDocument();
    expect(screen.getByText("Organisations")).toBeInTheDocument();
    expect(screen.getByText("Related People")).toBeInTheDocument();
    expect(screen.getByText("Related Organisations")).toBeInTheDocument();
    expect(screen.getByText("Related Resources")).toBeInTheDocument();
    expect(screen.getByText("Sources")).toBeInTheDocument();
    expect(screen.getByText("Notes")).toBeInTheDocument();
    expect(screen.getByText(mockNote)).toBeInTheDocument();

  })

  it("renders person correctly", () => {
    (useSidebar as ReturnType<typeof vi.fn>).mockReturnValue({
      currentId: mockCurrentId,
      currentType: mockCurrentTypePerson,
      toggleRightSidebar: mockToggleRightSidebar,
      isEmptyPrevs: mockIsEmptyPrevs,
      isEmptyNexts: mockIsEmptyNexts,
      setPublicationDate: mockSetPublicationDate,
    });

    render(<RightSidebar />);

    expect(screen.getByText("Description")).toBeInTheDocument();
  })

});


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


