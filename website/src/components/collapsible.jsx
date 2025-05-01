'use client';

import React, { useState } from 'react';

/**
 * Collapsible component that can be toggled open/closed
 */
export function Collapsible({ title, children, defaultOpen = false }) {
  const [isOpen, setIsOpen] = useState(defaultOpen);

  return (
    <div className="border border-gray-200 rounded-md my-4">
      <button
        className="flex justify-between items-center w-full px-4 py-2 text-left font-medium bg-gray-100 hover:bg-gray-200 focus:outline-none"
        onClick={() => setIsOpen(!isOpen)}
      >
        <span>{title}</span>
        <span className="text-gray-500">
          {isOpen ? '▼' : '►'}
        </span>
      </button>
      
      {isOpen && (
        <div className="p-4 border-t border-gray-200">
          {children}
        </div>
      )}
    </div>
  );
}

/**
 * Specialized Collapsible for showing inherited members
 */
export function CollapsibleInherited({ title, children }) {
  return (
    <Collapsible title={title} defaultOpen={false}>
      <div className="space-y-2">
        {children}
      </div>
    </Collapsible>
  );
}