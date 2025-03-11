import { ColumnDef } from "@tanstack/react-table";

export type Result = {
    title: string;
    uploadDate: Date;
    updateDate: Date;
    fileSize: number;
}

export const columns: ColumnDef<Result>[] = [
    {
        accessorKey: "title",
        header: "Title",
    },
    {
        accessorKey: "upload",
        header: "Date Upload",
    },
    {
        accessorKey: "update",
        header: "Last Update",
    },
    {
        accessorKey: "filesize",
        header: "File Size",
    },
]
