import { Input } from "@/components/ui/input";
import { ResultsTable } from "./results-table";
import React from "react";
import { columns, Result } from "./archive_componenents";
import { FetchWithValidation } from "@/lib/fetchWithValidation";
import { DocumentArraySchema } from "@/types/document.type";



export default async function Archive() {
    var page: number = 1;
    var size: number = 10;
    const results: any[] = [];

    async function query(q: string) {

    }

    return (
        <div className="bg-[#E5E5E5] w-screen h-screen p-3">
            <Input type="string" className="text-[#c9c9c9] bg-white font-semibold" placeholder="Search..."/>

            <ResultsTable columns={columns} data={results}/>
        </div>
    );
}