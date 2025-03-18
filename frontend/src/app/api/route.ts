'use server'
import { NextResponse } from "next/server";

export async function GET(request: Request) {
    const { searchParams } = new URL(request.url);
    const query = searchParams.get("query");
    const pageIndex = parseInt(searchParams.get("pageIndex") || "1", 10);
    const pageSize = parseInt(searchParams.get("pageSize") || "20", 10);

    if (!query) {
        return NextResponse.json({ error: "Invalid Search Query" }, { status: 400 });
    }

    try {
        const backendUrl = `http://backend:8080/Search/search-full-text?query=${encodeURIComponent(query)}&pageIndex=${pageIndex}&pageSize=${pageSize}`;
        
        const response = await fetch(backendUrl, {
            method: "GET",
            headers: { "Content-Type": "application/json" },
        });

        // If backend response is not OK, throw an error
        if (!response.ok) {
            const errorMessage = `Backend returned error: ${response.statusText}`;
            console.error(errorMessage);
            return NextResponse.json({ error: errorMessage }, { status: 500 });
        }

        // Parse backend response and forward the JSON response
        const data = await response.json();

        return NextResponse.json(data, { status: 200 });

    } catch (error) {
        console.error("Error fetching search results:", error);
        return NextResponse.json({ error: "Internal Server Error" }, { status: 500 });
    }
}
