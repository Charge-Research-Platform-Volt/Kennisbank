import * as React from "react";

export default function WebsiteIcon({ fill = "#000", ...props }: React.SVGProps<SVGSVGElement>) {
  return (
    <svg 
        {...props} 
        width="18" 
        height="18" 
        viewBox="0 0 64 64"
        xmlns="http://www.w3.org/2000/svg"
        stroke={fill}
        fill="none"
        strokeWidth="3"
    >
         <path d="M39.93 55.72a24.86 24.86 0 1 1 16.93-23.57 37 37 0 0 1-.73 6"></path>
         <path d="M37.86 51.1A47 47 0 0 1 32 56.7M32 7a34.14 34.14 0 0 1 11.57 23 34 34 0 0 1 .09 4.85M32 7a34.1 34.1 0 0 0-11.69 25.46c0 16.2 7.28 21 11.66 24.24M10.37 19.9h43.38M32 6.99V56.7M11.05 45.48h25.99M7.14 32.46l49.72-.61M53.57 57 58 52.56l-8-8 4.55-2.91a.38.38 0 0 0-.12-.7l-15.29-3.58a.39.39 0 0 0-.46.46L42 53.41a.39.39 0 0 0 .71.13L45.57 49Z"></path>
    </svg>
  );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


