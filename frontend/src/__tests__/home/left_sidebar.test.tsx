import { describe, expect, test, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import LeftSidebarClient from "@/components/sidebar/left-sidebar/left-sidebar-client";
import { TagArraySchema } from "@/types/tag.type";
import { QuickSearchProvider } from "@/context/quick-search-provider";
import Home from "@/icons/home";
import Archive from "@/icons/archive";
import Tags from "@/icons/tags-icon";
import { SidebarItem } from "@/types/sidebar";
import ProjectIcon1 from "@/icons/project-icons/icon-1";
import Help from "@/icons/help";
import Settings from "@/icons/settings";
import { SidebarProvider } from "@/context/sidebar-provider";

const testTags = TagArraySchema.parse([]);

const expectedComponents: string[] = ["Home", "Tags", "Archive", "Project 1", "Settings", "Help"].sort();
const menuItems: SidebarItem[] = [
  { id: 1, name: "Home", path: "/", icon: <Home className="h-4 w-4" /> },
  { id: 2, name: "Archive", path: "/archive", icon: <Archive className="h-4 w-4" /> },
  { id: 3, name: "Tags", path: "/tags", icon: <Tags className="h-4 w-4" /> },
];
const projects: SidebarItem[] = [{ id: 1, name: "Project 1", path: "/", icon: <ProjectIcon1 className="h-4 w-4" /> }];
const bottomMenuItems: SidebarItem[] = [
  { id: 1, name: "Settings", path: "/users", icon: <Settings className="h-4 w-4" /> },
  { id: 1, name: "Help", path: "http://localhost:3001/guide", icon: <Help className="h-4 w-4" /> },
];
const expectedHiddenomponents: string[] = ["Home", "Tags", "Archive", "Project 1", "Settings", "Help"].sort();

const username = "Testuser";
const userEmail = "testuser@mail.nl";

vi.mock("next/navigation", () => ({
  useRouter: () => ({
    push: vi.fn(),
  }),
  usePathname: () => "/",
}));

describe("LeftSidebar", () => {
  test("Left_Sidebar renders all components with correct names", async () => {
    render(
      <SidebarProvider leftSidebarDefaultState={true}>
        <QuickSearchProvider>
          <LeftSidebarClient menuItems={menuItems} projects={projects} bottomMenuItems={bottomMenuItems} tags={testTags} userEmail={userEmail} userRole={username} />
        </QuickSearchProvider>
      </SidebarProvider>,
    );
    const componentNames = await screen
      .getAllByTestId("sidebar")
      .map((elem) => elem.textContent ?? "")
      .sort();
    expect(componentNames).toEqual(expectedComponents);
    const foldedComponentNames = await screen
      .queryAllByTestId("hiddensidebar")
      .map((elem) => elem.textContent ?? "")
      .sort();
    expect(foldedComponentNames).toEqual([]);
    expect(screen.getByText((content) => content.includes(username))).toBeInTheDocument();
    expect(screen.getByText((content) => content.includes(userEmail))).toBeInTheDocument();
  });
});

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
