"use client"

import React from "react";
import { AgGridReact } from "ag-grid-react";
import { AllCommunityModule, ModuleRegistry } from "ag-grid-community";
import type { ColDef, GridReadyEvent, RowClickedEvent, SortChangedEvent } from "ag-grid-community";
import { tableTheme } from "@/lib/tableConfig";
import { useArchiveSidebar } from "@/context/archive-sidebar-provider";
import GetFileIcon from "../getFileIcon";
import { ResourceGridItem, useArchive } from "@/context/archive-provider";
import OpenFileButton from "../open-file-button";
import RestoreIcon from "@/icons/restore-icon";
import { Button } from "@/components/ui/button";
import { UntrashResource } from "@/actions/trashResourceActions";

ModuleRegistry.registerModules([AllCommunityModule]);

export default function ResourcesGrid() 
{
    // Contexts
    const { openArchiveSidebar } = useArchiveSidebar();
    const {
        rowData,
        loading,
        setSortBy,
        setSortDirection,
        trashOpen,
        triggerGridReload,
    } = useArchive();
    
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
    }, [setSortBy, setSortDirection]);
    
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
        openArchiveSidebar(rowItem.id, rowItem.type);
    }, [openArchiveSidebar]);
    
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
                    {trashOpen && <Button variant="ghost" className="hover:bg-gray-200" onClick={async () => { await UntrashResource(params.data.id, params.data.type); triggerGridReload(); } }><RestoreIcon className="h-10 w-10 text-gray-500" style={{ width: '23px', height: '23px', minWidth: '23px', minHeight: '23px' }} /></Button> }
                </div>
            )
        }
    }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)

