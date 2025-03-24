import * as React from "react";

export default function Projects({ fill = "#000", ...props }: React.SVGProps<SVGSVGElement>) {
  return (
    <svg {...props} width="76" height="76" viewBox="0 0 76 76" fill="none" xmlns="http://www.w3.org/2000/svg">
      <g clipPath="url(#clip0)">
        <path 
          fillRule="evenodd" 
          clipRule="evenodd" 
          d="M45.125 68.875 16.625 57V19l28.5-11.875v61.75Zm-32.704-5.91A4.75 4.75 0 0 1 9.5 58.582V17.418a4.75 4.75 0 0 1 2.921-4.384L42.384.546a7.125 7.125 0 0 1 9.866 6.579V9.5h5.938a8.312 8.312 0 0 1 8.312 8.312v40.375a8.312 8.312 0 0 1-8.313 8.313H52.25v2.375a7.125 7.125 0 0 1-9.866 6.578L12.421 62.966Zm39.829-3.59h5.938a1.188 1.188 0 0 0 1.187-1.188V17.812a1.187 1.187 0 0 0-1.188-1.187H52.25v42.75Z"
          fill={fill}
        />
      </g>
      <defs>
        <clipPath id="clip0">
          <path fill="#fff"  d="M0 0h76v76H0z" />
        </clipPath>
      </defs>
    </svg>
  );
}
