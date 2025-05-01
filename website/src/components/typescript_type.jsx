'use client';

import React from 'react';

/**
 * Component to display a TypeScript type with syntax highlighting
 */
export function TSType({ type }) {
  // Basic TypeScript types
  const basicTypes = ['string', 'number', 'boolean', 'any', 'void', 'undefined', 'null', 'never', 'unknown'];
  
  // Parse the type string to handle generics and arrays
  const parseType = (typeStr) => {
    // Handle array types (e.g. string[])
    if (typeStr.endsWith('[]')) {
      const baseType = typeStr.slice(0, -2);
      return (
        <>
          {parseType(baseType)}
          <span className="text-blue-600">[]</span>
        </>
      );
    }
    
    // Handle generic types (e.g. Array<string>)
    if (typeStr.includes('<') && typeStr.includes('>')) {
      const genericMatch = typeStr.match(/^([^<]+)<(.+)>$/);
      if (genericMatch) {
        const [_, baseType, genericParams] = genericMatch;
        
        // Split generic parameters, handling nested generics
        const params = splitGenericParams(genericParams);
        
        return (
          <>
            <span className={basicTypes.includes(baseType) ? 'text-blue-600' : 'text-emerald-600'}>
              {baseType}
            </span>
            <span className="text-gray-600">{'<'}</span>
            {params.map((param, index) => (
              <React.Fragment key={index}>
                {index > 0 && <span className="text-gray-600">, </span>}
                {parseType(param)}
              </React.Fragment>
            ))}
            <span className="text-gray-600">{'>'}</span>
          </>
        );
      }
    }
    
    // Handle union types
    if (typeStr.includes('|')) {
      const unionTypes = typeStr.split('|').map(t => t.trim());
      return (
        <>
          {unionTypes.map((unionType, index) => (
            <React.Fragment key={index}>
              {index > 0 && <span className="text-gray-600"> | </span>}
              {parseType(unionType)}
            </React.Fragment>
          ))}
        </>
      );
    }
    
    // Handle intersection types
    if (typeStr.includes('&')) {
      const intersectionTypes = typeStr.split('&').map(t => t.trim());
      return (
        <>
          {intersectionTypes.map((intersectionType, index) => (
            <React.Fragment key={index}>
              {index > 0 && <span className="text-gray-600"> & </span>}
              {parseType(intersectionType)}
            </React.Fragment>
          ))}
        </>
      );
    }
    
    // Handle literal types (strings in quotes)
    if ((typeStr.startsWith('"') && typeStr.endsWith('"')) || 
        (typeStr.startsWith("'") && typeStr.endsWith("'"))) {
      return <span className="text-amber-600">{typeStr}</span>;
    }
    
    // Handle basic types with special coloring
    if (basicTypes.includes(typeStr)) {
      return <span className="text-blue-600">{typeStr}</span>;
    }
    
    // Default case: user-defined type
    return <span className="text-emerald-600">{typeStr}</span>;
  };
  
  // Helper to split generic parameters correctly, handling nested generics
  const splitGenericParams = (paramsStr) => {
    const result = [];
    let current = '';
    let depth = 0;
    
    for (let i = 0; i < paramsStr.length; i++) {
      const char = paramsStr[i];
      
      if (char === '<') {
        depth++;
        current += char;
      } else if (char === '>') {
        depth--;
        current += char;
      } else if (char === ',' && depth === 0) {
        result.push(current.trim());
        current = '';
      } else {
        current += char;
      }
    }
    
    if (current.trim()) {
      result.push(current.trim());
    }
    
    return result;
  };
  
  return (
    <code className="font-mono bg-gray-100 rounded px-1 py-0.5">
      {parseType(type)}
    </code>
  );
}