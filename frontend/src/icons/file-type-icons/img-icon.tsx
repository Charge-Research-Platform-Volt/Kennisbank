import * as React from "react";

export default function ImgIcon({ fill = "#000", ...props }: React.SVGProps<SVGSVGElement>) {
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
            d="M9.293 1.293A1 1 0 0 1 10 1h8a3 3 0 0 1 3 3v16a3 3 0 0 1-3 3H6a3 3 0 0 1-3-3V8a1 1 0 0 1 .293-.707zM18 3h-7v5a1 1 0 0 1-1 1H5v5.586l2.793-2.793a1 1 0 0 1 1.414 0L13 15.586l1.293-1.293a1 1 0 0 1 1.414 0L19 17.586V4a1 1 0 0 0-1-1M5 20v-2.586l3.5-3.5 3.793 3.793a1 1 0 0 0 1.414 0L15 16.414l3.927 3.927.01.01A1 1 0 0 1 18 21H6a1 1 0 0 1-1-1M6.414 7H9V4.414zm8.086 6a1.5 1.5 0 1 0 0-3 1.5 1.5 0 0 0 0 3"
            clipRule="evenodd"
        ></path>
    </svg>
  );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


