import * as React from "react";

export default function WordIcon({ fill = "#000", ...props }: React.SVGProps<SVGSVGElement>) {
  return (
    <svg 
        {...props} 
        width="18" 
        height="18" 
        viewBox="0 0 24 24"
        xmlns="http://www.w3.org/2000/svg"
        fill="none"
        strokeWidth="3"
    >
        <path
            fill={fill}
            fillRule="evenodd"
            d="M10 1a1 1 0 0 0-.707.293l-6 6A1 1 0 0 0 3 8v12a3 3 0 0 0 3 3h4a1 1 0 1 0 0-2H6a1 1 0 0 1-1-1V9h5a1 1 0 0 0 1-1V3h7a1 1 0 0 1 1 1v5a1 1 0 1 0 2 0V4a3 3 0 0 0-3-3zM9 7H6.414L9 4.414zm3.952 5.694a1 1 0 1 0-1.904.612l2.25 7a1 1 0 0 0 1.904 0l1.298-4.038 1.298 4.038a1 1 0 0 0 1.904 0l2.25-7a1 1 0 1 0-1.904-.612l-1.298 4.038-1.298-4.038a1 1 0 0 0-1.904 0l-1.298 4.038z"
            clipRule="evenodd"
        ></path>    
    </svg>
  );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


