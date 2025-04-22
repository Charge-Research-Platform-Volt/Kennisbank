import * as React from "react";

export default function Filter({ fill = "#000", ...props }: React.SVGProps<SVGSVGElement>) {
  return (
    <svg 
        {...props} 
        width="30" 
        height="30" 
        viewBox="0 0 24 24"
        xmlns="http://www.w3.org/2000/svg"
    >
        <path 
            fill={fill}
            d="M3 7a1 1 0 0 1 1-1h16a1 1 0 1 1 0 2H4a1 1 0 0 1-1-1m3 5a1 1 0 0 1 1-1h10a1 1 0 1 1 0 2H7a1 1 0 0 1-1-1m3 5a1 1 0 0 1 1-1h4a1 1 0 1 1 0 2h-4a1 1 0 0 1-1-1"
        />
    </svg>
  );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


