import { z } from 'zod';

// Define GridFilterOptions schema
const GridFilterOptionsSchema = z.object(
{
    typeFilter: z.string().array().optional(),
    pubdateMin: z.string().date('Pubdate min must be in date format').optional(),
    pubdateMax: z.string().date('Pubdate max must be in date format').optional(),
    tagFilter: z.string().uuid('Please provide valid IDs').array().optional(),
    regionFilter: z.string().uuid('Please provide valid IDs').array().optional(),
});

type GridFilterOptions = z.infer<typeof GridFilterOptionsSchema>;

// Define the GridRequest schema
const GridRequestSchema = z.object(
{
    pageIndex: z.number().min(1, 'Page index must be at least 1'),
    pageSize: z.number().min(1, 'Page size must be at least 1'),
    searchQuery: z.string().optional(),
    sortBy: z.string().optional(),
    sortDirection: z.string().optional(),
    filterOptions: GridFilterOptionsSchema.optional(),
});
    
type GridRequest = z.infer<typeof GridRequestSchema>;

export { GridRequestSchema, GridFilterOptionsSchema };
export type { GridRequest, GridFilterOptions };

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


