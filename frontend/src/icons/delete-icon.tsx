import * as React from "react";

export default function DeleteIcon({ fill = "#000", ...props }: React.SVGProps<SVGSVGElement>) {
  return (
    <svg 
        {...props} 
        width="30" 
        height="30" 
        viewBox="0 0 24 24"
        xmlns="http://www.w3.org/2000/svg"
        fill="none"
    >
        <path 
            stroke={fill}
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth="2"
            d="M10 11v6M14 11v6M4 7h16M6 7h12v11a3 3 0 0 1-3 3H9a3 3 0 0 1-3-3zM9 5a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2v2H9z"
        />
    </svg>
  );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


