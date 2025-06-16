"use client"

import { useState, useMemo, useEffect } from 'react'
import { Search, X } from 'lucide-react'
import Fuse from 'fuse.js'

interface SearchResult {
  title: string
  description: string
  url: string
  type: 'docs' | 'guide'
}

interface FuseSearchProps {
  searchData: SearchResult[]
}

export function FuseSearch({ searchData }: FuseSearchProps) {
  const [query, setQuery] = useState('')
  const [isOpen, setIsOpen] = useState(false)
  const [results, setResults] = useState<SearchResult[]>([])

  // Configure Fuse.js
  const fuse = useMemo(() => {
    const options = {
      keys: [
        { name: 'title', weight: 0.7 },        // Title is most important
        { name: 'description', weight: 0.3 },   // Description is secondary
      ],
      threshold: 0.4,          // 0.0 = exact match, 1.0 = match anything
      distance: 100,           // How far to search for matches
      minMatchCharLength: 2,   // Minimum characters to start searching
      includeScore: true,      // Include relevance score
    }
    
    return new Fuse(searchData, options)
  }, [searchData])

  // Perform search when query changes
  useEffect(() => {
    if (!query.trim()) {
      setResults([])
      return
    }

    const searchResults = fuse.search(query, { limit: 8 })
    setResults(searchResults.map(result => result.item))
  }, [query, fuse])

  const clearSearch = () => {
    setQuery('')
    setResults([])
    setIsOpen(false)
  }

  return (
    <div className="relative w-full max-w-lg">
      <div className="relative">
        <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
        <input
          type="text"
          placeholder="Search..."
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          onFocus={() => setIsOpen(true)}
          className="w-full pl-10 pr-10 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent dark:bg-gray-800 dark:border-gray-600 dark:text-white"
        />
        {query && (
          <button
            onClick={clearSearch}
            className="absolute right-3 top-1/2 transform -translate-y-1/2 text-gray-400 hover:text-gray-600"
          >
            <X className="h-4 w-4" />
          </button>
        )}
      </div>
      
      {isOpen && query && (
        <div className="absolute top-full left-0 right-0 mt-1 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-600 rounded-lg shadow-lg z-50 max-h-96 overflow-y-auto">
          {results.length > 0 ? (
            results.map((result, index) => (
              <a
                key={index}
                href={result.url}
                onClick={() => setIsOpen(false)}
                className="block px-4 py-3 hover:bg-gray-50 dark:hover:bg-gray-700 border-b border-gray-100 dark:border-gray-600 last:border-b-0 transition-colors"
              >
                <div className="font-medium text-gray-900 dark:text-white">
                  {result.title}
                </div>
                {result.description && (
                  <div className="text-sm text-gray-600 dark:text-gray-300 mt-1 line-clamp-2">
                    {result.description}
                  </div>
                )}
                <div className="text-xs text-blue-600 dark:text-blue-400 mt-1 capitalize">
                  {result.type}
                </div>
              </a>
            ))
          ) : (
            <div className="px-4 py-3 text-sm text-gray-500 dark:text-gray-400">
              No results found for &quot;{query}&quot;
            </div>
          )}
        </div>
      )}
      
      {/* Overlay to close search when clicking outside */}
      {isOpen && (
        <div 
          className="fixed inset-0 z-40" 
          onClick={() => setIsOpen(false)}
        />
      )}
    </div>
  )
}