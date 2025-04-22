import { describe, expect, test, vi } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import LeftSidebarClient from '@/components/sidebar/left-sidebar/left-sidebar-client'
import { TagArraySchema } from '@/types/tag.type'
import { QuickSearchProvider } from '@/components/quick-search-context'
import Home from '@/icons/home'
import Archive from '@/icons/archive'
import Tags from '@/icons/tags-icon'
import { SidebarItem } from '@/types/sidebar'
import ProjectIcon1 from '@/icons/project-icons/icon-1'
import Help from '@/icons/help'
import Settings from '@/icons/settings'

const testTags = TagArraySchema.parse([])

const expectedComponents: string[] = ["Home", "Tags", "Archive", "Project 1", "Settings", "Help"].sort();
const menuItems: SidebarItem[] = [
  { id: 1, name: "Home", path: "/", icon: <Home className="h-4 w-4" /> },
  { id: 2, name: "Archive", path: "/archive", icon: <Archive className="h-4 w-4" /> },
  { id: 3, name: "Tags", path: "/tags", icon: <Tags className="h-4 w-4" /> },
];
const projects: SidebarItem[] = [
  { id: 1, name: "Project 1", path: "/", icon: <ProjectIcon1 className="h-4 w-4" /> },
];
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

describe('sidebar', () =>
  test('Left_Sidebar renders all components with correct names', async() =>{
    render(<QuickSearchProvider><LeftSidebarClient menuItems={menuItems} projects={projects} bottomMenuItems={bottomMenuItems} open={true} tags={testTags} userEmail={userEmail} userRole={username}/></QuickSearchProvider>);
    const componentNames = await screen.getAllByTestId("sidebar").map(elem => elem.textContent ?? '').sort();
    expect(componentNames).toEqual(expectedComponents);
    const foldedComponentNames = await screen.queryAllByTestId("hiddensidebar").map(elem => elem.textContent ?? '').sort();
    expect(foldedComponentNames).toEqual([]);
    expect(screen.getByText((content) => content.includes(username))).toBeInTheDocument();
    expect(screen.getByText((content) => content.includes(userEmail))).toBeInTheDocument();
  }),

  test('Left_Sidebar renders different components if hidden', async() =>{
    render(<QuickSearchProvider><LeftSidebarClient menuItems={menuItems} projects={projects} bottomMenuItems={bottomMenuItems} open={true} tags={testTags} userEmail={userEmail} userRole={username} /></QuickSearchProvider>)

    // hides sidebar
    const button = screen.getByTestId("sidebar_hide");
    fireEvent.click(button);

    const componentNames = await screen.queryAllByTestId("hiddensidebar");
    expect(componentNames.length).toEqual(expectedHiddenomponents.length);
    const unfoldedComponentNames = await screen.queryAllByTestId("sidebar");
    expect(unfoldedComponentNames).toEqual([]);
    expect(screen.queryByText((content) => content.includes(username))).not.toBeInTheDocument();
    expect(screen.queryByText((content) => content.includes(userEmail))).not.toBeInTheDocument();

    // unhides sidebar
    const buttonUnhide = screen.getByTestId("sidebar_show");
    fireEvent.click(buttonUnhide);
    const newComponentNames = await screen
      .queryAllByTestId("sidebar")
      .map((elem) => elem.textContent ?? "")
      .sort();
    expect(newComponentNames).toEqual(expectedComponents);
    const foldedComponentNames = await screen.queryAllByTestId("hiddensidebar");
    expect(foldedComponentNames).toEqual([]);
    expect(screen.getByText((content) => content.includes(username))).toBeInTheDocument();
    expect(screen.getByText((content) => content.includes(userEmail))).toBeInTheDocument();
  }),
);


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


