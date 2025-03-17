import * as React from "react";

export default function Tags(props: React.SVGProps<SVGSVGElement>) {
  return (
    <svg {...props} fill="none" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 14 14">
      <path
        clipRule="evenodd"
        d="M8.61 2.333H11a1.333 1.333 0 0 1 1.333 1.333v2.391c0 .177-.07.346-.195.471l-4.529 4.529a1.333 1.333 0 0 1-1.885 0L3.609 8.942a1.333 1.333 0 0 1 0-1.885l4.529-4.529a.667.667 0 0 1 .471-.195Z"
        stroke="#000"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <path d="m5.667 11-1.342.67a1.333 1.333 0 0 1-1.845-.724l-1.077-2.87a1.333 1.333 0 0 1 .6-1.634L7 3.666" stroke="#000" strokeLinecap="round" strokeLinejoin="round" />
      <path d="M11.333 4A.667.667 0 1 0 10 4a.667.667 0 0 0 1.333 0Z" fill="#000" />
    </svg>
  );
}
