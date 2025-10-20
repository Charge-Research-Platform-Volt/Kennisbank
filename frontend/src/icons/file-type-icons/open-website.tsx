import * as React from "react";

export default function OpenWebsiteIcon({ fill = "#000", ...props }: React.SVGProps<SVGSVGElement>) {
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
      d="M5 12V6a1 1 0 0 1 1-1h12a1 1 0 0 1 1 1v12a1 1 0 0 1-1 1h-6m-3.889-7H12m0 0v3.889M12 12l-7 7"
    ></path>
    </svg>
  );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


