import * as React from "react";

export default function Settings(props: React.SVGProps<SVGSVGElement>) {
  return (
    <svg {...props} fill="none" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 14 14">
      <g clipPath="url(#a)">
        <path
          d="M3.631 3.231a1.361 1.361 0 0 0 1.79-1.034l.218-1.17a6.125 6.125 0 0 1 2.721 0l.22 1.17a1.361 1.361 0 0 0 1.789 1.034l1.122-.395a6.125 6.125 0 0 1 1.362 2.355l-.905.776a1.36 1.36 0 0 0 0 2.067l.905.775a6.125 6.125 0 0 1-1.362 2.355l-1.123-.395a1.362 1.362 0 0 0-1.79 1.034l-.217 1.17a6.125 6.125 0 0 1-2.72 0l-.22-1.17a1.361 1.361 0 0 0-1.79-1.034l-1.122.395a6.125 6.125 0 0 1-1.362-2.355l.906-.776a1.361 1.361 0 0 0 0-2.066l-.906-.776A6.125 6.125 0 0 1 2.51 2.836l1.122.395Zm3.37 1.727a2.042 2.042 0 1 1 0 4.084 2.042 2.042 0 0 1 0-4.084Z"
          stroke="#000"
          strokeWidth={1.021}
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      </g>
      <defs>
        <clipPath id="a">
          <path fill="#fff" d="M0 0h14v14H0z" />
        </clipPath>
      </defs>
    </svg>
  );
}
