import { expect, test } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import LeftSidebarClient from '@/components/left-sidebar/left-sidebar-client'
import { TagsArraySchema } from '@/types/tag.type'
import { QuickSearchProvider } from '@/components/quick-search-context'

const testTags = TagsArraySchema.parse([])

const expectedComponents : string[] = ['Home', 'Tags', 'Archive', 'Project 1', 'Settings', 'Help'].sort()

describe('sidebar', () =>
    test('Left_Sidebar renders all components with correct names', async() =>{
        render(<QuickSearchProvider><LeftSidebarClient tags={testTags}/></QuickSearchProvider>)
        const componentNames = await screen.getAllByTestId("sidebar").map(elem => elem.textContent ?? '').sort();
        expect(componentNames).toEqual(expectedComponents);
    }),

    test('Left_Sidebar renders different components if hidden', async() =>{
        render(<QuickSearchProvider><LeftSidebarClient tags={testTags}/></QuickSearchProvider>)

        // hides sidebar
        const button = screen.getByTestId("sidebar_hide");
        fireEvent.click(button);
        const componentNames = await screen.queryAllByTestId("sidebar");
        expect(componentNames).toEqual([]);

        // unhides sidebar
        const buttonUnhide = screen.getByTestId("sidebar_hide");
        fireEvent.click(buttonUnhide);
        const newComponentNames = await screen.queryAllByTestId("sidebar").map(elem => elem.textContent ?? '').sort();
        expect(newComponentNames).toEqual(expectedComponents);
    })
)
