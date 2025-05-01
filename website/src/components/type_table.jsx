'use client';

import React from 'react';
import { TSType } from './typescript_type';

/**
 * Component to display a table of types with their descriptions
 */
export function TypeTable({ types }) {
  if (!types || types.length === 0) {
    return null;
  }

  // Determine which columns to show based on data
  const showName = types.some(type => type.name);
  const showType = types.some(type => type.type);
  const showDescription = types.some(type => type.description);

  return (
    <div className="overflow-x-auto my-4">
      <table className="min-w-full border-collapse">
        <thead>
          <tr className="bg-gray-100">
            {showName && (
              <th className="py-2 px-4 border-b text-left font-medium text-gray-600">
                Name
              </th>
            )}
            {showType && (
              <th className="py-2 px-4 border-b text-left font-medium text-gray-600">
                Type
              </th>
            )}
            {showDescription && (
              <th className="py-2 px-4 border-b text-left font-medium text-gray-600">
                Description
              </th>
            )}
          </tr>
        </thead>
        <tbody>
          {types.map((item, index) => (
            <tr key={index} className={index % 2 === 0 ? 'bg-white' : 'bg-gray-50'}>
              {showName && (
                <td className="py-2 px-4 border-b font-mono">
                  {item.name}
                </td>
              )}
              {showType && (
                <td className="py-2 px-4 border-b">
                  <TSType type={item.type} />
                </td>
              )}
              {showDescription && (
                <td className="py-2 px-4 border-b">
                  {item.description}
                </td>
              )}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}