import * as React from "react";

export default function Search({ fill = "#000", ...props }: React.SVGProps<SVGSVGElement>) {
  return (
    <svg {...props} width="76" height="76" viewBox="0 0 76 76" fill="none" xmlns="http://www.w3.org/2000/svg">
      <g clipPath="url(#clip0)">
        <path
          fillRule="evenodd"
          clipRule="evenodd"
          d="M55.775 49.133a30.737 30.737 0 0 0 5.975-18.258C61.75 13.823 47.927 0 30.875 0S0 13.823 0 30.875 13.823 61.75 30.875 61.75a30.74 30.74 0 0 0 18.263-5.978l-.005.003c.14.19.297.372.469.544l18.29 18.29a4.75 4.75 0 1 0 6.717-6.718l-18.29-18.29a4.793 4.793 0 0 0-.544-.468ZM57 30.875C57 45.303 45.303 57 30.875 57S4.75 45.303 4.75 30.875 16.447 4.75 30.875 4.75 57 16.447 57 30.875Z"
          fill={fill}
        />
      </g>
      <defs>
        <clipPath id="clip0">
          <path fill="#fff" d="M0 0h76v76H0z" />
        </clipPath>
      </defs>
    </svg>
  );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


