import * as React from "react";

export default function AboutIcon({ fill = "#000", ...props }: React.SVGProps<SVGSVGElement>) {
  return (
    <svg 
        {...props} 
        width="30" 
        height="30" 
        viewBox="0 0 24 24"
        xmlns="http://www.w3.org/2000/svg"
        stroke={fill}
        fill="none"
    >
    <path d="M12.5 2.2a10.3 10.3 0 1 0 10.3 10.3A10.3 10.3 0 0 0 12.5 2.2m0 3.3a1 1 0 1 1-1 1 1 1 0 0 1 1-1M14 19h-3v-1h1v-8h-1V9h2v9h1z"></path>
    </svg>
  );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)