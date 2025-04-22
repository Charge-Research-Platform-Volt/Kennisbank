import * as React from "react";

export default function ZipIcon({ fill = "#000", ...props }: React.SVGProps<SVGSVGElement>) {
  return (
    <svg 
        {...props} 
        width="18" 
        height="18" 
        viewBox="0 0 24 24"
        xmlns="http://www.w3.org/2000/svg"
        fill="none"
    >
        <path
            fill={fill}
            fillRule="evenodd"
            d="M9.293 1.293A1 1 0 0 1 10 1h8a3 3 0 0 1 3 3v13a1 1 0 1 1-2 0V4a1 1 0 0 0-1-1h-7v5a1 1 0 0 1-1 1H5v11a1 1 0 0 0 1 1h2a1 1 0 1 1 0 2H6a3 3 0 0 1-3-3V8a1 1 0 0 1 .293-.707zM14.5 3a1.5 1.5 0 1 1 0 3 1.5 1.5 0 0 1 0-3M6.414 7H9V4.414zM11 20.577A3.423 3.423 0 0 0 14.423 24h.154A3.423 3.423 0 0 0 18 20.577c0-.706-.18-1.401-.523-2.019l-1.491-2.684a1.7 1.7 0 0 0-2.972 0l-1.49 2.684A4.16 4.16 0 0 0 11 20.578M14.5 22a1.5 1.5 0 1 0 0-3 1.5 1.5 0 0 0 0 3m0-8a1.5 1.5 0 1 0 0-3 1.5 1.5 0 0 0 0 3M16 8.5a1.5 1.5 0 1 1-3 0 1.5 1.5 0 0 1 3 0"
            clipRule="evenodd"
        ></path>
    </svg>
  );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


