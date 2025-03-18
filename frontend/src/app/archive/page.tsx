"use client";
import { Input } from "@/components/ui/input";
import { ResultsTable } from "./results-table";
import React, { useState } from "react";
import { columns, Result } from "./archive_componenents";
import { toast } from "sonner";



export default function Archive() {
    const [results, setResults] = useState<Result[]>([]);
    const [queryText, setQueryText] = useState("");
    const [page, setPage] = useState<number>(1);
    const [size, setSize] = useState<number>(20);

    const handleKeyDown = async (event: React.KeyboardEvent<HTMLInputElement>) => {
        if (event.key === "Enter") {
            console.log("pressed enter");
            await query(queryText, page, size);
        }
    };

    async function query(q: string, pageIndex: number, pageSize: number) {
        try {
            const response = await fetch(`/api?query=${encodeURIComponent(q)}&pageIndex=${pageIndex}&pageSize=${pageSize}`, {
                method: "GET",
                headers: { "Accept": "application/json" },
            });

            if (response.ok) {
                const data = await response.json();

                const transformedData: Result[] = data.files.map((item: any) => ({
                    title: item.name,
                    uploadDate: new Date(),
                    updateDate: new Date(),
                    fileSize: item.fileType.length,
                }))

                setResults(transformedData);


            } else if (response.status === 400) {
                toast.warning("Invalid Search Query");
            } else {
                toast.error("Internal Server Error. Please try again later.");
            }
        } catch (error) {
            console.error("Fetch error:", error);
            toast.error("An error occurred while fetching data.");
        }
    }

    return (
        <div className="bg-gray-100 w-full h-full pt-3 p-2">
            <Input type="string" className="bg-white font-semibold" placeholder="Search..." onKeyDown={handleKeyDown} onChange={(e) => setQueryText(e.target.value)}/>

            <ResultsTable columns={columns} data={results}/>
        </div>
    );
}