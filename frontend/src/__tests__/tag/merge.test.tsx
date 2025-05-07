import { expect, test, vi, beforeEach, describe } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { MergeTag } from '@/actions/tagActions';
import { TagMergeButton } from '@/app/(knowledgebank)/tags/components/merge-tags-popup';
import { TagArraySchema } from '@/types/tag.type';
import React from 'react';

// RENDERING THE TAG PAGE DOES NOT WORK DUE TO COOKIE / AUTH STUFF, SAME WITH LISTTAGS

// Mock fetchTagSearch once for all tests
vi.mock('@/actions/tagActions', () => ({
  MergeTag: vi.fn().mockResolvedValue({ success: true, message: 'Tags merged successfully.' }),
}));

const tags = TagArraySchema.parse([
    {
        name: "bla",
        id: "3b542bc8-9b38-4c40-926d-dcd46c576fdf",
        isStandardized: false,
        isApproved: false,
        approvedOn: null,
        approvedBy: null,
        createdBy: "3b542bc8-9b38-4c40-926d-dcd46c576fdf", // Must be a valid UUID
        createdOn: "2024-01-01T12:00:00Z",
        usageCount: 0,
        canEditAndDelete: true,
    },

    {
        name: "test",
        id: "3b542bc8-9b38-4c40-926d-dcd46c576fef",
        isStandardized: false,
        isApproved: false,
        approvedOn: null,
        approvedBy: null,
        createdBy: "3b542bc8-9b38-4c40-926d-dcd46c576fdf", // Must be a valid UUID
        createdOn: "2024-01-01T12:00:00Z",
        usageCount: 0,
        canEditAndDelete: true,
    }
])

describe('merge popup', () =>{
  beforeEach(() => {
    vi.clearAllMocks();
  });

  test('Test if the merge popup calls the correct function', async() => {
        render(<TagMergeButton tag={tags[0]} extraTag={tags[1]}></TagMergeButton>)

        // OPEN POPUP
        const openButton = await screen.getByTestId("open");
        fireEvent.click(openButton);

        // VALUES ARE SET SO JUST MERGE
        const mergeButton = await screen.getByTestId("merge");
        fireEvent.click(mergeButton);

        screen.debug(undefined, Infinity)
        console.log(mergeButton)

        // EXPECT THE MERGE ACTION TO HAVE BEEN CALLED ONCE
        await waitFor(() => expect(MergeTag).toHaveBeenCalledTimes(1));
        //expect(MergeTag).toHaveBeenCalledOnce();
    });
})

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)