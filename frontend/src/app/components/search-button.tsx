"use client";

import { useQuickSearch } from "@/components/quick-search-context";
import Search from "@/icons/search-icon";

export default function SearchButton() {
    const { setIsOpen } = useQuickSearch();

    return (
        <button onClick={() => setIsOpen(true)}>
            <div className="flex h-32 w-32 cursor-pointer flex-col items-center rounded-xl border border-gray-300 bg-gray-100 p-6 shadow-md transition hover:bg-gray-200">
                <Search className="h-50 w-50" fill="#4b5563" />
                <p className="mt-2 text-lg font-medium text-gray-600">Search</p>
            </div>
        </button>
    );
}