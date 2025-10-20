import * as React from "react";

export default function Archive({ fill = "#000", ...props }: React.SVGProps<SVGSVGElement>) {
  return (
    <svg {...props} fill="none" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 14 14">
      <g clipPath="url(#a)">
        <path
          fillRule="evenodd"
          clipRule="evenodd"
          d="M10.063.875a2.188 2.188 0 0 1 2.086 1.537l1.54 5.372c.079.25.14.502.186.758.062.205.094.42.095.646v.874c0 .58-.23 1.138-.64 1.55-.41.41-.963.64-1.549.64H2.156A2.193 2.193 0 0 1 .133 10.9a2.187 2.187 0 0 1-.166-.838v-.876c0-.218.032-.436.096-.645.045-.256.107-.51.185-.758l1.54-5.372A2.188 2.188 0 0 1 3.875.875H10.062Zm1.723 7H2.161a1.308 1.308 0 0 0-1.24.884 5.25 5.25 0 0 0-.072.866v.438a1.312 1.312 0 0 0 1.311 1.311h9.625a1.312 1.312 0 0 0 1.312-1.312v-.437c0-.291-.024-.58-.07-.866a1.313 1.313 0 0 0-1.242-.884h.001Zm-.437.875a.873.873 0 0 1 .809 1.21.873.873 0 0 1-1.144.474.872.872 0 0 1-.54-.809.872.872 0 0 1 .875-.875Zm-1.287-7H3.886a1.313 1.313 0 0 0-1.251.921l-1.26 4.48c.245-.096.51-.147.79-.147h9.626c.279 0 .546.052.79.147l-1.26-4.48a1.308 1.308 0 0 0-1.251-.921h-.007Z"
          fill={fill}
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


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


