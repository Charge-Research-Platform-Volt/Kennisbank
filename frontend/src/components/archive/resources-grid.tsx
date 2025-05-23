"use client"

import React from "react";
import "@/app/globals.css";
import { AgGridReact } from "ag-grid-react";
import { AllCommunityModule, ModuleRegistry } from "ag-grid-community";
import type { CellContextMenuEvent, ColDef, GridApi, GridReadyEvent, RowClickedEvent } from "ag-grid-community";
import { tableTheme } from "@/lib/tableConfig";
import { ArchiveRestore } from "lucide-react";
import { MetadataTypeEnum, useSidebar } from "@/context/sidebar-provider";
import { useUserRole } from "@/context/user-role-context";
import GetFileIcon from "../getFileIcon";

ModuleRegistry.registerModules([AllCommunityModule]);

export type RowItem =
{
    name: string;
    publicationDate: string;
    id: string;
    type: MetadataTypeEnum;
    fileType: string;
}

export default function ResourcesGrid({items}: {items: RowItem[]}) 
{
    const { openRightSidebar } = useSidebar();
    const { userRole } = useUserRole();

    // Data for grid
    const [rowData, setRowData] = React.useState<RowItem[]>(items);
    
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
        { field: "", minWidth: 50, maxWidth: 50, cellRenderer:renderRowButton(), resizable: false}
    ];
    
    // On grid ready event
    const onGridReady = React.useCallback((params: GridReadyEvent) => 
    {
        params.api.sizeColumnsToFit();
    }, []);
    
    // On row clicked event
    const onRowClicked = React.useCallback((event: RowClickedEvent) => 
    {
        const rowItem: RowItem = event.data;
        openRightSidebar(rowItem.id, rowItem.type);
    }, []);
    
    return (
        <AgGridReact
                ref={gridRef}
                rowData={rowData}
                columnDefs={columnDefs}
                onGridReady={onGridReady}
                onRowClicked={onRowClicked}
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
            />
    )
}

function renderResourceIcon(params: { data: RowItem; value: string }) 
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
    return function rowButtonRenderer(params: {data: RowItem}) 
    {
        return (
            <div className="flex w-full h-full items-center justify-center cursor-pointer">
                <ArchiveRestore />
            </div>
        )
    }
}