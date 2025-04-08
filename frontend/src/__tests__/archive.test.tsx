import { describe, expect, test } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import ListDocuments from "@/components/list-documents";
import { SidebarProvider } from "@/context/sidebar-provider";

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
      tags: [],
    },
  ],
};

describe("ListDocuments", () => {
  test("renders documents correctly", async () => {
    render(
      <SidebarProvider>
        <ListDocuments data={testData} />
      </SidebarProvider>,
    );

    expect(await screen.findByText("Name")).toBeInTheDocument();
    expect(await screen.findByText("Description")).toBeInTheDocument();
    expect(await screen.findByText("Type")).toBeInTheDocument();
    expect(await screen.findByText("Created At")).toBeInTheDocument();
    expect(await screen.findByText("Updated At")).toBeInTheDocument();

    expect(await screen.findByText("Test Document")).toBeInTheDocument();
    expect(await screen.findByText("A sample test document")).toBeInTheDocument();
    expect(await screen.findByText("pdf")).toBeInTheDocument();
    expect(await screen.findByText("2024-01-01 12:00")).toBeInTheDocument();
  });
});
