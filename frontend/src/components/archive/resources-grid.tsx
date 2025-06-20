"use client"

import React from "react";
import "@/app/globals.css";
import { AgGridReact } from "ag-grid-react";
import { AllCommunityModule, ModuleRegistry } from "ag-grid-community";
import type { ColDef, GridReadyEvent, RowClickedEvent, SortChangedEvent } from "ag-grid-community";
import { tableTheme } from "@/lib/tableConfig";
import { MetadataTypeEnum, useSidebar } from "@/context/sidebar-provider";
import GetFileIcon from "../getFileIcon";
import { ApiResponse } from "@/types/apiResponse.type";
import { useArchive } from "@/context/archive-provider";
import { GridRequest, GridRequestSchema } from "@/types/gridRequest.type";
import OpenFileButton from "../open-file-button";
import RestoreIcon from "@/icons/restore-icon";
import { Button } from "@/components/ui/button";
import { UntrashResource } from "@/actions/trashResourceActions";

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
        trashOpen,
        gridReloadTrigger
    } = useArchive();

    // Data for grid
    const [rowData, setRowData] = React.useState<ResourceGridItem[]>([]);
    const [loading, setLoading] = React.useState(false);
    
    // Sort states -- Controlled by AgGrid
    const [sortBy, setSortBy] = React.useState<string>('');
    const [sortDirection, setSortDirection] = React.useState<'asc' | 'desc'>('asc');
    
    // Grid API reference
    const gridRef = React.useRef<AgGridReact>(null);
    
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const dateFormatter = (params: any) =>
    {
        if (!params.value) return '';
        
        // Extract data part before T and reverse
        const datePart = params.value.split('T')[0];
        const [year, month, day] = datePart.split('-');
        return `${day}-${month}-${year}`;
    } 
    
    // Column definitions
    const columnDefs: ColDef[] = [
        { field: 'name', headerName: 'Name', cellRenderer: renderResourceIcon },
        { field: 'publicationDate', headerName: 'Publication Date', maxWidth: 200, cellStyle: { textAlign: 'center' }, headerClass: 'ag-header-cell-centered', valueFormatter: dateFormatter},
        ...(trashOpen ? [{field: 'trashDate', headerName: 'Trash Date', maxWidth: 200, valueFormatter: dateFormatter, sort: 'asc' as const}] : []),
        { field: "", maxWidth: trashOpen ? 120 : 50, minWidth: trashOpen ? 100 : 50, cellRenderer:renderRowButton(), resizable: false, cellClass: 'no-row-click' }
    ];
    
    // Fetch data function
    const fetchData = React.useCallback(async () => 
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
            const response = trashOpen ?
                await fetch(`/api/resources/trash-grid`, 
                {
                    credentials: 'include',
                })
                :
                await fetch(`/api/resources/grid`,
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
                setRowData(trashOpen ? data.body : data.body.items);
                setTotalItems(trashOpen ? data.body.length : data.body.totalCount);
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
        setTotalItems,
        trashOpen
    ]);
    
    // Fetch data when dependencies change or when trigger is activated
    React.useEffect(() => 
    {
        fetchData();
    }, [fetchData, gridReloadTrigger]);
    
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
            overlayNoRowsTemplate='<h1 className="text-2xl"> Nothing here.</h1>'
        />
    )
    
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
                    {trashOpen && <Button variant="ghost" className="hover:bg-gray-200" onClick={async () => { await UntrashResource(params.data.id, params.data.type); await fetchData(); } }><RestoreIcon className="h-10 w-10 text-gray-500" style={{ width: '23px', height: '23px', minWidth: '23px', minHeight: '23px' }} /></Button> }
                </div>
            )
        }
    }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


