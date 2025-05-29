"use client"

import React from "react";
import "@/app/globals.css";
import { AgGridReact } from "ag-grid-react";
import { AllCommunityModule, ModuleRegistry } from "ag-grid-community";
import type { CellContextMenuEvent, ColDef, GridApi, GridReadyEvent, RowClickedEvent, SortChangedEvent } from "ag-grid-community";
import { tableTheme } from "@/lib/tableConfig";
import { ArchiveRestore } from "lucide-react";
import { MetadataTypeEnum, useSidebar } from "@/context/sidebar-provider";
import { useUserRole } from "@/context/user-role-context";
import GetFileIcon from "../getFileIcon";
import { ApiResponse } from "@/types/apiResponse.type";
import { useArchive } from "@/context/archive-provider";
import { GridRequest, GridRequestSchema } from "@/types/gridRequest.type";
import OpenFileButton from "../open-file-button";

ModuleRegistry.registerModules([AllCommunityModule]);

export type ResourceGridItem =
{
    id: string;
    name: string;
    publicationDate: string;
    type: MetadataTypeEnum;
    fileType: string;
    creationDate: string;
}

export default function ResourcesGrid() 
{
    // Contexts
    const { openRightSidebar } = useSidebar();
    const {
        searchQuery,
        currentPage,
        pageSize,
        setTotalItems,
        typeFilter,
        publicationDateRangeMin,
        publicationDateRangeMax,
        tagFilter,
        regionFilter,
    } = useArchive();

    // Data for grid
    const [rowData, setRowData] = React.useState<ResourceGridItem[]>([]);
    const [loading, setLoading] = React.useState(false);
    
    // Sort states -- Controlled by AgGrid
    const [sortBy, setSortBy] = React.useState<string>('');
    const [sortDirection, setSortDirection] = React.useState<'asc' | 'desc'>('asc');
    
    // Grid API reference
    const gridRef = React.useRef<AgGridReact>(null);
    
    // Column definitions
    const columnDefs: ColDef[] = [
        { field: 'name', headerName: 'Name', cellRenderer: renderResourceIcon },
        { field: 'publicationDate', headerName: 'Publication Date', maxWidth: 200, valueFormatter: (params) => 
        {
            if (!params.value) return '-';
            
            // Extract data part before T and reverse
            const datePart = params.value.split('T')[0];
            const [year, month, day] = datePart.split('-');
            return `${day}-${month}-${year}`;
        } },
        { field: "", minWidth: 50, maxWidth: 50, cellRenderer:renderRowButton(), resizable: false, cellClass: 'no-row-click' }
    ];
    
    // Fetch data function
    const fetchData = React.useCallback(async (getTrash: boolean = false) => 
    {
        setLoading(true);
        
        try 
        {   
            // Create the request
            const request: GridRequest = GridRequestSchema.parse(
            {
                pageIndex: currentPage,
                pageSize: pageSize,
                searchQuery: searchQuery || undefined,
                sortBy: sortBy || undefined,
                sortDirection: sortDirection || undefined,
                filterOptions:
                {
                    typeFilter: typeFilter || undefined,
                    pubdateMin: publicationDateRangeMin || undefined,
                    pubdateMax: publicationDateRangeMax || undefined,
                    tagFilter: tagFilter || undefined,
                    regionFilter: regionFilter || undefined,
                },
            });
            
            // Fetch the data from the backend
            const response = getTrash ?
                await fetch(`/api/resources/list`)
                : await fetch(`/api/resources/grid`,
                {
                    method: 'POST',
                    credentials: 'include',
                    headers: 
                    {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify(request),
                });
            
            if (!response.ok)
                throw new Error(`HTTP error! Status: ${response.status}`);
                
            const data: ApiResponse = await response.json();
            
            if (data.success) 
            {
                setRowData(data.body.items);
                setTotalItems(data.body.totalCount);
            }
            else 
            {
                console.error('API returned error:', data.message);
                setRowData([]);
                setTotalItems(0);
            }
        }
        catch (error) 
        {
            console.error('Failed to fetch data:', error);
            setRowData([]);
            setTotalItems(0);
        }
        finally 
        {
            setLoading(false);
        }
    }, [
        currentPage,
        pageSize,
        searchQuery,
        typeFilter,
        publicationDateRangeMax,
        publicationDateRangeMin,
        sortBy,
        sortDirection,
        tagFilter,
        regionFilter,
        setTotalItems
    ]);
    
    // Fetch data when dependencies change
    React.useEffect(() => 
    {
        fetchData();
    }, [fetchData]);
    
    // Handle AgGrid sort changes
    const onSortChanged = React.useCallback((event: SortChangedEvent) => 
    {
        const columnState = event.api.getColumnState();
        const sortedColumn = columnState.find(col => col.sort !== null);
        
        if (sortedColumn) 
        {
            setSortBy(sortedColumn.colId || '');
            setSortDirection(sortedColumn.sort === 'desc' ? 'desc' : 'asc');
        }
        else 
        {
            setSortBy('');
            setSortDirection('asc');
        }
    }, []);
    
    // On grid ready event
    const onGridReady = React.useCallback((params: GridReadyEvent) => 
    {
        params.api.sizeColumnsToFit();
    }, []);
    
    // On row clicked event
    const onRowClicked = React.useCallback((event: RowClickedEvent) => 
    {
        const target = event.event?.target as HTMLElement;
        if (target?.closest('.no-row-click')) return;
    
        const rowItem: ResourceGridItem = event.data;
        openRightSidebar(rowItem.id, rowItem.type);
    }, [openRightSidebar]);
    
    return (
        <AgGridReact
                ref={gridRef}
                rowData={rowData}
                columnDefs={columnDefs}
                onGridReady={onGridReady}
                onRowClicked={onRowClicked}
                loading={loading}
                onSortChanged={onSortChanged}
                defaultColDef={
                {
                    resizable: true,
                    sortable: true,
                    filter: false,
                    flex: 1
                }}
                animateRows={true}
                pagination={false}
                theme={tableTheme}
                suppressCellFocus={true}
                domLayout="autoHeight"
            />
    )
}

function renderResourceIcon(params: { data: ResourceGridItem; value: string }) 
{
    return (
        <div className="flex items-center justify-start gap-2">
            <GetFileIcon fileType={params.data.fileType} />
            <span className="">{params.value}</span>
        </div>
    )
}

function renderRowButton() 
{
    return function rowButtonRenderer(params: {data: ResourceGridItem}) 
    {
        return (
            <div className="flex w-full h-full items-center justify-center">
                { params.data.type === 'resource' && <OpenFileButton id={params.data.id} fileType={params.data.fileType} asIcon={true} />}
            </div>
        )
    }
}